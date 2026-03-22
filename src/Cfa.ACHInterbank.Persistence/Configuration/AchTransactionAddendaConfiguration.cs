using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionAddendaConfiguration : IEntityTypeConfiguration<AchTransactionAddenda>
{
    public void Configure(EntityTypeBuilder<AchTransactionAddenda> builder)
    {
        builder.ToTable("AchTransactionAddenda");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AddendaType)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.BusinessType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Information)
            .HasMaxLength(500);

        builder.Property(a => a.Purpose).HasMaxLength(10);
        builder.Property(a => a.Reference).HasMaxLength(53);
        builder.Property(a => a.CollectorId).HasMaxLength(13);
        builder.Property(a => a.ReceiverCustomerCode).HasMaxLength(30);
        builder.Property(a => a.ServiceDescription).HasMaxLength(15);
        builder.Property(a => a.ReturnReasonCode).HasMaxLength(4);
        builder.Property(a => a.OriginalTraceNumber).HasMaxLength(15);
        builder.Property(a => a.NewTraceNumber).HasMaxLength(15);

        builder.HasOne(a => a.Transaction)
            .WithMany(t => t.Addendas)
            .HasForeignKey(a => a.AchTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}

