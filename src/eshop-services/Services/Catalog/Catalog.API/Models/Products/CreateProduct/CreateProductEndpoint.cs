using Carter;
using MediatR;

namespace Catalog.API.Models.Products.CreateProduct
{
    public record CreateProductRequest(string Name, string Description,
        List<string> Category, string ImageFiles, decimal Price, int? Stock);

    public record CreateProductResponse(Guid Id);

    public class CreateProductEndPoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/products", async (CreateProductRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = request.Adapt<CreateProductCommand>();

                var result = await sender.Send(command, cancellationToken);
                var response = result.Adapt<CreateProductResponse>();
                return Results.Created($"/products/{response.Id}", response);


            })
                .WithName("CrearProducto")
                .RequireAuthorization("AdminOnly")
                .Produces<CreateProductResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Crear un nuevo producto")
                .WithDescription("Crea a nuevo producto y se retorna el identidicador de la entidad");
                
        }
    }
}
