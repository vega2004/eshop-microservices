namespace Basket.API.Models
{
    public class ShoppingCart
    {
        public string UserId { get; set; } = string.Empty;

        public List<ShoppingCartItem> Items { get; set; } = new();

        public decimal TotalPrice => Items.Sum(x => x.Price * x.Quantity);

        public ShoppingCart(string userId)
        {
            UserId = userId;
        }

        public ShoppingCart()
        {
        }
    }
}
