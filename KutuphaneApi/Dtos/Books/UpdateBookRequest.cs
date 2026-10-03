namespace KutuphaneApi.Dtos.Books;

public record UpdateBookRequest(
    string Title,
    string Isbn,
    int PublishedYear,
    int StockCount,
    int AuthorId,
    IReadOnlyList<int> CategoryIds);
