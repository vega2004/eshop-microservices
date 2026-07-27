using Basket.API.Models;

namespace Basket.API.Data
{
    public interface IBasketRepository
    {
        Task<ShoppingCart> GetBasket(
            string userId,
            CancellationToken cancellationToken = default);

        Task<ShoppingCart> StoreBasket(
            ShoppingCart basket,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteBasket(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
