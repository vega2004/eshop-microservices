using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tickets.API.Clients;
using Tickets.API.Exceptions;
using Tickets.API.Settings;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = GetAllowedOrigins(builder.Configuration, builder.Environment);
var servicesSettings = builder.Configuration
    .GetSection(ServicesSettings.SectionName)
    .Get<ServicesSettings>() ?? new ServicesSettings();
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

ValidateServicesOptions(servicesSettings);
ValidateJwtOptions(jwtOptions);

builder.Services.Configure<ServicesSettings>(
    builder.Configuration.GetSection(ServicesSettings.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddHttpClient<OrdersClient>(client =>
{
    client.BaseAddress = new Uri(servicesSettings.OrdersBaseUrl);
});
builder.Services.AddSingleton<ITicketPdfService, TicketPdfService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<TicketsExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("ReactApp");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/tickets/orders/{orderId:guid}", async (
    Guid orderId,
    HttpRequest request,
    HttpResponse response,
    OrdersClient ordersClient,
    ITicketPdfService ticketPdfService,
    CancellationToken cancellationToken) =>
{
    var bearerToken = GetBearerToken(request);
    var order = await ordersClient.GetOrder(orderId, bearerToken, cancellationToken);
    var ticket = ticketPdfService.Generate(order);
    var fileName = $"ticket-{order.OrderNumber}.pdf";
    response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";

    return Results.File(
        ticket,
        MediaTypeNames.Application.Pdf,
        enableRangeProcessing: false);
})
.RequireAuthorization()
.WithName("GetOrderTicket")
.Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Application.Pdf)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError)
.WithOpenApi();

app.MapHealthChecks("/health");

app.Run();

static string GetBearerToken(HttpRequest request)
{
    var authorization = request.Headers.Authorization.FirstOrDefault();

    if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        throw new TicketUnauthorizedException();
    }

    return authorization["Bearer ".Length..].Trim();
}

static string[] GetAllowedOrigins(IConfiguration configuration, IWebHostEnvironment environment)
{
    var origins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim())
        .ToArray() ?? [];

    if (environment.IsDevelopment() && origins.Length == 0)
    {
        return ["http://localhost:5173", "http://localhost:5174"];
    }

    if (environment.IsProduction())
    {
        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins debe configurar al menos un origen en Production.");
        }

        if (origins.Any(origin => origin == "*"))
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins no permite wildcard '*' en Production.");
        }
    }

    return origins;
}

static void ValidateServicesOptions(ServicesSettings settings)
{
    if (!Uri.TryCreate(settings.OrdersBaseUrl, UriKind.Absolute, out _))
    {
        throw new InvalidOperationException("Services:OrdersBaseUrl debe estar configurado con una URL absoluta.");
    }
}

static void ValidateJwtOptions(JwtOptions options)
{
    if (string.IsNullOrWhiteSpace(options.Issuer))
    {
        throw new InvalidOperationException("Jwt:Issuer debe estar configurado.");
    }

    if (string.IsNullOrWhiteSpace(options.Audience))
    {
        throw new InvalidOperationException("Jwt:Audience debe estar configurado.");
    }

    if (string.IsNullOrWhiteSpace(options.Key))
    {
        throw new InvalidOperationException("Jwt:Key debe estar configurado mediante Jwt__Key.");
    }

    if (Encoding.UTF8.GetByteCount(options.Key) < 32)
    {
        throw new InvalidOperationException("Jwt:Key debe tener al menos 32 bytes para HMAC SHA-256.");
    }
}
