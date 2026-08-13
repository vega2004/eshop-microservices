using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Tickets.API.Exceptions;

public class TicketsExceptionHandler(ILogger<TicketsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (title, detail, statusCode) = exception switch
        {
            TicketUnauthorizedException => ("Unauthorized", "JWT invalido o ausente.", StatusCodes.Status401Unauthorized),
            TicketForbiddenException => ("Forbidden", exception.Message, StatusCodes.Status403Forbidden),
            TicketNotFoundException => ("NotFound", exception.Message, StatusCodes.Status404NotFound),
            TicketInternalException => ("InternalServerError", exception.Message, StatusCodes.Status500InternalServerError),
            _ => ("InternalServerError", "Error interno del servidor.", StatusCodes.Status500InternalServerError)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error interno procesando solicitud de tickets.");
        }
        else
        {
            logger.LogWarning("Solicitud de tickets rechazada: {Message}", exception.Message);
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

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

        return true;
    }
}
