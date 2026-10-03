using KutuphaneApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Data;

// DbContext: veritabanıyla konuşan ana sınıf. LINQ sorgularını SQL'e çevirir ve değişiklikleri kaydeder.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Her DbSet bir tabloyu temsil eder.
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bu projedeki tüm IEntityTypeConfiguration<T> sınıflarını bulup uygular (Configurations/ klasörü).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite tarihin UTC olduğu bilgisini saklamaz. Okurken DateTimeKind.Utc olarak işaretleyen dönüştürücü.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
