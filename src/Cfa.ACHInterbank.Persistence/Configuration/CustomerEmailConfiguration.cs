using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CustomerEmailConfiguration : IEntityTypeConfiguration<CustomerEmail>
{
    public void Configure(EntityTypeBuilder<CustomerEmail> builder)
    {
        builder.ToTable("CustomerEmails");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EmailType).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Address).HasMaxLength(160).IsRequired();
        builder.Property(m => m.IsPrimary).HasDefaultValue(false);

        builder.HasOne(m => m.Customer)
         .WithMany(c => c.Emails)
         .HasForeignKey(m => m.CustomerId)
         .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.EmailTypeCatalog)
         .WithMany(e => e.CustomerEmails)
         .HasForeignKey(m => m.EmailType)
         .HasPrincipalKey(e => e.Code)
         .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.CustomerId, m.IsPrimary })
         .IsUnique();
    }
}
