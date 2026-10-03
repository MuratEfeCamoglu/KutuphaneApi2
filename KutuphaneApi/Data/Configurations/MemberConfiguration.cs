using KutuphaneApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KutuphaneApi.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(200).IsRequired().UseCollation("NOCASE");
        builder.Property(m => m.PhoneNumber).HasMaxLength(20);
        builder.HasIndex(m => m.Email).IsUnique();
    }
}
