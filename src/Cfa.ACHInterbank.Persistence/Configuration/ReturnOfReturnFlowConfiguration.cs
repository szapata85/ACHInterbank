using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ReturnOfReturnFlowConfiguration : IEntityTypeConfiguration<ReturnOfReturnFlow>
{
    public void Configure(EntityTypeBuilder<ReturnOfReturnFlow> builder)
    {
        builder.ToTable("ReturnOfReturnFlows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReasonCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();

        builder.HasOne(x => x.SourceReturnTransaction)
            .WithMany()
            .HasForeignKey(x => x.SourceReturnTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReturnOfReturnTransaction)
            .WithMany()
            .HasForeignKey(x => x.ReturnOfReturnTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CenitCycleExecution)
            .WithMany(x => x.ReturnOfReturnFlows)
            .HasForeignKey(x => x.CenitCycleExecutionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
