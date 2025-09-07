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
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(a => a.Information)
            .HasMaxLength(80) // estándar NACHA
            .IsRequired();

        builder.HasOne(a => a.Transaction)
            .WithMany(t => t.Addendas)
            .HasForeignKey(a => a.AchTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


