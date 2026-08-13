using System.Net;
using System.Net.Http.Json;
using Orders.API.Exceptions;

namespace Orders.API.Clients;

public class CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger)
{
    public async Task<ProductDto> GetProduct(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/products/{productId:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new OrderBadRequestException($"El producto {productId} no existe.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Catalog.API respondio con estado {StatusCode} al consultar producto {ProductId}.",
                response.StatusCode,
                productId);
            throw new OrderInternalException("No fue posible consultar el catalogo.");
        }

        var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>(
            cancellationToken: cancellationToken);

        return productResponse?.Product
            ?? throw new OrderBadRequestException($"La respuesta del producto {productId} no es valida.");
    }
}
