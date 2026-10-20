using System.Text.Json;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class CfaOperationalTransactionRestrictionConfiguration
    : IEntityTypeConfiguration<CfaOperationalTransactionRestriction>
{
    public void Configure(EntityTypeBuilder<CfaOperationalTransactionRestriction> builder)
    {
        builder.ToTable("CfaOperationalTransactionRestrictions", table =>
        {
            table.HasCheckConstraint(
                "CK_CfaOperationalTransactionRestrictions_Scope",
                "NOT (\"ClearingHouseId\" IS NOT NULL AND \"ClearingHouseCycleConfigId\" IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_CfaOperationalTransactionRestrictions_EffectiveRange",
                "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            table.HasCheckConstraint(
                "CK_CfaOperationalTransactionRestrictions_MaxAmountPerTransaction",
                "\"MaxAmountPerTransaction\" IS NULL OR \"MaxAmountPerTransaction\" >= 0");
            table.HasCheckConstraint(
                "CK_CfaOperationalTransactionRestrictions_MaxAmountPerCycle",
                "\"MaxAmountPerCycle\" IS NULL OR \"MaxAmountPerCycle\" >= 0");
            table.HasCheckConstraint(
                "CK_CfaOperationalTransactionRestrictions_MaxTransactionsPerCycle",
                "\"MaxTransactionsPerCycle\" IS NULL OR \"MaxTransactionsPerCycle\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaxAmountPerTransaction).HasPrecision(18, 2);
        builder.Property(x => x.MaxAmountPerCycle).HasPrecision(18, 2);
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        var converter = new ValueConverter<List<AccountTypeEnum>?, string?>(
            value => SerializeAllowedAccountTypes(value),
            value => DeserializeAllowedAccountTypes(value));
        var comparer = new ValueComparer<List<AccountTypeEnum>?>(
            (left, right) => AllowedAccountTypesEqual(left, right),
            value => AllowedAccountTypesHashCode(value),
            value => SnapshotAllowedAccountTypes(value));

        var allowedAccountTypes = builder.Property(x => x.AllowedAccountTypes)
            .HasConversion(converter)
            .HasColumnName("AllowedAccountTypes")
            .HasMaxLength(256);
        allowedAccountTypes.Metadata.SetValueComparer(comparer);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClearingHouseCycleConfig)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseCycleConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
            {
                x.ClearingHouseId,
                x.ClearingHouseCycleConfigId,
                x.TransactionType,
                x.IsPrenotification,
                x.IsActive,
                x.EffectiveFrom
            })
            .HasDatabaseName("IX_CfaOperationalRestrictions_Resolve");
    }

    private static string? SerializeAllowedAccountTypes(List<AccountTypeEnum>? value)
        => value is null
            ? null
            : JsonSerializer.Serialize(value.Distinct().OrderBy(item => (int)item).ToArray());

    private static List<AccountTypeEnum>? DeserializeAllowedAccountTypes(string? value)
        => value is null
            ? null
            : JsonSerializer.Deserialize<List<AccountTypeEnum>>(value) ?? [];

    private static bool AllowedAccountTypesEqual(List<AccountTypeEnum>? left, List<AccountTypeEnum>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.OrderBy(item => (int)item).SequenceEqual(right.OrderBy(item => (int)item));
    }

    private static int AllowedAccountTypesHashCode(List<AccountTypeEnum>? value)
    {
        if (value is null)
        {
            return 0;
        }

        var hash = new HashCode();
        foreach (var item in value.Distinct().OrderBy(item => (int)item))
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    private static List<AccountTypeEnum>? SnapshotAllowedAccountTypes(List<AccountTypeEnum>? value)
        => value is null ? null : [.. value];
}
