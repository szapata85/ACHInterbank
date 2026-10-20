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
        builder.Property(x => x.SoapMethodName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SoapEndpoint).HasMaxLength(500);
        builder.Property(x => x.ExecutionMode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.MappingSnapshotHash).HasMaxLength(200);
        builder.Property(x => x.RequestHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ResponseHash).HasMaxLength(200);
        builder.Property(x => x.SoapResponseCode).HasMaxLength(80);
        builder.Property(x => x.SoapResponseDescription).HasMaxLength(4000);
        builder.Property(x => x.SoapTechnicalStatus).HasMaxLength(80).IsRequired();
        builder.Property(x => x.TransportStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.BusinessStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.TechnicalException).HasMaxLength(4000);
        builder.Property(x => x.ResponseCode).HasMaxLength(80);
        builder.Property(x => x.ResponseMessage).HasMaxLength(4000);
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ProcessingStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.BusinessOutcome).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResultCode).HasMaxLength(20);
        builder.Property(x => x.ResultDescription).HasMaxLength(500);
        builder.Property(x => x.ResultSource).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ExternalTransactionId).HasMaxLength(120);
        builder.Property(x => x.TechnicalErrorCode).HasMaxLength(100);
        builder.Property(x => x.TechnicalErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => new { x.DispatchQueueId, x.StartedAtUtc });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.DispatchQueueId);
        builder.HasIndex(x => x.SoapMethodName);
        builder.HasIndex(x => x.StartedAtUtc);
        builder.HasIndex(x => x.SoapResponseCode);
        builder.HasIndex(x => x.SoapTechnicalStatus);
        builder.HasIndex(x => x.ResponseCatalogId);
        builder.HasIndex(x => new { x.BusinessStatus, x.ProcessedAtUtc });
        builder.HasIndex(x => new { x.EntryDetailId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => new { x.ClearingHouseId, x.ResultCode });
        builder.HasIndex(x => x.AchReturnCodeId);

        builder.HasOne(x => x.ResponseCatalog)
            .WithMany()
            .HasForeignKey(x => x.ResponseCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DispatchQueue)
            .WithMany(x => x.Executions)
            .HasForeignKey(x => x.DispatchQueueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EntryDetail)
            .WithMany(x => x.ProcessingAttempts)
            .HasForeignKey(x => x.EntryDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AchReturnCode)
            .WithMany()
            .HasForeignKey(x => x.AchReturnCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
