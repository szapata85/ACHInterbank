using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaProcessingEventConfiguration : IEntityTypeConfiguration<IncomingNachaProcessingEvent>
{
    public void Configure(EntityTypeBuilder<IncomingNachaProcessingEvent> builder)
    {
        builder.ToTable("IncomingNachaProcessingEvents");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EventStatus).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.EvidenceJson).IsRequired();
        builder.Property(x => x.RaisedBy).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => new { x.IncomingNachaFileIngestionId, x.OccurredAtUtc });
        builder.HasIndex(x => x.EventType);

        builder.HasOne(x => x.Ingestion)
            .WithMany()
            .HasForeignKey(x => x.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
