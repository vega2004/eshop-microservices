using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Auth.API.Exceptions;
using Auth.API.Security;
using BuildingBlocks.Behaviors;
using Marten.Schema;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(Program).Assembly;
var allowedOrigins = GetAllowedOrigins(builder.Configuration, builder.Environment);

builder.Services.AddCarter();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("AuthDb")!);

    opts.Schema.For<AuthUser>()
        .Identity(x => x.Id)
        .UniqueIndex(UniqueIndexType.DuplicatedField, x => x.NormalizedUserName)
        .UniqueIndex(UniqueIndexType.DuplicatedField, x => x.NormalizedEmail);
})
.UseLightweightSessions();

builder.Services.AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

ValidateJwtOptions(jwtOptions);

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.AddHostedService<BootstrapAdminInitializer>();

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

builder.Services.AddExceptionHandler<AuthExceptionHandler>();
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

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("AuthDb")!);

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("ReactApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapCarter();

app.MapHealthChecks("/health");

app.Run();

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

    if (options.ExpirationMinutes <= 0)
    {
        throw new InvalidOperationException("Jwt:ExpirationMinutes debe ser mayor que cero.");
    }
}
