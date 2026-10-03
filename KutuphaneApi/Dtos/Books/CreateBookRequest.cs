namespace KutuphaneApi.Dtos.Books;

public record CreateBookRequest(
    string Title,
    string Isbn,
    int PublishedYear,
    int StockCount,
    int AuthorId,
    IReadOnlyList<int> CategoryIds);
