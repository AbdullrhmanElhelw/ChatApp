namespace ChatApp.Application.Common.Models;

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public IReadOnlyCollection<T> Data { get; set; } = Array.Empty<T>();
    public PaginationMetadata Pagination { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static PagedResponse<T> Create(IReadOnlyCollection<T> items, int pageIndex, int pageSize, int totalCount, string? nextCursor = null)
    {
        return new PagedResponse<T>
        {
            Success = true,
            Data = items,
            Pagination = new PaginationMetadata
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = pageIndex * pageSize < totalCount,
                HasPreviousPage = pageIndex > 1,
                NextCursor = nextCursor
            },
            Timestamp = DateTime.UtcNow
        };
    }
}

public class PaginationMetadata
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? NextCursor { get; set; }
}
