namespace Catalog.API.Common.Pagination
{
    public record PaginatedRequest(
        int PageNumber = 1,
        int PageSize = 10);
}
