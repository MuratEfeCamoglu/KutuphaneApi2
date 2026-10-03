namespace KutuphaneApi.Dtos.Books;

// Detay yanıtı: yazar ve kategoriler iç içe küçük DTO'lar olarak döner.
public record BookDetailDto(
    int Id,
    string Title,
    string Isbn,
    int PublishedYear,
    int StockCount,
    int AvailableCopies,
    BookAuthorDto Author,
    IReadOnlyList<BookCategoryDto> Categories);

public record BookAuthorDto(int Id, string FullName);

public record BookCategoryDto(int Id, string Name);
