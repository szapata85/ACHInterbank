using Cfa.ACHInterbank.Domain.Models.ACH.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class ClaimTransactionConfiguration : IEntityTypeConfiguration<ClaimTransaction>
{
    public void Configure(EntityTypeBuilder<ClaimTransaction> builder)
    {
        builder.ToTable("ClaimTransactions");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ClaimCase)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.ClaimCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClaimCaseId, x.AchTransactionId })
            .IsUnique()
            .HasDatabaseName("UX_ClaimTransactions_ClaimCase_AchTransaction");

        builder.HasIndex(x => x.AchTransactionId)
            .HasDatabaseName("IX_ClaimTransactions_AchTransactionId");
    }
}
