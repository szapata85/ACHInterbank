using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaHeaderConfiguration : IEntityTypeConfiguration<NachaHeader>
{
    public void Configure(EntityTypeBuilder<NachaHeader> builder)
    {
        builder.ToTable("NachaHeaders");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PriorityCode).HasMaxLength(2);
        builder.Property(x => x.ImmediateDestination).HasMaxLength(10);
        // ... agrega validaciones según la especificación NACHA
    }
}
