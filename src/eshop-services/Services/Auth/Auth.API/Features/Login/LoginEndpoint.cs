using Auth.API.Features;

namespace Auth.API.Features.Login;

public record LoginRequest(string Email, string Password);

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, ISender sender) =>
        {
            var command = new LoginCommand(
                request.Email,
                request.Password);

            var response = await sender.Send(command);

            return Results.Ok(response);
        })
        .WithName("LoginUser")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Iniciar sesión")
        .WithDescription("Autentica un usuario por correo electrónico y devuelve un JWT de acceso");
    }
}
