using Microsoft.AspNetCore.Http;

namespace Catalog.API.Exceptions
{
    using FluentValidation;
    using Microsoft.AspNetCore.Diagnostics;
    public class CustomExceptionHandler
        : IExceptionHandler
    {
        private readonly ILogger<CustomExceptionHandler> _logger;

        public CustomExceptionHandler(
            ILogger<CustomExceptionHandler> logger)
        {
            _logger = logger;
        }

        //Este metodo se encarga de manejar las excepciones

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Excepcion capturada");

            var statusCode = StatusCodes.Status500InternalServerError;

            if(exception is ValidationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
            }

            if (exception is ProductNotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
            }

            httpContext.Response.StatusCode = statusCode;
            // este metodo devuelve un json como respuesta
            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    Title = exception.GetType().Name,
                    StatusCode = statusCode,
                    Detail = exception.Message
                },
                cancellationToken: cancellationToken);

            return true;
        }
    }
}
