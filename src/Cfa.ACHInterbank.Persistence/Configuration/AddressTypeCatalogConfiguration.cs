using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class AddressTypeCatalogConfiguration : IEntityTypeConfiguration<AddressTypeCatalog>
{
    public void Configure(EntityTypeBuilder<AddressTypeCatalog> builder)
    {
        builder.ToTable("AddressTypes");
        builder.HasKey(a => a.Code);

        builder.Property(a => a.Code).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(200);

        builder.HasData(
            new AddressTypeCatalog { Code = "CASA", Name = "Casa" },
            new AddressTypeCatalog { Code = "TRABAJO", Name = "Trabajo" },
            new AddressTypeCatalog { Code = "FINCA", Name = "Finca" },
            new AddressTypeCatalog { Code = "CORRESPONDENCIA", Name = "Correspondencia" },
            new AddressTypeCatalog { Code = "PERSONAL", Name = "Personal" }
        );
    }
}
