namespace Tickets.API.Exceptions;

public class TicketNotFoundException(Guid orderId) : Exception($"La orden {orderId} no fue encontrada.");
