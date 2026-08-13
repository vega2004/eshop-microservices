using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orders.API.Models;

public class OrderItem
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal LineTotal { get; set; }
}
