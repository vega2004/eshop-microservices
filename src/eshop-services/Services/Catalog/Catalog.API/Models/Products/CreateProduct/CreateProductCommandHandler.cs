using Marten;
using System.Windows.Input;

namespace Catalog.API.Models.Products.CreateProduct
{
    /*record nos permite crear el producto con los datos para registrar como uno nuevo*/

    public record CreateProductCommand(string Name, string Description,
        List<String> Category, string ImageFiles, decimal Price, int? Stock)
        : ICommand<CreateProductResult>;

    public class CreateProductCommandValidator
        : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("El nombre del producto es requerido.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("La descripcion del producto es requerida.");

            RuleFor(x => x.Category)
                .NotNull()
                .NotEmpty()
                .WithMessage("El producto debe tener al menos una categoria.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El precio no puede ser negativo.");

            RuleFor(x => x.Stock)
                .NotNull()
                .WithMessage("El stock es requerido.")
                .GreaterThanOrEqualTo(0)
                .WithMessage("El stock no puede ser negativo.");
        }
    }


    /*este record retorna el objeto de respuesta es decir el identificador el identificador del objeto insertado*/

    public record CreateProductResult(Guid Id);

    internal class CreateProductCommandHandler(IDocumentSession documentSession) :
        ICommandHandler<CreateProductCommand,
        CreateProductResult>
    {
        public async Task<CreateProductResult> Handle(CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            Product product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                ImageFiles = request.ImageFiles,
                Price = request.Price,
                Stock = request.Stock!.Value
            };

            documentSession.Store(product);
            await documentSession.SaveChangesAsync(cancellationToken);
            return new CreateProductResult(product.Id);
        }
    }
}
