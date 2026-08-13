namespace Orders.API.Data;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAll(string? search, CancellationToken cancellationToken = default);
    Task<Order?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByCustomerAndIdempotencyKey(
        string customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerId(string customerId, CancellationToken cancellationToken = default);
    Task Insert(Order order, CancellationToken cancellationToken = default);
    Task<Order?> UpdateStatus(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
}
