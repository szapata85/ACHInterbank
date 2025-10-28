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

        builder.Property(a => a.Information)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(a => a.Transaction)
            .WithMany(t => t.Addendas)
            .HasForeignKey(a => a.AchTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}


