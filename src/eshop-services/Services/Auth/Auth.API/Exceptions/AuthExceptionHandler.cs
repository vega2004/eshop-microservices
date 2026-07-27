using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Exceptions;

public class AuthExceptionHandler(ILogger<AuthExceptionHandler> logger) : IExceptionHandler
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
            InvalidCredentialsException => ("InvalidCredentialsException", exception.Message, StatusCodes.Status401Unauthorized),
            InvalidAuthenticatedUserException => ("InvalidAuthenticatedUserException", exception.Message, StatusCodes.Status401Unauthorized),
            UserAlreadyExistsException => ("UserAlreadyExistsException", exception.Message, StatusCodes.Status409Conflict),
            AuthUserNotFoundException => ("AuthUserNotFoundException", exception.Message, StatusCodes.Status404NotFound),
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
