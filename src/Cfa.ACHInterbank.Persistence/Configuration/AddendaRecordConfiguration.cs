using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AddendaRecordConfiguration : IEntityTypeConfiguration<AddendaRecord>
{
    public void Configure(EntityTypeBuilder<AddendaRecord> builder)
    {
        builder.ToTable("AddendaRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentRelatedInformation).HasMaxLength(80);
        builder.Property(x => x.AddendaSequenceNumber).HasMaxLength(4);
        builder.Property(x => x.EntryDetailSequenceNumber).HasMaxLength(7);
    }
}
