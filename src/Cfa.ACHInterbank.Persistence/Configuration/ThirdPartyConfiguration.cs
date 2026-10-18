using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class ThirdPartyConfiguration : IEntityTypeConfiguration<ThirdParty>
{
    public void Configure(EntityTypeBuilder<ThirdParty> builder)
    {
        builder.ToTable("ThirdParties");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PersonType)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.DocumentNumberNormalized)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.CanonicalThirdParties)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DocumentTypeCatalog)
            .WithMany()
            .HasForeignKey(x => x.DocumentType)
            .HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PersonTypeCatalog)
            .WithMany()
            .HasForeignKey(x => x.PersonType)
            .HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
            {
                x.CustomerId,
                x.DocumentType,
                x.DocumentNumberNormalized
            })
            .IsUnique()
            .HasDatabaseName("UX_ThirdParties_Customer_Document");
    }
}
