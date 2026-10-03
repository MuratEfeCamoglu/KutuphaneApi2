using KutuphaneApi.Dtos.Authors;

namespace KutuphaneApi.Services.Interfaces;

// Interface: controller servisin kendisine değil bu sözleşmeye bağımlıdır; DI hangi sınıfın verileceğine karar verir.
public interface IAuthorService
{
    Task<IReadOnlyList<AuthorDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AuthorDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<AuthorDto> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, UpdateAuthorRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
