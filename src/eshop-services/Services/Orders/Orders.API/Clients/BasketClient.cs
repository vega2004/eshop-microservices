using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Orders.API.Exceptions;

namespace Orders.API.Clients;

public class BasketClient(HttpClient httpClient, ILogger<BasketClient> logger)
{
    public async Task<BasketDto> GetBasket(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/basket");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new OrderBadRequestException("El carrito del cliente no existe.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Basket.API respondio con estado {StatusCode} al consultar el carrito.",
                response.StatusCode);
            throw new OrderInternalException("No fue posible consultar el carrito.");
        }

        var basketResponse = await response.Content.ReadFromJsonAsync<BasketResponse>(
            cancellationToken: cancellationToken);

        return basketResponse?.Cart
            ?? throw new OrderBadRequestException("La respuesta del carrito no es valida.");
    }
}
