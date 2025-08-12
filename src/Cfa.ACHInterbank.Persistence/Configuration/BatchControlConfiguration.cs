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
        builder.Property(x => x.CodAutMessage).HasMaxLength(8);

        builder.Property(p => p.TotalCreditAmount).HasColumnType("money");

        builder.Property(p => p.TotalDebitAmount).HasColumnType("money");
    }
}
