using Catalog.API.Exceptions;

namespace Catalog.API.Models.Products.GetProductById
{
    public record GetProductByIdQuery(Guid Id)
        : IQuery<GetProductByIdResult>;

    public record GetProductByIdResult(Product Product);

    public class GetProductByIdQueryValidator
        : AbstractValidator<GetProductByIdQuery>
    {
        public GetProductByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("El identificador del producto es requerido.");
        }
    }

    internal class GetProductByIdQueryHandler(
        IDocumentSession session,
        ILogger<GetProductByIdQueryHandler> logger)
        : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
    {
        public async Task<GetProductByIdResult> Handle(
            GetProductByIdQuery query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Consultando producto con identificador {ProductId}",
                query.Id);

            var product = await session.LoadAsync<Product>(
                query.Id,
                cancellationToken);

            if (product is null)
            {
                throw new ProductNotFoundException(query.Id);
            }

            return new GetProductByIdResult(product);
        }
    }
}
