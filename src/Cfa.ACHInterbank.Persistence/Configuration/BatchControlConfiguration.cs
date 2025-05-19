using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BatchControlConfiguration : IEntityTypeConfiguration<BatchControl>
{
    public void Configure(EntityTypeBuilder<BatchControl> builder)
    {
        builder.ToTable("BatchControls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyId).HasMaxLength(10);
        builder.Property(x => x.OdfiIdentification).HasMaxLength(8);
    }
}
