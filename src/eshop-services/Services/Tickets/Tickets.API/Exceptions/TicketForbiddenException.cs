namespace Tickets.API.Exceptions;

public class TicketForbiddenException(string message) : Exception(message);
