using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaFileNamingRuleConfiguration : IEntityTypeConfiguration<NachaFileNamingRule>
{
    public void Configure(EntityTypeBuilder<NachaFileNamingRule> builder)
    {
        builder.ToTable("NachaFileNamingRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileDirection)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.NamePattern).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Extension).HasMaxLength(20);
        builder.Property(x => x.InternalFileIdMappingMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.NormativeSource).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormativeReference).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.DailySequenceMin).IsRequired();
        builder.Property(x => x.DailySequenceMax).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.RequiresNameHeaderEntityMatch).HasDefaultValue(true);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceFinancialInstitution)
            .WithMany()
            .HasForeignKey(x => x.SourceFinancialInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClearingHouseId, x.FileDirection, x.IsActive, x.EffectiveFrom })
            .HasDatabaseName("IX_NFNR_RuleLookup");
    }
}
