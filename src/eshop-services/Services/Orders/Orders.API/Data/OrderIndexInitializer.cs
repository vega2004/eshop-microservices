using MongoDB.Driver;

namespace Orders.API.Data;

public class OrderIndexInitializer(
    IMongoCollection<Order> orders,
    ILogger<OrderIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var customerIdempotencyIndex = new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys
                .Ascending(order => order.CustomerId)
                .Ascending(order => order.IdempotencyKey),
            new CreateIndexOptions
            {
                Name = "ux_orders_customer_idempotency",
                Unique = true
            });

        var customerIndex = new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(order => order.CustomerId),
            new CreateIndexOptions
            {
                Name = "ix_orders_customer_id"
            });

        var orderNumberIndex = new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(order => order.OrderNumber),
            new CreateIndexOptions
            {
                Name = "ux_orders_order_number",
                Unique = true
            });

        var customerUserNameIndex = new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(order => order.CustomerUserName),
            new CreateIndexOptions
            {
                Name = "ix_orders_customer_user_name"
            });

        var customerEmailIndex = new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(order => order.CustomerEmail),
            new CreateIndexOptions
            {
                Name = "ix_orders_customer_email"
            });

        await orders.Indexes.CreateManyAsync(
            [customerIdempotencyIndex, customerIndex, orderNumberIndex, customerUserNameIndex, customerEmailIndex],
            cancellationToken: cancellationToken);

        logger.LogInformation("Indices de ordenes verificados.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
