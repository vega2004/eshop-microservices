using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tickets.API.Exceptions;

namespace Tickets.API.Clients;

public class OrdersClient(HttpClient httpClient, ILogger<OrdersClient> logger)
{
    public async Task<OrderDto> GetOrder(
        Guid orderId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{orderId:D}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new TicketUnauthorizedException();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new TicketForbiddenException("No tiene acceso a la orden solicitada.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new TicketNotFoundException(orderId);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Orders.API respondio con estado {StatusCode} al consultar orden {OrderId}.",
                response.StatusCode,
                orderId);
            throw new TicketInternalException("No fue posible consultar la orden.");
        }

        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken)
            ?? throw new TicketInternalException("La respuesta de la orden no es valida.");
    }
}
