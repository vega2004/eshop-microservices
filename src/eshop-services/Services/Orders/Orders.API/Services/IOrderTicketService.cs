namespace Orders.API.Services;

public interface IOrderTicketService
{
    byte[] Generate(Order order);
}
