namespace KutuphaneApi.Dtos.Books;

// Liste yanıtı: detaydan daha az alan, listede her satır için gereken kadar bilgi.
public record BookListItemDto(
    int Id,
    string Title,
    string Isbn,
    int PublishedYear,
    string AuthorName,
    int AvailableCopies);
