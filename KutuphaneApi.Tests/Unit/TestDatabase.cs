using KutuphaneApi.Data;
using KutuphaneApi.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Tests.Unit;

// Her test için bellekte (in-memory) yaşayan gerçek bir SQLite veritabanı.
// Mock yerine gerçek veritabanı kullandığımız için EF Core sorguları, ilişkiler ve kısıtlar da test edilmiş olur.
public sealed class TestDatabase : IDisposable
{
    // ":memory:" veritabanı bağlantı kapanınca silinir; bu yüzden bağlantıyı test boyunca açık tutarız.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDatabase()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var context = CreateContext();

        // EnsureCreated: migration'ları değil, güncel modeli doğrudan tablo olarak oluşturur. Testler için yeterli ve hızlı.
        context.Database.EnsureCreated();
    }

    // Test verisini bir context ile ekleyip servisi başka bir context ile çalıştırmak,
    // EF Core'un bellekteki takip (tracking) önbelleğinin sonucu etkilemesini önler.
    public AppDbContext CreateContext() => new(_options);

    public Author AddAuthor(string lastName = "Yazar")
    {
        using var context = CreateContext();
        var author = new Author { FirstName = "Test", LastName = lastName };
        context.Authors.Add(author);
        context.SaveChanges();
        return author;
    }

    public Book AddBook(int authorId, int stockCount = 1, string isbn = "9780000000001")
    {
        using var context = CreateContext();
        var book = new Book
        {
            Title = $"Kitap {isbn}",
            Isbn = isbn,
            PublishedYear = 2000,
            StockCount = stockCount,
            AuthorId = authorId
        };
        context.Books.Add(book);
        context.SaveChanges();
        return book;
    }

    public Member AddMember(string email = "uye@example.com")
    {
        using var context = CreateContext();
        var member = new Member
        {
            FirstName = "Test",
            LastName = "Üye",
            Email = email,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Members.Add(member);
        context.SaveChanges();
        return member;
    }

    public void Dispose() => _connection.Dispose();
}
