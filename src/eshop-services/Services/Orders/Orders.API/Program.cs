using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Orders.API.Clients;
using Orders.API.Data;
using Orders.API.Exceptions;
using Orders.API.HealthChecks;
using Orders.API.Services;
using Orders.API.Settings;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = GetAllowedOrigins(builder.Configuration, builder.Environment);

var mongoDbConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var mongoDbSettings = builder.Configuration
    .GetSection(MongoDbSettings.SectionName)
    .Get<MongoDbSettings>() ?? new MongoDbSettings();
var servicesSettings = builder.Configuration
    .GetSection(ServicesSettings.SectionName)
    .Get<ServicesSettings>() ?? new ServicesSettings();
var ordersSettings = builder.Configuration
    .GetSection(OrdersSettings.SectionName)
    .Get<OrdersSettings>() ?? new OrdersSettings();
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

ValidateMongoDbOptions(mongoDbConnectionString, mongoDbSettings);
ValidateServicesOptions(servicesSettings);
ValidateOrdersOptions(ordersSettings);
ValidateJwtOptions(jwtOptions);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.Configure<ServicesSettings>(
    builder.Configuration.GetSection(ServicesSettings.SectionName));
builder.Services.Configure<OrdersSettings>(
    builder.Configuration.GetSection(OrdersSettings.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoDbConnectionString));
builder.Services.AddSingleton(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDbSettings.DatabaseName);
});
builder.Services.AddSingleton(serviceProvider =>
{
    var database = serviceProvider.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<Order>(mongoDbSettings.OrdersCollection);
});
// Safe as singleton: MongoOrderRepository is stateless and only wraps thread-safe MongoDB driver types.
builder.Services.AddSingleton<IOrderRepository, MongoOrderRepository>();
builder.Services.AddSingleton<IOrderTicketService, OrderTicketService>();
builder.Services.AddHostedService<OrderIndexInitializer>();

builder.Services.AddHttpClient<BasketClient>(client =>
{
    client.BaseAddress = new Uri(servicesSettings.BasketBaseUrl);
});
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(servicesSettings.CatalogBaseUrl);
});

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

builder.Services.AddExceptionHandler<OrdersExceptionHandler>();
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

builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb");

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

app.MapPost("/api/orders", async (
    CreateOrderRequest? request,
    HttpContext httpContext,
    IOrderRepository orders,
    BasketClient basketClient,
    CatalogClient catalogClient,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var customerId = httpContext.User.GetRequiredCustomerId();
    var customerUserName = httpContext.User.GetCustomerUserName();
    var customerEmail = httpContext.User.GetCustomerEmail();
    var idempotencyKey = GetRequiredIdempotencyKey(httpContext.Request);
    request ??= new CreateOrderRequest(null, null);

    ValidateCreateOrderRequest(request, customerId);

    var existingOrder = await orders.GetByCustomerAndIdempotencyKey(
        customerId,
        idempotencyKey,
        cancellationToken);

    if (existingOrder is not null)
    {
        logger.LogInformation(
            "Replay idempotente de orden {OrderId} para cliente {CustomerId}.",
            existingOrder.Id,
            customerId);
        return Results.Ok(existingOrder);
    }

    var bearerToken = GetBearerToken(httpContext.Request);
    var basket = await basketClient.GetBasket(bearerToken, cancellationToken);

    if (basket.UserId != customerId)
    {
        throw new OrderBadRequestException("El carrito no pertenece al cliente autenticado.");
    }

    var basketItems = basket.Items?.ToList() ?? [];

    if (basketItems.Count == 0)
    {
        throw new OrderBadRequestException("El carrito esta vacio.");
    }

    var orderItems = new List<OrderItem>();

    foreach (var basketItem in basketItems)
    {
        if (basketItem.Quantity <= 0)
        {
            throw new OrderBadRequestException("La cantidad de cada producto debe ser mayor a cero.");
        }

        if (basketItem.ProductId == Guid.Empty)
        {
            throw new OrderBadRequestException("El identificador del producto es requerido.");
        }

        if (string.IsNullOrWhiteSpace(basketItem.ProductName))
        {
            throw new OrderBadRequestException("El nombre del producto es requerido.");
        }

        var product = await catalogClient.GetProduct(basketItem.ProductId, cancellationToken);

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new OrderBadRequestException($"El producto {product.Id} tiene datos inconsistentes.");
        }

        if (product.Stock < basketItem.Quantity)
        {
            throw new OrderBadRequestException($"Stock insuficiente para el producto {product.Id}.");
        }

        if (basketItem.Price != product.Price)
        {
            throw new OrderBadRequestException($"El precio del producto {product.Id} no coincide con catalogo.");
        }

        var lineTotal = product.Price * basketItem.Quantity;

        orderItems.Add(new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = basketItem.Quantity,
            UnitPrice = product.Price,
            LineTotal = lineTotal
        });
    }

    var subtotal = orderItems.Sum(item => item.LineTotal);
    var tax = subtotal * ordersSettings.TaxRate;
    var total = subtotal + tax;

    var createdAt = DateTime.UtcNow;
    var order = new Order
    {
        Id = Guid.NewGuid(),
        OrderNumber = OrderNumberGenerator.Generate(createdAt),
        CustomerId = customerId,
        CustomerUserName = customerUserName,
        CustomerEmail = customerEmail,
        BasketId = request.BasketId ?? customerId,
        CreatedAt = createdAt,
        Status = OrderStatus.Pending,
        Items = orderItems,
        Subtotal = subtotal,
        Tax = tax,
        Total = total,
        IdempotencyKey = idempotencyKey
    };

    for (var attempt = 0; attempt < 5; attempt++)
    {
        try
        {
            await orders.Insert(order, cancellationToken);
            logger.LogInformation(
                "Orden {OrderId} con folio {OrderNumber} creada para cliente {CustomerId}.",
                order.Id,
                order.OrderNumber,
                customerId);

            return Results.Created($"/api/orders/{order.Id:D}", order);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var duplicatedOrder = await orders.GetByCustomerAndIdempotencyKey(
                customerId,
                idempotencyKey,
                cancellationToken);

            if (duplicatedOrder is not null)
            {
                logger.LogInformation(
                    "Condicion de carrera idempotente resuelta para orden {OrderId}.",
                    duplicatedOrder.Id);
                return Results.Ok(duplicatedOrder);
            }

            order.OrderNumber = OrderNumberGenerator.Generate(createdAt);
        }
    }

    throw new OrderInternalException("No fue posible generar un folio unico para la orden.");
})
.RequireAuthorization()
.WithName("CreateOrder")
.Produces<Order>(StatusCodes.Status201Created)
.Produces<Order>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status500InternalServerError)
.WithOpenApi();

app.MapGet("/api/orders", async (
    string? search,
    IOrderRepository orders,
    CancellationToken cancellationToken) =>
{
    var allOrders = await orders.GetAll(search, cancellationToken);

    return Results.Ok(allOrders);
})
.RequireAuthorization("AdminOnly")
.WithName("GetAllOrders")
.Produces<IReadOnlyList<Order>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.WithOpenApi();

app.MapGet("/api/orders/{id:guid}", async (
    Guid id,
    ClaimsPrincipal user,
    IOrderRepository orders,
    CancellationToken cancellationToken) =>
{
    var customerId = user.GetRequiredCustomerId();
    var order = await orders.GetById(id, cancellationToken)
        ?? throw new OrderNotFoundException(id);

    EnsureOrderAccess(user, customerId, order);

    return Results.Ok(order);
})
.RequireAuthorization()
.WithName("GetOrderById")
.Produces<Order>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

app.MapGet("/api/orders/{id:guid}/ticket", async (
    Guid id,
    ClaimsPrincipal user,
    IOrderRepository orders,
    IOrderTicketService ticketService,
    CancellationToken cancellationToken) =>
{
    var customerId = user.GetRequiredCustomerId();
    var order = await orders.GetById(id, cancellationToken)
        ?? throw new OrderNotFoundException(id);

    EnsureOrderAccess(user, customerId, order);

    var ticket = ticketService.Generate(order);
    var fileName = $"ticket-{order.OrderNumber}.pdf";

    return Results.File(ticket, "application/pdf", fileName);
})
.RequireAuthorization()
.WithName("GetOrderTicket")
.Produces(StatusCodes.Status200OK, contentType: "application/pdf")
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

app.MapGet("/api/orders/customer/{customerId}", async (
    string customerId,
    ClaimsPrincipal user,
    IOrderRepository orders,
    CancellationToken cancellationToken) =>
{
    var authenticatedCustomerId = user.GetRequiredCustomerId();

    if (!user.IsAdmin() && customerId != authenticatedCustomerId)
    {
        throw new OrderForbiddenException("No puede consultar ordenes de otro cliente.");
    }

    var customerOrders = await orders.GetByCustomerId(customerId, cancellationToken);

    return Results.Ok(customerOrders);
})
.RequireAuthorization()
.WithName("GetOrdersByCustomer")
.Produces<IReadOnlyList<Order>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.WithOpenApi();

app.MapPatch("/api/orders/{id:guid}/status", async (
    Guid id,
    UpdateOrderStatusRequest request,
    IOrderRepository orders,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var order = await orders.GetById(id, cancellationToken)
        ?? throw new OrderNotFoundException(id);

    var requestedStatus = ParseOrderStatus(request.Status);

    if (order.Status == requestedStatus)
    {
        return Results.Ok(order);
    }

    if (!IsValidTransition(order.Status, requestedStatus))
    {
        throw new OrderConflictException(
            $"No se permite cambiar una orden de {order.Status} a {requestedStatus}.");
    }

    var updatedOrder = await orders.UpdateStatus(id, requestedStatus, cancellationToken)
        ?? throw new OrderNotFoundException(id);

    logger.LogInformation(
        "Orden {OrderId} cambio de estado {OldStatus} a {NewStatus}.",
        id,
        order.Status,
        requestedStatus);

    return Results.Ok(updatedOrder);
})
.RequireAuthorization("AdminOnly")
.WithName("UpdateOrderStatus")
.Produces<Order>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict)
.WithOpenApi();

app.MapHealthChecks("/health");

app.Run();

static string GetRequiredIdempotencyKey(HttpRequest request)
{
    var idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(idempotencyKey))
    {
        throw new OrderBadRequestException("Idempotency-Key es requerido.");
    }

    return idempotencyKey.Trim();
}

static string GetBearerToken(HttpRequest request)
{
    var authorization = request.Headers.Authorization.FirstOrDefault();

    if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        throw new OrderBadRequestException("Authorization Bearer es requerido para consultar Basket.API.");
    }

    return authorization["Bearer ".Length..].Trim();
}

static void ValidateCreateOrderRequest(CreateOrderRequest request, string customerId)
{
    if (!string.IsNullOrWhiteSpace(request.CustomerId) && request.CustomerId != customerId)
    {
        throw new OrderForbiddenException("El customerId no coincide con el usuario autenticado.");
    }

    if (!string.IsNullOrWhiteSpace(request.BasketId) && request.BasketId != customerId)
    {
        throw new OrderBadRequestException("El basketId no coincide con el carrito del usuario autenticado.");
    }
}

static OrderStatus ParseOrderStatus(string status)
{
    if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsedStatus))
    {
        throw new OrderBadRequestException("Estado de orden invalido.");
    }

    return parsedStatus;
}

static bool IsValidTransition(OrderStatus currentStatus, OrderStatus requestedStatus)
{
    return currentStatus == OrderStatus.Pending &&
        (requestedStatus == OrderStatus.Confirmed || requestedStatus == OrderStatus.Cancelled);
}

static void EnsureOrderAccess(ClaimsPrincipal user, string customerId, Order order)
{
    if (!user.IsAdmin() && order.CustomerId != customerId)
    {
        throw new OrderForbiddenException("No puede acceder a ordenes de otro cliente.");
    }
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

static void ValidateMongoDbOptions(string? connectionString, MongoDbSettings settings)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:MongoDb debe estar configurado mediante ConnectionStrings__MongoDb.");
    }

    if (string.IsNullOrWhiteSpace(settings.DatabaseName))
    {
        throw new InvalidOperationException("MongoDb:DatabaseName debe estar configurado.");
    }

    if (string.IsNullOrWhiteSpace(settings.OrdersCollection))
    {
        throw new InvalidOperationException("MongoDb:OrdersCollection debe estar configurado.");
    }
}

static void ValidateServicesOptions(ServicesSettings settings)
{
    if (!Uri.TryCreate(settings.BasketBaseUrl, UriKind.Absolute, out _))
    {
        throw new InvalidOperationException("Services:BasketBaseUrl debe estar configurado con una URL absoluta.");
    }

    if (!Uri.TryCreate(settings.CatalogBaseUrl, UriKind.Absolute, out _))
    {
        throw new InvalidOperationException("Services:CatalogBaseUrl debe estar configurado con una URL absoluta.");
    }
}

static void ValidateOrdersOptions(OrdersSettings settings)
{
    if (settings.TaxRate < 0)
    {
        throw new InvalidOperationException("Orders:TaxRate no puede ser negativo.");
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

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredCustomerId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new OrderBadRequestException("El usuario autenticado no es valido.");
        }

        return userId.ToString("D");
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin") ||
            principal.Claims.Any(claim => claim.Type == "role" && claim.Value == "Admin");
    }

    public static string GetCustomerUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? string.Empty;
    }

    public static string GetCustomerEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
    }
}
