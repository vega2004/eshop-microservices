namespace Tickets.API.Services;

public interface ITicketPdfService
{
    byte[] Generate(OrderDto order);
}
