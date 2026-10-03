using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Data;
using KutuphaneApi.Dtos.Books;
using KutuphaneApi.Entities;
using KutuphaneApi.Mappings;
using KutuphaneApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Services;

public class BookService(
    AppDbContext context,
    IValidator<CreateBookRequest> createValidator,
    IValidator<UpdateBookRequest> updateValidator) : IBookService
{
    public async Task<IReadOnlyList<BookListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .OrderBy(b => b.Title)
            .SelectListItemDto()
            .ToListAsync(cancellationToken);
    }

    public async Task<BookDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .SelectDetailDto()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Kitap", id);
    }

    public async Task<BookDetailDto> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureAuthorExistsAsync(request.AuthorId, cancellationToken);
        await EnsureIsbnIsUniqueAsync(request.Isbn, excludeId: null, cancellationToken);
        var categories = await GetCategoriesAsync(request.CategoryIds, cancellationToken);

        var book = request.ToEntity(categories);
        context.Books.Add(book);
        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(book.Id, cancellationToken);
    }

    public async Task UpdateAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Include: ilişkili koleksiyonu da belleğe yükler. Kategorileri değiştireceğimiz için mevcut listeyi bilmemiz gerekir.
        var book = await context.Books
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException("Kitap", id);

        await EnsureAuthorExistsAsync(request.AuthorId, cancellationToken);
        await EnsureIsbnIsUniqueAsync(request.Isbn, excludeId: id, cancellationToken);

        // İş kuralı 9: stok, o an ödünçte olan kopya sayısının altına düşürülemez.
        var activeLoanCount = await context.Loans
            .CountAsync(l => l.BookId == id && l.ReturnDate == null, cancellationToken);
        if (request.StockCount < activeLoanCount)
        {
            throw new BusinessRuleException(
                $"Stok {activeLoanCount} adetten az olamaz; bu kadar kopya şu anda ödünçte.");
        }

        var categories = await GetCategoriesAsync(request.CategoryIds, cancellationToken);

        request.ApplyTo(book, categories);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var book = await context.Books.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Kitap", id);

        // İş kuralı 7: iade edilmemiş ödüncü olan kitap silinemez.
        if (await context.Loans.AnyAsync(l => l.BookId == id && l.ReturnDate == null, cancellationToken))
        {
            throw new BusinessRuleException("İade edilmemiş ödüncü olan kitap silinemez.");
        }

        // Loan → Book ilişkisi Restrict olduğu için önce kitabın (iade edilmiş) ödünç geçmişini sileriz.
        // İkisi de aynı SaveChanges içinde, yani tek bir transaction'da çalışır.
        var returnedLoans = await context.Loans.Where(l => l.BookId == id).ToListAsync(cancellationToken);
        context.Loans.RemoveRange(returnedLoans);
        context.Books.Remove(book);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAuthorExistsAsync(int authorId, CancellationToken cancellationToken)
    {
        if (!await context.Authors.AnyAsync(a => a.Id == authorId, cancellationToken))
        {
            throw new NotFoundException("Yazar", authorId);
        }
    }

    private async Task EnsureIsbnIsUniqueAsync(string isbn, int? excludeId, CancellationToken cancellationToken)
    {
        if (await context.Books.AnyAsync(b => b.Isbn == isbn && b.Id != excludeId, cancellationToken))
        {
            throw new BusinessRuleException($"'{isbn}' ISBN numarasıyla kayıtlı bir kitap zaten var.");
        }
    }

    private async Task<List<Category>> GetCategoriesAsync(IReadOnlyList<int> categoryIds, CancellationToken cancellationToken)
    {
        var distinctIds = categoryIds.Distinct().ToList();

        // Contains → SQL'de "WHERE Id IN (...)". Her Id için ayrı sorgu atmayız (N+1 yok).
        var categories = await context.Categories
            .Where(c => distinctIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var missingId = distinctIds.Except(categories.Select(c => c.Id)).FirstOrDefault();
        if (missingId != 0)
        {
            throw new NotFoundException("Kategori", missingId);
        }

        return categories;
    }
}
