namespace Catalog.API.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<String> Category { get; set; } = new();
        public string ImageFiles { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }

    }
}
