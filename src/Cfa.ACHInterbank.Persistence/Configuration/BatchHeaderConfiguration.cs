using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BatchHeaderConfiguration : IEntityTypeConfiguration<BatchHeader>
{
    public void Configure(EntityTypeBuilder<BatchHeader> builder)
    {
        builder.ToTable("BatchHeaders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName).HasMaxLength(16);
        builder.Property(x => x.CompanyId).HasMaxLength(10);
        builder.Property(x => x.StandardEntryClassCode).HasMaxLength(3);

        builder.HasOne(x => x.NachaHeader)
               .WithMany(x => x.Batches)
               .HasForeignKey(x => x.NachaHeaderId);

        builder.HasMany(x => x.Entries)
               .WithOne(x => x.BatchHeader)
               .HasForeignKey(x => x.BatchHeaderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BatchControl)
               .WithOne(x => x.BatchHeader)
               .HasForeignKey<BatchControl>(x => x.BatchHeaderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
