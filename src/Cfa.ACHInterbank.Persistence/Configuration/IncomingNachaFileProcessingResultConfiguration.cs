using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaFileProcessingResultConfiguration : IEntityTypeConfiguration<IncomingNachaFileProcessingResult>
{
    public void Configure(EntityTypeBuilder<IncomingNachaFileProcessingResult> builder)
    {
        builder.ToTable("IncomingNachaFileProcessingResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OutcomeStatus)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FailureStage)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ParserWarningsJson).IsRequired();
        builder.Property(x => x.ParserErrorsJson).IsRequired();

        builder.HasIndex(x => new { x.IncomingNachaFileIngestionId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => x.StartedAtUtc);
    }
}
