using System.Text.Json.Serialization;

namespace Orders.API.Clients;

public record BasketResponse(BasketDto Cart);

public record BasketDto(
    string UserId,
    IEnumerable<BasketItemDto> Items,
    decimal TotalPrice);

public record BasketItemDto(
    int Quantity,
    string Color,
    decimal Price,
    Guid ProductId,
    string ProductName);

public record ProductResponse(ProductDto Product);

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    List<string> Category,
    string ImageFiles,
    decimal Price,
    int Stock);
