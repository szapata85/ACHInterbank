using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AddressType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(100).HasDefaultValue("Colombia");
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();

        builder.HasOne(a => a.Customer)
         .WithMany(c => c.Addresses)
         .HasForeignKey(a => a.CustomerId)
         .OnDelete(DeleteBehavior.Cascade);

        // Índice filtrado opcional si quieres solo UNA dirección primaria:
        //builder.HasIndex(a => new { a.CustomerId, a.IsPrimary })
        // .IsUnique()
        // .HasFilter("[IsPrimary] = 1");
    }
}
