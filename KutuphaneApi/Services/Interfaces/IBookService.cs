using KutuphaneApi.Dtos.Books;

namespace KutuphaneApi.Services.Interfaces;

public interface IBookService
{
    Task<IReadOnlyList<BookListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<BookDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<BookDetailDto> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
