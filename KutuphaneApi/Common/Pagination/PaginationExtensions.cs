using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Common.Pagination;

public static class PaginationExtensions
{
    // İki sorgu çalışır: biri toplam kayıt sayısı (COUNT), diğeri sadece istenen sayfa (LIMIT/OFFSET).
    // Sorgu bu metoda sıralanmış olarak gelmelidir; sırasız Skip/Take her seferinde farklı sonuç verebilir.
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PagingParameters parameters, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, parameters.Page, parameters.PageSize, totalCount);
    }
}
