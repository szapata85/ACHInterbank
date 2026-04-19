using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.ACHConfig;

public class CfgProfileConfiguration : IEntityTypeConfiguration<CfgProfile>
{
    public void Configure(EntityTypeBuilder<CfgProfile> builder)
    {
        builder.ToTable("CfgProfile", t =>
        {
            t.HasCheckConstraint("CK_CfgProfile_VersionMajor_Positive", "\"VersionMajor\" >= 1");
            t.HasCheckConstraint("CK_CfgProfile_VersionMinor_NonNegative", "\"VersionMinor\" >= 0");
            t.HasCheckConstraint("CK_CfgProfile_EffectiveRange", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProfileCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.PublishedBy).HasMaxLength(120);

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => x.ProfileCode).IsUnique();
        builder.HasIndex(x => new { x.ClearingHouseId, x.FlowTypeId, x.DirectionId, x.ServiceClassId, x.VersionMajor, x.VersionMinor })
            .IsUnique();
        builder.HasIndex(x => new { x.StatusId, x.EffectiveFrom, x.EffectiveTo });
        builder.HasIndex(x => new { x.ClearingHouseId, x.FlowTypeId, x.DirectionId, x.ServiceClassId, x.StatusId, x.ContextPriority });

        builder.HasOne(x => x.ClearingHouse)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FlowType)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.FlowTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Direction)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.DirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ServiceClass)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.ServiceClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupersedesProfile)
            .WithMany(x => x.SupersededByProfiles)
            .HasForeignKey(x => x.SupersedesProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgProfileTagConfiguration : IEntityTypeConfiguration<CfgProfileTag>
{
    public void Configure(EntityTypeBuilder<CfgProfileTag> builder)
    {
        builder.ToTable("CfgProfileTag");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TagKey).HasMaxLength(60).IsRequired();
        builder.Property(x => x.TagValue).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.ProfileId, x.TagKey, x.TagValue }).IsUnique();

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.Tags)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CfgProfileRecordConfiguration : IEntityTypeConfiguration<CfgProfileRecord>
{
    public void Configure(EntityTypeBuilder<CfgProfileRecord> builder)
    {
        builder.ToTable("CfgProfileRecord", t =>
        {
            t.HasCheckConstraint("CK_CfgProfileRecord_MinOccurs_NonNegative", "\"MinOccurs\" >= 0");
            t.HasCheckConstraint("CK_CfgProfileRecord_MaxOccurs_Valid", "\"MaxOccurs\" IS NULL OR \"MaxOccurs\" >= \"MinOccurs\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceStrategy).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.ProfileId, x.RecordCodeId, x.Sequence }).IsUnique();
        builder.HasIndex(x => new { x.ProfileId, x.Sequence });

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.Records)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RecordCode)
            .WithMany(x => x.ProfileRecords)
            .HasForeignKey(x => x.RecordCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LayoutVariant)
            .WithMany(x => x.ProfileRecords)
            .HasForeignKey(x => x.LayoutVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SemanticRuleSet)
            .WithMany(x => x.ProfileRecords)
            .HasForeignKey(x => x.SemanticRuleSetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgLayoutVariantConfiguration : IEntityTypeConfiguration<CfgLayoutVariant>
{
    public void Configure(EntityTypeBuilder<CfgLayoutVariant> builder)
    {
        builder.ToTable("CfgLayoutVariant", t =>
        {
            t.HasCheckConstraint("CK_CfgLayoutVariant_TotalLength_Positive", "\"TotalLength\" > 0");
            t.HasCheckConstraint("CK_CfgLayoutVariant_Priority_NonNegative", "\"Priority\" >= 0");
            t.HasCheckConstraint("CK_CfgLayoutVariant_EffectiveRange", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VariantCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProfileId, x.RecordCodeId, x.VariantCode }).IsUnique();
        builder.HasIndex(x => new { x.ProfileId, x.RecordCodeId, x.StatusId, x.EffectiveFrom, x.EffectiveTo, x.Priority });

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.LayoutVariants)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RecordCode)
            .WithMany(x => x.LayoutVariants)
            .HasForeignKey(x => x.RecordCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.LayoutVariants)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgLayoutFieldConfiguration : IEntityTypeConfiguration<CfgLayoutField>
{
    public void Configure(EntityTypeBuilder<CfgLayoutField> builder)
    {
        builder.ToTable("CfgLayoutField", t =>
        {
            t.HasCheckConstraint("CK_CfgLayoutField_StartPosition_Positive", "\"StartPosition\" > 0");
            t.HasCheckConstraint("CK_CfgLayoutField_Length_Positive", "\"Length\" > 0");
            t.HasCheckConstraint("CK_CfgLayoutField_Justification_Valid", "\"Justification\" IN ('L','R')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FieldNameEs).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FormatMask).HasMaxLength(80);
        builder.Property(x => x.TransformationPipelineJson).HasMaxLength(4000);

        builder.HasIndex(x => new { x.LayoutVariantId, x.FieldCode }).IsUnique();
        builder.HasIndex(x => new { x.LayoutVariantId, x.StartPosition }).IsUnique();
        builder.HasIndex(x => new { x.LayoutVariantId, x.SortOrder });

        builder.HasOne(x => x.LayoutVariant)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.LayoutVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SourceDefinition)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.SourceDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgFieldSourceDefinitionConfiguration : IEntityTypeConfiguration<CfgFieldSourceDefinition>
{
    public void Configure(EntityTypeBuilder<CfgFieldSourceDefinition> builder)
    {
        builder.ToTable("CfgFieldSourceDefinition");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConstantValue).HasMaxLength(400);
        builder.Property(x => x.EntityName).HasMaxLength(120);
        builder.Property(x => x.PropertyPath).HasMaxLength(250);
        builder.Property(x => x.SqlObjectName).HasMaxLength(250);
        builder.Property(x => x.ExpressionDsl).HasMaxLength(4000);
        builder.Property(x => x.ExternalCatalogCode).HasMaxLength(120);
        builder.Property(x => x.FallbackPolicyJson).HasMaxLength(2000);

        builder.HasOne(x => x.DataSourceType)
            .WithMany(x => x.FieldSources)
            .HasForeignKey(x => x.DataSourceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgFieldRuleConfiguration : IEntityTypeConfiguration<CfgFieldRule>
{
    public void Configure(EntityTypeBuilder<CfgFieldRule> builder)
    {
        builder.ToTable("CfgFieldRule", t =>
        {
            t.HasCheckConstraint("CK_CfgFieldRule_Severity_Valid", "\"Severity\" IN ('ERROR','WARN')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RuleCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ErrorMessageEs).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ConditionDsl).HasMaxLength(4000);
        builder.Property(x => x.RuleConfigJson).HasMaxLength(4000);

        builder.HasIndex(x => new { x.LayoutFieldId, x.RuleCode }).IsUnique();
        builder.HasIndex(x => new { x.LayoutFieldId, x.Order });

        builder.HasOne(x => x.LayoutField)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.LayoutFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RuleType)
            .WithMany(x => x.FieldRules)
            .HasForeignKey(x => x.RuleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgRuleSetConfiguration : IEntityTypeConfiguration<CfgRuleSet>
{
    public void Configure(EntityTypeBuilder<CfgRuleSet> builder)
    {
        builder.ToTable("CfgRuleSet");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RuleSetCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameEs).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Scope).HasMaxLength(30).IsRequired();

        builder.HasIndex(x => x.RuleSetCode).IsUnique();
    }
}

public class CfgRuleSetRuleConfiguration : IEntityTypeConfiguration<CfgRuleSetRule>
{
    public void Configure(EntityTypeBuilder<CfgRuleSetRule> builder)
    {
        builder.ToTable("CfgRuleSetRule", t =>
        {
            t.HasCheckConstraint("CK_CfgRuleSetRule_Order_NonNegative", "\"Order\" >= 0");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RuleCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConditionDsl).HasMaxLength(4000);
        builder.Property(x => x.RuleConfigJson).HasMaxLength(4000);
        builder.Property(x => x.ErrorCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ErrorMessageEs).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => new { x.RuleSetId, x.RuleCode }).IsUnique();
        builder.HasIndex(x => new { x.RuleSetId, x.Order });

        builder.HasOne(x => x.RuleSet)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.RuleSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RuleType)
            .WithMany(x => x.RuleSetRules)
            .HasForeignKey(x => x.RuleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CfgPublishRequestConfiguration : IEntityTypeConfiguration<CfgPublishRequest>
{
    public void Configure(EntityTypeBuilder<CfgPublishRequest> builder)
    {
        builder.ToTable("CfgPublishRequest", t =>
        {
            t.HasCheckConstraint("CK_CfgPublishRequest_Status_Valid", "\"Status\" IN ('PENDING','APPROVED','REJECTED','CANCELLED')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(120);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ValidationReportJson).HasMaxLength(8000);

        builder.HasIndex(x => new { x.ProfileId, x.Status, x.RequestedAt });

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.PublishRequests)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HistConfigSnapshotConfiguration : IEntityTypeConfiguration<HistConfigSnapshot>
{
    public void Configure(EntityTypeBuilder<HistConfigSnapshot> builder)
    {
        builder.ToTable("HistConfigSnapshot");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnapshotType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.SnapshotJson).HasMaxLength(16000).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => new { x.ProfileId, x.VersionMajor, x.VersionMinor, x.CreatedAtUtc });

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.Snapshots)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class HistConfigChangeConfiguration : IEntityTypeConfiguration<HistConfigChange>
{
    public void Configure(EntityTypeBuilder<HistConfigChange> builder)
    {
        builder.ToTable("HistConfigChange");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ChangeType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.BeforeJson).HasMaxLength(16000);
        builder.Property(x => x.AfterJson).HasMaxLength(16000);
        builder.Property(x => x.ChangedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(120);

        builder.HasIndex(x => new { x.ProfileId, x.ChangedAtUtc });
        builder.HasIndex(x => x.CorrelationId);

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.Changes)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
