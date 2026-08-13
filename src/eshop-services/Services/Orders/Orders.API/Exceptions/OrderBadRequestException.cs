namespace Orders.API.Exceptions;

public class OrderBadRequestException(string message) : Exception(message);
