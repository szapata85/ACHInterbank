using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaRecordDefinitionConfiguration : IEntityTypeConfiguration<NachaRecordDefinition>
{
    private static readonly DateTimeOffset SeedTimestamp = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<NachaRecordDefinition> builder)
    {
        builder.ToTable("NachaRecordDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecordCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(x => x.Sequence)
            .IsRequired();

        builder.Property(x => x.SourceName)
            .HasMaxLength(200);

        builder.Property(x => x.FilterKey)
            .HasMaxLength(50);

        builder.Property(x => x.IsEnabled)
            .HasDefaultValue(true);

        builder.HasData(
            new NachaRecordDefinition
            {
                Id = 1,
                RecordCode = "1",
                Sequence = 10,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new NachaRecordDefinition
            {
                Id = 2,
                RecordCode = "5",
                Sequence = 20,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new NachaRecordDefinition
            {
                Id = 3,
                RecordCode = "6",
                Sequence = 30,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransaction),
                FilterKey = "BatchId",
                IsEnabled = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new NachaRecordDefinition
            {
                Id = 4,
                RecordCode = "7",
                Sequence = 40,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransactionAddenda),
                FilterKey = "BatchId",
                IsEnabled = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new NachaRecordDefinition
            {
                Id = 5,
                RecordCode = "8",
                Sequence = 50,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new NachaRecordDefinition
            {
                Id = 6,
                RecordCode = "9",
                Sequence = 60,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });
    }
}
