using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaIntegrationExecutionConfiguration : IEntityTypeConfiguration<IncomingNachaIntegrationExecution>
{
    public void Configure(EntityTypeBuilder<IncomingNachaIntegrationExecution> builder)
    {
        builder.ToTable("IncomingNachaIntegrationExecution");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MethodName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MappingSnapshotHash).HasMaxLength(200);
        builder.Property(x => x.RequestHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ResponseHash).HasMaxLength(200);
        builder.Property(x => x.ResponseCode).HasMaxLength(80);
        builder.Property(x => x.ResponseMessage).HasMaxLength(4000);
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => new { x.DispatchQueueId, x.StartedAtUtc });
        builder.HasIndex(x => x.CorrelationId);

        builder.HasOne(x => x.DispatchQueue)
            .WithMany(x => x.Executions)
            .HasForeignKey(x => x.DispatchQueueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
