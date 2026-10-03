using KutuphaneApi.Common.Pagination;

namespace KutuphaneApi.Dtos.Books;

// GET /api/books?search=...&authorId=...&sortBy=year&sortDirection=desc&page=1&pageSize=10
public record BookQueryParameters : PagingParameters
{
    public string? Search { get; init; }
    public int? AuthorId { get; init; }
    public int? CategoryId { get; init; }
    public bool OnlyAvailable { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
