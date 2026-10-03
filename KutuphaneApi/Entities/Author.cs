namespace KutuphaneApi.Entities;

// Entity: veritabanındaki bir tablonun C# karşılığı. EF Core bu sınıftan "Authors" tablosunu üretir.
public class Author
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? BirthYear { get; set; }

    // Navigation property: ilişkili kayıtlara C# nesnesi üzerinden erişmeyi sağlar (1 yazar → çok kitap).
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
