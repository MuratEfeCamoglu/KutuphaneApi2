using KutuphaneApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KutuphaneApi.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Isbn).HasMaxLength(13).IsFixedLength().IsRequired();
        builder.HasIndex(b => b.Isbn).IsUnique();

        // Check constraint: veritabanı seviyesinde son savunma hattı; stok negatif olamaz.
        builder.ToTable(t => t.HasCheckConstraint("CK_Books_StockCount", "\"StockCount\" >= 0"));

        // Restrict: kitabı olan yazar silinmeye çalışılırsa veritabanı hata verir (İş kuralı 8).
        builder.HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Çoka-çok: EF Core "BookCategory" ara tablosunu otomatik oluşturur.
        builder.HasMany(b => b.Categories)
            .WithMany(c => c.Books)
            .UsingEntity(j => j.ToTable("BookCategories"));
    }
}
