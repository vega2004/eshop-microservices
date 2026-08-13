using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Orders.API.Exceptions;

public class OrdersExceptionHandler(ILogger<OrdersExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (title, detail, statusCode) = exception switch
        {
            OrderBadRequestException => ("BadRequest", exception.Message, StatusCodes.Status400BadRequest),
            OrderForbiddenException => ("Forbidden", exception.Message, StatusCodes.Status403Forbidden),
            OrderNotFoundException => ("NotFound", exception.Message, StatusCodes.Status404NotFound),
            OrderConflictException => ("Conflict", exception.Message, StatusCodes.Status409Conflict),
            OrderInternalException => ("InternalServerError", exception.Message, StatusCodes.Status500InternalServerError),
            MongoException => ("InternalServerError", "Error interno de persistencia.", StatusCodes.Status500InternalServerError),
            _ => ("InternalServerError", "Error interno del servidor.", StatusCodes.Status500InternalServerError)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error interno procesando solicitud de ordenes.");
        }
        else
        {
            logger.LogWarning("Solicitud de ordenes rechazada: {Message}", exception.Message);
        }

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken: cancellationToken);

        return true;
    }
}
