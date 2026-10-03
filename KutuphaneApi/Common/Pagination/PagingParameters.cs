namespace KutuphaneApi.Common.Pagination;

// Sayfalanan tüm listelerin ortak sorgu parametreleri. ?page=2&pageSize=20 gibi query string'den doldurulur.
public abstract record PagingParameters
{
    public const int MaxPageSize = 50;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
