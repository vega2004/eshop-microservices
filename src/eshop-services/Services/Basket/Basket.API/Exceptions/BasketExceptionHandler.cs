using BuildingBlocks.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Exceptions;

public class BasketExceptionHandler(ILogger<BasketExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            "Error Message: {ExceptionMessage}, Time of occurrence {Time}",
            exception.Message,
            DateTime.UtcNow);

        var (title, detail, statusCode) = exception switch
        {
            ValidationException => ("ValidationException", exception.Message, StatusCodes.Status400BadRequest),
            InvalidAuthenticatedUserException => ("InvalidAuthenticatedUserException", exception.Message, StatusCodes.Status401Unauthorized),
            BadRequestException => ("BadRequestException", exception.Message, StatusCodes.Status400BadRequest),
            NotFoundException => ("NotFoundException", exception.Message, StatusCodes.Status404NotFound),
            _ => ("InternalServerError", "Ocurrió un error inesperado.", StatusCodes.Status500InternalServerError)
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions.Add("validationErrors", validationException.Errors);
        }

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
