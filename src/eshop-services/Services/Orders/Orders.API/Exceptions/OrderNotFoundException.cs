namespace Orders.API.Exceptions;

public class OrderNotFoundException(Guid orderId)
    : Exception($"No se encontro la orden {orderId}.");
