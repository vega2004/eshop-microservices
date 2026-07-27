namespace Catalog.API.Common.Pagination
{
    public class PaginatedResult<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; init; }
        public long TotalCount { get; init; }
        public IEnumerable<T> Data { get; init; }
        =Enumerable.Empty<T>();
    }
}
