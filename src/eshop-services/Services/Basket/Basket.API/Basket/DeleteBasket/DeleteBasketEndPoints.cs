using Basket.API.Security;
using Carter;
using Mapster;
using MediatR;
using System.Security.Claims;

namespace Basket.API.Basket.DeleteBasket
{
    public record DeleteBasketResponse(bool IsSuccess);

    public class DeleteBasketEndPoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                "/basket",
                async (ClaimsPrincipal user, ISender sender) =>
                {
                    var userId = user.GetRequiredUserId();
                    var result = await sender.Send(
                        new DeleteBasketCommand(userId));

                    var response = result.Adapt<DeleteBasketResponse>();

                    return Results.Ok(response);
                })
                .RequireAuthorization()
                .WithName("DeleteBasket")
.Produces<DeleteBasketResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithSummary("Eliminar carrito")
.WithDescription("Elimina el carrito del usuario autenticado");
        }
    }
}
