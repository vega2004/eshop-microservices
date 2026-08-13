namespace Orders.API.Settings;

public class OrdersSettings
{
    public const string SectionName = "Orders";

    public decimal TaxRate { get; set; } = 0.16m;
}
