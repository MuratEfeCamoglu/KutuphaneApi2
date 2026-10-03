using KutuphaneApi.Dtos.Categories;
using KutuphaneApi.Entities;

namespace KutuphaneApi.Mappings;

public static class CategoryMappings
{
    public static IQueryable<CategoryDto> SelectDto(this IQueryable<Category> query) =>
        query.Select(c => new CategoryDto(c.Id, c.Name, c.Books.Count));

    public static Category ToEntity(this CreateCategoryRequest request) => new()
    {
        Name = request.Name
    };

    public static void ApplyTo(this UpdateCategoryRequest request, Category category)
    {
        category.Name = request.Name;
    }
}
