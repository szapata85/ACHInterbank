using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class EmailTypeCatalogConfiguration : IEntityTypeConfiguration<EmailTypeCatalog>
{
    public void Configure(EntityTypeBuilder<EmailTypeCatalog> builder)
    {
        builder.ToTable("EmailTypes");
        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(200);

        builder.HasData(
            new EmailTypeCatalog { Code = "PERSONAL", Name = "Personal" },
            new EmailTypeCatalog { Code = "TRABAJO", Name = "Trabajo" },
            new EmailTypeCatalog { Code = "OTRO", Name = "Otro" }
        );
    }
}
