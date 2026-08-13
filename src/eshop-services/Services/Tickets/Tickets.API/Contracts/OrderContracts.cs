using System.Text.Json;

namespace Tickets.API.Contracts;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerUserName,
    string CustomerEmail,
    DateTime CreatedAt,
    JsonElement Status,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    decimal Tax,
    decimal Total);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
