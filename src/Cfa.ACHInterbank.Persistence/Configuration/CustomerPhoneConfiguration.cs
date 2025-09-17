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

        builder.Property(p => p.PhoneType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Number).HasMaxLength(30).IsRequired();
        builder.Property(p => p.IsPrimary).HasDefaultValue(false);

        builder.HasOne(p => p.Customer)
         .WithMany(c => c.Phones)
         .HasForeignKey(p => p.CustomerId)
         .OnDelete(DeleteBehavior.Cascade);

        // Permitir sólo un teléfono primario por cliente (índice filtrado)
        builder.HasIndex(p => new { p.CustomerId, p.IsPrimary })
         .IsUnique()
         .HasFilter("[IsPrimary] = 1");
    }
}
