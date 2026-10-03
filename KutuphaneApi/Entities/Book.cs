namespace KutuphaneApi.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int StockCount { get; set; }

    // Foreign key: Books tablosunda yazarın Id'sini tutan sütun.
    public int AuthorId { get; set; }

    // "null!": EF Core bu alanı veritabanından doldurur; derleyiciye "null kalmayacak" sözü veriyoruz.
    public Author Author { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
