using Catalog.API.Common.Pagination;

namespace Catalog.API.Models.Products.GetProducts

{
    public record GetProductsQuery(
        int PageNumber = 1,
        int PageSize = 10)
    : IQuery<GetProductsResult>;

    public record GetProductsResult(PaginatedResult<Product> Products);
    internal class GetProductsQueryHandler
        (IDocumentSession session, 
        ILogger<GetProductsQueryHandler> logger)
        :IQueryHandler<GetProductsQuery, GetProductsResult>
    {
        public async Task<GetProductsResult> Handle(GetProductsQuery query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "GetProductsQueryHandler.Hanlde llamado con {@Query}", query);

            var totalCount = await session.Query<Product>().LongCountAsync(cancellationToken);
            var products = await session.Query<Product>()
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);


            var paginatedResult =
                new PaginatedResult<Product>
                {
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    Data = products
                };


            return new GetProductsResult(paginatedResult);
        }
    }
}
