using Auth.API.Features;

namespace Auth.API.Features.Register;

public record RegisterRequest(string UserName, string Email, string Password);

public class RegisterEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, ISender sender) =>
        {
            var command = new RegisterCommand(
                request.UserName,
                request.Email,
                request.Password);

            var response = await sender.Send(command);

            return Results.Created($"/auth/users/{response.User.Id}", response);
        })
        .WithName("RegisterUser")
        .Produces<AuthResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Registrar usuario")
        .WithDescription("Registra un nuevo usuario y devuelve un JWT de acceso");
    }
}
