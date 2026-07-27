using Basket.API.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Basket.API.Data
{
    public class CachedBasketRepository(
        IBasketRepository repository,
        IDistributedCache cache)
        : IBasketRepository
    {
        private static readonly DistributedCacheEntryOptions CacheOptions =
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(30),

                SlidingExpiration =
                    TimeSpan.FromMinutes(10)
            };

        public async Task<ShoppingCart> GetBasket(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GetCacheKey(userId);
            var cachedBasket = await cache.GetStringAsync(
                cacheKey,
                cancellationToken);

            if (!string.IsNullOrEmpty(cachedBasket))
            {
                return JsonSerializer.Deserialize<ShoppingCart>(
                    cachedBasket)!;
            }

            var basket = await repository.GetBasket(
                userId,
                cancellationToken);

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(basket),
                CacheOptions,
                cancellationToken);

            return basket;
        }

        public async Task<ShoppingCart> StoreBasket(
            ShoppingCart basket,
            CancellationToken cancellationToken = default)
        {
            await repository.StoreBasket(
                basket,
                cancellationToken);

            await cache.SetStringAsync(
                GetCacheKey(basket.UserId),
                JsonSerializer.Serialize(basket),
                CacheOptions,
                cancellationToken);

            return basket;
        }

        public async Task<bool> DeleteBasket(
            string userId,
            CancellationToken cancellationToken = default)
        {
            await repository.DeleteBasket(
                userId,
                cancellationToken);

            await cache.RemoveAsync(
                GetCacheKey(userId),
                cancellationToken);

            return true;
        }

        private static string GetCacheKey(string userId) => $"basket:{userId}";
    }
}
