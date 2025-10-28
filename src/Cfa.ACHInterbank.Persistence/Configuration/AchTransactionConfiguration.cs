using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionConfiguration : IEntityTypeConfiguration<AchTransaction>
{
    public void Configure(EntityTypeBuilder<AchTransaction> builder)
    {
        builder.ToTable("AchTransactions");

        builder.HasKey(t => t.Id);

        // 🔹 Guardar el enum como string en BD
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(t => t.AchBatch)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.AchBatchId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

