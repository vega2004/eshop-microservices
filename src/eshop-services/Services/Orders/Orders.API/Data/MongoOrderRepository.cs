using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Orders.API.Data;

public class MongoOrderRepository(IMongoCollection<Order> orders) : IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetAll(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escapedSearch = Regex.Escape(search.Trim());
            var regex = new MongoDB.Bson.BsonRegularExpression(escapedSearch, "i");
            filter = Builders<Order>.Filter.Or(
                Builders<Order>.Filter.Regex(order => order.OrderNumber, regex),
                Builders<Order>.Filter.Regex(order => order.CustomerUserName, regex),
                Builders<Order>.Filter.Regex(order => order.CustomerEmail, regex),
                Builders<Order>.Filter.Regex(order => order.CustomerId, regex));
        }

        return await orders.Find(filter)
            .SortByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await orders.Find(order => order.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Order?> GetByCustomerAndIdempotencyKey(
        string customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await orders.Find(order =>
                order.CustomerId == customerId && order.IdempotencyKey == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerId(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        return await orders.Find(order => order.CustomerId == customerId)
            .SortByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task Insert(Order order, CancellationToken cancellationToken = default)
    {
        await orders.InsertOneAsync(order, cancellationToken: cancellationToken);
    }

    public async Task<Order?> UpdateStatus(
        Guid id,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<Order>.Update.Set(order => order.Status, status);

        return await orders.FindOneAndUpdateAsync(
            order => order.Id == id,
            update,
            new FindOneAndUpdateOptions<Order>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }
}
