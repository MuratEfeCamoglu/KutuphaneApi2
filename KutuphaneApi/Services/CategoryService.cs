using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Data;
using KutuphaneApi.Dtos.Categories;
using KutuphaneApi.Mappings;
using KutuphaneApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Services;

public class CategoryService(
    AppDbContext context,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<UpdateCategoryRequest> updateValidator) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .SelectDto()
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .SelectDto()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Kategori", id);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var category = request.ToEntity();
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(category.Id, cancellationToken);
    }

    public async Task UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var category = await context.Categories.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Kategori", id);

        await EnsureNameIsUniqueAsync(request.Name, excludeId: id, cancellationToken);

        request.ApplyTo(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Kategori", id);

        // Kategori silinince ara tablodaki (BookCategories) satırlar da silinir; kitapların kendisi silinmez.
        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        // Name sütunu NOCASE olduğu için bu karşılaştırma SQL'de büyük/küçük harf duyarsız yapılır.
        // Güncellemede kaydın kendi adı çakışma sayılmasın diye kendi Id'si hariç tutulur.
        var exists = await context.Categories
            .AnyAsync(c => c.Name == name && c.Id != excludeId, cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException($"'{name}' adında bir kategori zaten var.");
        }
    }
}
