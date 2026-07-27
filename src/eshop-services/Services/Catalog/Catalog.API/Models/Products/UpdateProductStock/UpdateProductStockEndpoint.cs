namespace Catalog.API.Models.Products.UpdateProductStock
{
    public record UpdateProductStockRequest(int? Stock);

    public record UpdateProductStockResponse(Product Product);

    public class UpdateProductStockEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/products/{id:guid}/stock", async (
                Guid id,
                UpdateProductStockRequest request,
                IDocumentSession session,
                CancellationToken cancellationToken) =>
            {
                if (request.Stock is null || request.Stock < 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "El stock debe ser un entero mayor o igual a cero."
                    });
                }

                var product = await session.LoadAsync<Product>(id, cancellationToken);

                if (product is null)
                {
                    return Results.NotFound(new
                    {
                        message = $"No se encontro el producto {id}"
                    });
                }

                product.Stock = request.Stock.Value;

                session.Store(product);

                await session.SaveChangesAsync(cancellationToken);

                return Results.Ok(new UpdateProductStockResponse(product));
            })
            .WithName("UpdateProductStock")
            .RequireAuthorization("AdminOnly")
            .Produces<UpdateProductStockResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Actualizar stock de producto")
            .WithDescription("Actualiza solamente las unidades disponibles de un producto del catalogo.");
        }
    }
}
