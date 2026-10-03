namespace KutuphaneApi.Common.Pagination;

// Sayfalı yanıtın ortak şekli. Generic (T) olduğu için kitap, ödünç gibi her liste için kullanılabilir.
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    // Hesaplanan özellik; JSON'a "totalPages" olarak yazılır.
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
