using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class IntegrationResponseCodeConfiguration : IEntityTypeConfiguration<IntegrationResponseCode>
{
    public void Configure(EntityTypeBuilder<IntegrationResponseCode> builder)
    {
        builder.ToTable("IntegrationResponseCodes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BusinessStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.TargetTransactionState).HasMaxLength(60).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => new { x.Source, x.MethodId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_IntegrationResponseCodes_Source_Method_Code");
        builder.HasIndex(x => new { x.Category, x.IsActive });
        builder.HasIndex(x => new { x.EffectiveFromUtc, x.EffectiveToUtc });

        builder.HasOne(x => x.Method)
            .WithMany(x => x.ResponseCodes)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
