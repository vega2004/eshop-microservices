namespace Orders.API.Exceptions;

public class OrderConflictException(string message) : Exception(message);
