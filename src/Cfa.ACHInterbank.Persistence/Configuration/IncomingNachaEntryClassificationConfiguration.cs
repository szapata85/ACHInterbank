using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaEntryClassificationConfiguration : IEntityTypeConfiguration<IncomingNachaEntryClassification>
{
    public void Configure(EntityTypeBuilder<IncomingNachaEntryClassification> builder)
    {
        builder.ToTable("IncomingNachaEntryClassifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.FunctionalClass).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.EligibilityStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.PrenoteStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.OriginalTraceRef).HasMaxLength(30);
        builder.Property(x => x.ReturnReasonCode).HasMaxLength(10);
        builder.Property(x => x.BusinessMeaning).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ClassifierVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ClassificationEvidenceJson).IsRequired();

        builder.HasIndex(x => new { x.IncomingNachaFileIngestionId, x.EntryDetailId, x.AddendaRecordId }).IsUnique();
        builder.HasIndex(x => new { x.FunctionalClass, x.EligibilityStatus });

        builder.HasOne(x => x.Ingestion)
            .WithMany()
            .HasForeignKey(x => x.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EntryDetail)
            .WithMany(x => x.Classifications)
            .HasForeignKey(x => x.EntryDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AddendaRecord)
            .WithMany()
            .HasForeignKey(x => x.AddendaRecordId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
