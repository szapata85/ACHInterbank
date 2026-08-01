using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BatchControlConfiguration : IEntityTypeConfiguration<BatchControl>
{
    public void Configure(EntityTypeBuilder<BatchControl> builder)
    {
        builder.ToTable("BatchControls");

        builder.HasKey(x => x.BatchControlID);

        builder.Property(x => x.IdUserOrig).HasMaxLength(10);
        builder.Property(x => x.CodAutMessage).HasMaxLength(19);
        builder.Property(x => x.Reserved).HasMaxLength(6);
        builder.Property(x => x.IdOrigEntity).HasMaxLength(8);
        builder.Property(x => x.BatchNumber).HasMaxLength(7);

        builder.Property(p => p.TotalCreditAmount).HasPrecision(18, 2);
        builder.Property(p => p.TotalDebitAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.BatchHeaderId).IsUnique();
    }
}
