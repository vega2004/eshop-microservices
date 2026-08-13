namespace Orders.API.Exceptions;

public class OrderForbiddenException(string message) : Exception(message);
