using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Data;
using KutuphaneApi.Dtos.Authors;
using KutuphaneApi.Mappings;
using KutuphaneApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Services;

public class AuthorService(
    AppDbContext context,
    IValidator<CreateAuthorRequest> createValidator,
    IValidator<UpdateAuthorRequest> updateValidator) : IAuthorService
{
    public async Task<IReadOnlyList<AuthorDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        // AsNoTracking: sadece okuyacağımız için EF Core'un değişiklik takibini kapatırız; daha hızlı ve az bellek.
        return await context.Authors
            .AsNoTracking()
            .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
            .SelectDto()
            .ToListAsync(cancellationToken);
    }

    public async Task<AuthorDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Authors
            .AsNoTracking()
            .Where(a => a.Id == id)
            .SelectDto()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Yazar", id);
    }

    public async Task<AuthorDto> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken)
    {
        // Kurallara uymayan istekte ValidationException fırlatır; GlobalExceptionHandler bunu 400'e çevirir.
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var author = request.ToEntity();
        context.Authors.Add(author);
        await context.SaveChangesAsync(cancellationToken);

        // SaveChanges sonrası author.Id veritabanının verdiği değerle dolmuştur.
        return await GetByIdAsync(author.Id, cancellationToken);
    }

    public async Task UpdateAsync(int id, UpdateAuthorRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Güncelleme için entity takip edilerek (tracking) okunur; SaveChanges sadece değişen sütunları UPDATE eder.
        var author = await context.Authors.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Yazar", id);

        request.ApplyTo(author);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var author = await context.Authors.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Yazar", id);

        // İş kuralı 8: kitabı olan yazar silinemez.
        if (await context.Books.AnyAsync(b => b.AuthorId == id, cancellationToken))
        {
            throw new BusinessRuleException("Kitabı olan yazar silinemez. Önce yazarın kitaplarını silin veya başka bir yazara aktarın.");
        }

        context.Authors.Remove(author);
        await context.SaveChangesAsync(cancellationToken);
    }
}
