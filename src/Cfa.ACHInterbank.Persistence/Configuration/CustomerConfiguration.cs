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
        builder.Property(c => c.Gender).HasConversion<string>().HasMaxLength(20);

        // Guardar enum como string (opcional, útil para lectura)
        builder.Property(c => c.DocumentType).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.Property(c => c.DocumentNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.AccountNumber).HasMaxLength(50).IsRequired();

        // Unicidad por tipo de documento + número
        builder.HasIndex(c => new { c.DocumentType, c.DocumentNumber }).IsUnique();



    }
}
