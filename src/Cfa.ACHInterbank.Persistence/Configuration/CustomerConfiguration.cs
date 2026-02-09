using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.MiddleName).HasMaxLength(100);
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.SecondLastName).HasMaxLength(100);
        builder.Property(c => c.Gender).HasMaxLength(20);
        builder.Property(c => c.PersonType).HasMaxLength(5).IsRequired();
        builder.Property(c => c.CompanyName).HasMaxLength(200);

        builder.Property(c => c.DocumentType).HasMaxLength(10).IsRequired();

        builder.Property(c => c.DocumentNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.AccountNumber).HasMaxLength(50).IsRequired();

        // Unicidad por tipo de documento + número + cuenta
        builder.HasIndex(c => new { c.DocumentType, c.DocumentNumber, c.AccountNumber }).IsUnique();

        builder.HasOne(c => c.DocumentTypeCatalog)
            .WithMany(d => d.Customers)
            .HasForeignKey(c => c.DocumentType)
            .HasPrincipalKey(d => d.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.GenderCatalog)
            .WithMany(g => g.Customers)
            .HasForeignKey(c => c.Gender)
            .HasPrincipalKey(g => g.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.PersonTypeCatalog)
            .WithMany(p => p.Customers)
            .HasForeignKey(c => c.PersonType)
            .HasPrincipalKey(p => p.Code)
            .OnDelete(DeleteBehavior.Restrict);



    }
}
