using Basket.API.Models;
using Basket.API.Security;
using Carter;
using Mapster;
using MediatR;
using System.Security.Claims;

namespace Basket.API.Basket.GetBasket
{
    public record GetBasketResponse(ShoppingCart Cart);

    public class GetBasketEndPoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet(
                "/basket",
                async (ClaimsPrincipal user, ISender sender) =>
                {
                    var userId = user.GetRequiredUserId();
                    var result = await sender.Send(
                        new GetBasketQuery(userId));

                    var response = result.Adapt<GetBasketResponse>();

                    return Results.Ok(response);
                })
                .RequireAuthorization()
               .WithName("GetBasket")
.Produces<GetBasketResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithSummary("Consultar carrito")
.WithDescription("Obtiene el carrito del usuario autenticado");
        }
    }
}
