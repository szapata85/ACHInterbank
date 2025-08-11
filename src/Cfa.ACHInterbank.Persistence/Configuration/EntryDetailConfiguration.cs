using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class EntryDetailConfiguration : IEntityTypeConfiguration<EntryDetail>
{
    public void Configure(EntityTypeBuilder<EntryDetail> builder)
    {
        builder.ToTable("EntryDetails");

        builder.HasKey(x => x.EntryDetailID);

        builder.Property(x => x.AccountNumber).HasMaxLength(17);
        builder.Property(x => x.RecipIdNumber).HasMaxLength(15);
        builder.Property(x => x.RecipUserName).HasMaxLength(22);

        //builder.HasOne(x => x.BatchHeader)
        //       .WithMany(x => x.Entries)
        //       .HasForeignKey(x => x.BatchHeaderId);

        //builder.HasMany(x => x.AddendaRecords)
        //       .WithOne(x => x.EntryDetail)
        //       .HasForeignKey(x => x.EntryDetailId)
        //       .OnDelete(DeleteBehavior.Cascade);
    }
}
