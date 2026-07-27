using Auth.API.Features;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Auth.API.Features.GetCurrentUser;

public class GetCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var response = await sender.Send(new GetCurrentUserQuery(userId));

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Consultar usuario autenticado")
        .WithDescription("Devuelve el usuario asociado al JWT válido");
    }
}
