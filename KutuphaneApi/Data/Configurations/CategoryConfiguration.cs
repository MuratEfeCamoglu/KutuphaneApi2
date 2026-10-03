using KutuphaneApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KutuphaneApi.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // NOCASE: SQLite'ta karşılaştırmayı büyük/küçük harf duyarsız yapar ("Roman" ile "roman" aynı sayılır).
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");

        // Benzersiz index: aynı adla ikinci kategori eklenemez.
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
