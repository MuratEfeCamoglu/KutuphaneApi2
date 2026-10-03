namespace KutuphaneApi.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Çoka-çok ilişki: iki tarafta da koleksiyon olunca EF Core ara tabloyu kendisi oluşturur.
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
