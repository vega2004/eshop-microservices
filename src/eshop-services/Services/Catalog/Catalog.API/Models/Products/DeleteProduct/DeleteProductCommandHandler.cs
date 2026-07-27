namespace Catalog.API.Models.Products.DeleteProduct
{
    public record DeleteProductCommand(Guid Id)
        : ICommand<DeleteProductResult>;

    public record DeleteProductResult(bool IsSuccess);

    internal class DeleteProductCommandHandler(
        IDocumentSession session,
        ILogger<DeleteProductCommandHandler> logger)
        : ICommandHandler<DeleteProductCommand, DeleteProductResult>
    {
        public async Task<DeleteProductResult> Handle(
            DeleteProductCommand command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Eliminando producto con identificador {ProductId}",
                command.Id);

            var product = await session.LoadAsync<Product>(
                command.Id,
                cancellationToken);

            if (product is null)
            {
                return new DeleteProductResult(false);
            }

            session.Delete(product);

            await session.SaveChangesAsync(cancellationToken);

            return new DeleteProductResult(true);
        }
    }
}