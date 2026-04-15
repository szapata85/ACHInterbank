using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnCodeConfiguration : IEntityTypeConfiguration<AchReturnCode>
{
    public void Configure(EntityTypeBuilder<AchReturnCode> builder)
    {
        builder.ToTable("AchReturnCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RegulatorySource).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
