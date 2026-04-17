using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaTransactionLinkConfiguration : IEntityTypeConfiguration<IncomingNachaTransactionLink>
{
    public void Configure(EntityTypeBuilder<IncomingNachaTransactionLink> builder)
    {
        builder.ToTable("IncomingNachaTransactionLinks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LinkType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ConfidenceScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.EvidenceJson)
            .IsRequired();

        builder.Property(x => x.LinkedBy)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(x => new { x.IncomingNachaFileIngestionId, x.EntryDetailId, x.AddendaRecordId, x.AchTransactionId });

        builder.HasOne(x => x.EntryDetail)
            .WithMany()
            .HasForeignKey(x => x.EntryDetailId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AddendaRecord)
            .WithMany()
            .HasForeignKey(x => x.AddendaRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
