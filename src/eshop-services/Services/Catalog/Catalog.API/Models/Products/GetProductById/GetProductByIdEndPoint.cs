namespace Catalog.API.Models.Products.GetProductById
{
    public record GetProductByIdResponse(Product Product);

    public class GetProductByIdEndPoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetProductByIdQuery(id),
                    cancellationToken);

                var response = result.Adapt<GetProductByIdResponse>();

                return Results.Ok(response);
            })
            .WithName("GetProductById")
            .WithSummary("Obtener producto por identificador")
            .WithDescription("Obtiene un producto del catálogo mediante su identificador.")
            .Produces<GetProductByIdResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        }
    }
}
