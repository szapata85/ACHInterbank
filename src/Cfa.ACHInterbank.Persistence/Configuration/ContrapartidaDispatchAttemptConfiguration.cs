using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ContrapartidaDispatchAttemptConfiguration : IEntityTypeConfiguration<ContrapartidaDispatchAttempt>
{
    public void Configure(EntityTypeBuilder<ContrapartidaDispatchAttempt> builder)
    {
        builder.ToTable("ContrapartidaDispatchAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Result)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.TriggeredBy)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ExternalResponseCode)
            .HasMaxLength(20);

        builder.Property(x => x.ExternalResponseMessage)
            .HasMaxLength(1000);

        builder.Property(x => x.ErrorCode)
            .HasMaxLength(50);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.RequestPayloadXml);
        builder.Property(x => x.ResponsePayloadXml);

        builder.HasIndex(x => new { x.DispatchItemId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => new { x.DispatchBatchId, x.CreatedAt });
        builder.HasIndex(x => x.Result);
    }
}
