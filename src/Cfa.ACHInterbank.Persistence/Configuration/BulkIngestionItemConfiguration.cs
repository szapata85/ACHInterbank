using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BulkIngestionItemConfiguration : IEntityTypeConfiguration<BulkIngestionItem>
{
    public void Configure(EntityTypeBuilder<BulkIngestionItem> builder)
    {
        builder.ToTable("BulkIngestionItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference)
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(25)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(1000);

        builder.Property(x => x.RawPayloadJson)
            .IsRequired();

        builder.Property(x => x.NormalizedPayloadJson);

        builder.HasIndex(x => new { x.BatchId, x.ItemIndex })
            .IsUnique();

        builder.HasIndex(x => new { x.BatchId, x.Status });
    }
}
