using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CustomerPhoneConfiguration : IEntityTypeConfiguration<CustomerPhone>
{
    public void Configure(EntityTypeBuilder<CustomerPhone> builder)
    {
        builder.ToTable("CustomerPhones");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PhoneType).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Number).HasMaxLength(30).IsRequired();
        builder.Property(p => p.IsPrimary).HasDefaultValue(false);

        builder.HasOne(p => p.Customer)
         .WithMany(c => c.Phones)
         .HasForeignKey(p => p.CustomerId)
         .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PhoneTypeCatalog)
         .WithMany(c => c.CustomerPhones)
         .HasForeignKey(p => p.PhoneType)
         .HasPrincipalKey(c => c.Code)
         .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.CustomerId, p.IsPrimary })
         .IsUnique();
    }
}
