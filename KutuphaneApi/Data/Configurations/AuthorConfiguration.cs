using KutuphaneApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KutuphaneApi.Data.Configurations;

// Fluent API: tablo kurallarını (uzunluk, zorunluluk, index, ilişki) entity sınıfını kirletmeden ayrı bir yerde tanımlar.
public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.Property(a => a.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(100).IsRequired();
    }
}
