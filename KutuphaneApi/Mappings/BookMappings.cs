using KutuphaneApi.Dtos.Books;
using KutuphaneApi.Entities;

namespace KutuphaneApi.Mappings;

public static class BookMappings
{
    // Müsait kopya veritabanında saklanmaz: stok − iade edilmemiş ödünç sayısı. SQL'de COUNT alt sorgusuna çevrilir.
    // Yazar ve kategoriler de aynı tek sorguda JOIN ile gelir; Include gerekmez çünkü Select sadece istenen alanları okur.
    public static IQueryable<BookDetailDto> SelectDetailDto(this IQueryable<Book> query) =>
        query.Select(b => new BookDetailDto(
            b.Id,
            b.Title,
            b.Isbn,
            b.PublishedYear,
            b.StockCount,
            b.StockCount - b.Loans.Count(l => l.ReturnDate == null),
            new BookAuthorDto(b.Author.Id, b.Author.FirstName + " " + b.Author.LastName),
            b.Categories
                .OrderBy(c => c.Name)
                .Select(c => new BookCategoryDto(c.Id, c.Name))
                .ToList()));

    public static IQueryable<BookListItemDto> SelectListItemDto(this IQueryable<Book> query) =>
        query.Select(b => new BookListItemDto(
            b.Id,
            b.Title,
            b.Isbn,
            b.PublishedYear,
            b.Author.FirstName + " " + b.Author.LastName,
            b.StockCount - b.Loans.Count(l => l.ReturnDate == null)));

    public static Book ToEntity(this CreateBookRequest request, IEnumerable<Category> categories) => new()
    {
        Title = request.Title,
        Isbn = request.Isbn,
        PublishedYear = request.PublishedYear,
        StockCount = request.StockCount,
        AuthorId = request.AuthorId,
        Categories = categories.ToList()
    };

    public static void ApplyTo(this UpdateBookRequest request, Book book, IEnumerable<Category> categories)
    {
        book.Title = request.Title;
        book.Isbn = request.Isbn;
        book.PublishedYear = request.PublishedYear;
        book.StockCount = request.StockCount;
        book.AuthorId = request.AuthorId;

        // Kategori listesini tamamen yenileriz; EF Core ara tablodan çıkanları siler, yenileri ekler.
        book.Categories.Clear();
        foreach (var category in categories)
        {
            book.Categories.Add(category);
        }
    }
}
