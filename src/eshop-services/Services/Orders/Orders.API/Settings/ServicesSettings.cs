namespace Orders.API.Settings;

public class ServicesSettings
{
    public const string SectionName = "Services";

    public string BasketBaseUrl { get; set; } = string.Empty;
    public string CatalogBaseUrl { get; set; } = string.Empty;
}
