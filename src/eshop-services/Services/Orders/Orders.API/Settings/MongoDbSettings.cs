namespace Orders.API.Settings;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string DatabaseName { get; set; } = string.Empty;
    public string OrdersCollection { get; set; } = string.Empty;
}
