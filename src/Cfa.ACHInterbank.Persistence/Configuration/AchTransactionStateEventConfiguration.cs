using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionStateEventConfiguration : IEntityTypeConfiguration<AchTransactionStateEvent>
{
    public void Configure(EntityTypeBuilder<AchTransactionStateEvent> builder)
    {
        builder.ToTable("AchTransactionStateEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromState)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ToState)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ReasonCode)
            .HasMaxLength(20);

        builder.Property(x => x.PayloadJson);

        builder.HasIndex(x => x.AchTransactionId);

        builder.HasOne(x => x.AchTransaction)
            .WithMany(x => x.StateEvents)
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
