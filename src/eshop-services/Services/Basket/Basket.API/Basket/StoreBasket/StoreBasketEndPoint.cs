using Basket.API.Models;
using Basket.API.Security;
using Carter;
using MediatR;
using System.Security.Claims;

namespace Basket.API.Basket.StoreBasket
{
    public record BasketInput(IEnumerable<ShoppingCartItem> Items);

    public record StoreBasketRequest(BasketInput Cart);

    public record StoreBasketResponse(string UserId);

    public class StoreBasketEndPoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/basket",
                async (StoreBasketRequest request, ClaimsPrincipal user, ISender sender) =>
                {
                    var userId = user.GetRequiredUserId();
                    var command = new StoreBasketCommand(
                        userId,
                        request.Cart.Items);

                    var result = await sender.Send(command);

                    var response = new StoreBasketResponse(result.UserId);

                    return Results.Created(
                        "/basket",
                        response);
                })
                .RequireAuthorization()
                .WithName("StoreBasket")
.Produces<StoreBasketResponse>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.WithSummary("Guardar carrito")
.WithDescription("Guarda o actualiza el carrito del usuario autenticado");
        }
    }
}
