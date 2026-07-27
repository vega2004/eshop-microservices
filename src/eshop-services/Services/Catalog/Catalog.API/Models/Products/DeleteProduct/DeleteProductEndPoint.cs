namespace Catalog.API.Models.Products.DeleteProduct
{

    public record DeleteProductResponse(bool IsSuccess);
    public class DeleteProductEndPoint : ICarterModule
    {

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("/products/{id}", async (
     Guid id,
     ISender sender,
     CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new DeleteProductCommand(id),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new
                    {
                        message = $"No se encontró el producto {id}"
                    });
                }

                var response = result.Adapt<DeleteProductResponse>();

                return Results.Ok(response);
            })
 .WithName("DeleteProduct")
 .RequireAuthorization("AdminOnly")
 .Produces<DeleteProductResponse>(StatusCodes.Status200OK)
 .ProducesProblem(StatusCodes.Status401Unauthorized)
 .ProducesProblem(StatusCodes.Status403Forbidden)
 .ProducesProblem(StatusCodes.Status404NotFound)
 .ProducesProblem(StatusCodes.Status400BadRequest)
 .WithSummary("Borrar producto")
 .WithDescription("Elimina un producto del catálogo");
        }
    }
}
