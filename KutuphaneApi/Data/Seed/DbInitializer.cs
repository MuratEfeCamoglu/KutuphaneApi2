using KutuphaneApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Data.Seed;

// Geliştirme ortamında uygulama açılırken veritabanını hazırlar: bekleyen migration'ları uygular ve boşsa örnek veri ekler.
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // DbContext "scoped" bir servistir; uygulama başlarken HTTP isteği olmadığı için scope'u kendimiz açarız.
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Veritabanı dosyası yoksa oluşturur ve tüm migration'ları sırayla uygular.
        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Authors.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = TimeProvider.System.GetUtcNow().UtcDateTime;

        var novel = new Category { Name = "Roman" };
        var classic = new Category { Name = "Klasik" };
        var scienceFiction = new Category { Name = "Bilim Kurgu" };
        var history = new Category { Name = "Tarih" };

        var orhan = new Author { FirstName = "Orhan", LastName = "Pamuk", BirthYear = 1952 };
        var sabahattin = new Author { FirstName = "Sabahattin", LastName = "Ali", BirthYear = 1907 };
        var orwell = new Author { FirstName = "George", LastName = "Orwell", BirthYear = 1903 };
        var asimov = new Author { FirstName = "Isaac", LastName = "Asimov", BirthYear = 1920 };
        var ilber = new Author { FirstName = "İlber", LastName = "Ortaylı", BirthYear = 1947 };

        var books = new List<Book>
        {
            new() { Title = "Kar", Isbn = "9789750507977", PublishedYear = 2002, StockCount = 2, Author = orhan, Categories = { novel } },
            new() { Title = "Masumiyet Müzesi", Isbn = "9789750513473", PublishedYear = 2008, StockCount = 1, Author = orhan, Categories = { novel } },
            new() { Title = "Kürk Mantolu Madonna", Isbn = "9789753638029", PublishedYear = 1943, StockCount = 3, Author = sabahattin, Categories = { novel, classic } },
            new() { Title = "İçimizdeki Şeytan", Isbn = "9789753638036", PublishedYear = 1940, StockCount = 1, Author = sabahattin, Categories = { novel, classic } },
            new() { Title = "1984", Isbn = "9789750718533", PublishedYear = 1949, StockCount = 4, Author = orwell, Categories = { novel, classic, scienceFiction } },
            new() { Title = "Hayvan Çiftliği", Isbn = "9789750719387", PublishedYear = 1945, StockCount = 2, Author = orwell, Categories = { novel, classic } },
            new() { Title = "Vakıf", Isbn = "9786053757818", PublishedYear = 1951, StockCount = 2, Author = asimov, Categories = { scienceFiction } },
            new() { Title = "Ben, Robot", Isbn = "9786053757825", PublishedYear = 1950, StockCount = 1, Author = asimov, Categories = { scienceFiction } },
            new() { Title = "Osmanlı'yı Yeniden Keşfetmek", Isbn = "9789752637849", PublishedYear = 2006, StockCount = 2, Author = ilber, Categories = { history } },
        };

        var members = new List<Member>
        {
            new() { FirstName = "Ayşe", LastName = "Yılmaz", Email = "ayse.yilmaz@example.com", PhoneNumber = "05551112233", CreatedAt = now.AddDays(-60) },
            new() { FirstName = "Mehmet", LastName = "Demir", Email = "mehmet.demir@example.com", CreatedAt = now.AddDays(-45) },
            new() { FirstName = "Zeynep", LastName = "Kaya", Email = "zeynep.kaya@example.com", PhoneNumber = "05324445566", CreatedAt = now.AddDays(-30) },
        };

        // Üç farklı durumdan birer örnek: iade edilmiş, aktif ve gecikmiş ödünç.
        var loans = new List<Loan>
        {
            new() { Book = books[0], Member = members[0], LoanDate = now.AddDays(-40), DueDate = now.AddDays(-26), ReturnDate = now.AddDays(-30) },
            new() { Book = books[2], Member = members[0], LoanDate = now.AddDays(-3), DueDate = now.AddDays(11) },
            new() { Book = books[4], Member = members[1], LoanDate = now.AddDays(-20), DueDate = now.AddDays(-6) },
        };

        // Kitaplar yazar ve kategorilere, ödünçler kitap ve üyelere nesne olarak bağlı olduğu için
        // EF Core hepsini doğru sırayla ekler ve foreign key'leri kendisi doldurur.
        context.Books.AddRange(books);
        context.Members.AddRange(members);
        context.Loans.AddRange(loans);
        await context.SaveChangesAsync(cancellationToken);
    }
}
