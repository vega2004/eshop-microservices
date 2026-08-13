namespace Orders.API.Contracts;

public record CreateOrderRequest(string? CustomerId, string? BasketId);

public record UpdateOrderStatusRequest(string Status);
