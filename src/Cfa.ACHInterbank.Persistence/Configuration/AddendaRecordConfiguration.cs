using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AddendaRecordConfiguration : IEntityTypeConfiguration<AddendaRecord>
{
    public void Configure(EntityTypeBuilder<AddendaRecord> builder)
    {
        builder.ToTable("AddendaRecords");

        builder.HasKey(x => x.AddendaID);

        builder.Property(x => x.BusinessType).HasMaxLength(20);
        builder.Property(x => x.CollectorId).HasMaxLength(13);
        builder.Property(x => x.ReceiverCustomerCode).HasMaxLength(30);
        builder.Property(x => x.ServiceDescription).HasMaxLength(15);
        builder.Property(x => x.PaymentRelatedInformation).HasMaxLength(80);
        builder.Property(x => x.ReturnReasonCode).HasMaxLength(5);
        builder.Property(x => x.OriginalTraceNumber).HasMaxLength(15);
        builder.Property(x => x.NewTraceNumber).HasMaxLength(15);
        builder.Property(x => x.AddendumSequence).HasMaxLength(4);
        builder.Property(x => x.EntryDetailSequenceNumber).HasMaxLength(7);
        builder.HasIndex(x => x.EntryDetailId);
        builder.HasIndex(x => new { x.NachaID, x.EntryDetailSequenceNumber, x.AddendumSequence }).IsUnique();
    }
}
