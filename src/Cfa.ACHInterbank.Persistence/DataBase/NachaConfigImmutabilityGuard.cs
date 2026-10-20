using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cfa.ACHInterbank.Persistence.DataBase;

internal static class NachaConfigImmutabilityGuard
{
    private const string Publish = "PUBLISH";

    public static async Task ValidateAsync(AchDbContext context, CancellationToken ct)
    {
        var changes = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (changes.Count == 0)
            return;

        foreach (var entry in changes)
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.Entity is HistConfigSnapshot)
                    Reject("CONFIG_SNAPSHOT_IMMUTABLE");
                if (entry.Entity is HistConfigChange)
                    Reject("CONFIG_HISTORY_IMMUTABLE");
            }
        }

        var published = new Dictionary<int, bool>();
        async Task<bool> WasPublished(int profileId)
        {
            if (profileId <= 0)
                return false;
            if (!published.TryGetValue(profileId, out var value))
            {
                value = await context.CfgProfiles.AsNoTracking().AnyAsync(profile =>
                    profile.Id == profileId
                    && (profile.PublishedAt != null
                        || context.HistConfigSnapshots.Any(snapshot =>
                            snapshot.ProfileId == profileId && snapshot.SnapshotType == Publish)
                        || context.HistConfigChanges.Any(change =>
                            change.ProfileId == profileId && change.ChangeType == Publish)), ct);
                published.Add(profileId, value);
            }
            return value;
        }

        foreach (var entry in changes)
        {
            if (entry.Entity is HistConfigSnapshot addedSnapshot
                && entry.State == EntityState.Added && addedSnapshot.SnapshotType == Publish)
            {
                var existing = await context.HistConfigSnapshots.AsNoTracking()
                    .Where(snapshot => snapshot.ProfileId == addedSnapshot.ProfileId
                        && snapshot.VersionMajor == addedSnapshot.VersionMajor
                        && snapshot.VersionMinor == addedSnapshot.VersionMinor
                        && snapshot.SnapshotType == Publish)
                    .Select(snapshot => snapshot.SnapshotJson)
                    .ToListAsync(ct);
                if (existing.Any(json => NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(json).IsSupported))
                    Reject("CONFIG_SNAPSHOT_IMMUTABLE");

                if (await WasPublished(addedSnapshot.ProfileId))
                {
                    var candidate = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(
                        addedSnapshot.SnapshotJson);
                    var profileVersion = await context.CfgProfiles.AsNoTracking()
                        .Where(profile => profile.Id == addedSnapshot.ProfileId)
                        .Select(profile => new { profile.VersionMajor, profile.VersionMinor })
                        .FirstOrDefaultAsync(ct);
                    if (!candidate.IsSupported || candidate.Snapshot?.Profile.ProfileId != addedSnapshot.ProfileId
                        || profileVersion is null
                        || profileVersion.VersionMajor != addedSnapshot.VersionMajor
                        || profileVersion.VersionMinor != addedSnapshot.VersionMinor)
                        Reject("CONFIG_SNAPSHOT_IMMUTABLE");
                }
                continue;
            }

            if (entry.Entity is CfgProfile profile)
            {
                if (entry.State != EntityState.Added && await WasPublished(profile.Id))
                {
                    if (entry.State == EntityState.Deleted
                        || !await IsAllowedLifecycleChange(context, profile, ct))
                        Reject("CONFIG_PUBLISHED_PROFILE_IMMUTABLE");
                }
                continue;
            }

            if (entry.Entity is CfgProfileTag or CfgProfileRecord or CfgLayoutVariant
                or CfgLayoutField or CfgFieldRule)
            {
                if (entry.State == EntityState.Added && entry.Entity is CfgLayoutField addedField
                    && addedField.LayoutVariantId > 0)
                {
                    if (await context.CfgLayoutVariants.AsNoTracking().AnyAsync(variant =>
                        variant.Id == addedField.LayoutVariantId
                        && (variant.Profile.PublishedAt != null
                            || context.HistConfigSnapshots.Any(snapshot => snapshot.ProfileId == variant.ProfileId
                                && snapshot.SnapshotType == Publish)
                            || context.HistConfigChanges.Any(change => change.ProfileId == variant.ProfileId
                                && change.ChangeType == Publish)), ct))
                        Reject("CONFIG_PUBLISHED_CHILD_IMMUTABLE");
                    continue;
                }
                if (entry.State == EntityState.Added && entry.Entity is CfgFieldRule addedRule
                    && addedRule.LayoutFieldId > 0)
                {
                    if (await context.CfgLayoutFields.AsNoTracking().AnyAsync(field =>
                        field.Id == addedRule.LayoutFieldId
                        && (field.LayoutVariant.Profile.PublishedAt != null
                            || context.HistConfigSnapshots.Any(snapshot =>
                                snapshot.ProfileId == field.LayoutVariant.ProfileId
                                && snapshot.SnapshotType == Publish)
                            || context.HistConfigChanges.Any(change =>
                                change.ProfileId == field.LayoutVariant.ProfileId
                                && change.ChangeType == Publish)), ct))
                        Reject("CONFIG_PUBLISHED_CHILD_IMMUTABLE");
                    continue;
                }
                foreach (var profileId in await OwnerProfileIds(context, entry, ct))
                {
                    if (await WasPublished(profileId))
                        Reject("CONFIG_PUBLISHED_CHILD_IMMUTABLE");
                }
                continue;
            }

            if (entry.Entity is CfgFieldSourceDefinition source && entry.State != EntityState.Added)
            {
                if (await context.CfgLayoutFields.AsNoTracking().AnyAsync(field =>
                    field.SourceDefinitionId == source.Id
                    && (field.LayoutVariant.Profile.PublishedAt != null
                        || context.HistConfigSnapshots.Any(snapshot =>
                            snapshot.ProfileId == field.LayoutVariant.ProfileId
                            && snapshot.SnapshotType == Publish)
                        || context.HistConfigChanges.Any(change =>
                            change.ProfileId == field.LayoutVariant.ProfileId
                            && change.ChangeType == Publish)), ct))
                    Reject("CONFIG_PUBLISHED_SOURCE_IMMUTABLE");
                continue;
            }

            if (entry.Entity is CfgRuleSet ruleSet && entry.State != EntityState.Added)
            {
                if (await IsPublishedRuleSet(context, ruleSet.Id, ct))
                    Reject("CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");
                continue;
            }

            if (entry.Entity is CfgRuleSetRule rule)
            {
                var ruleSetIds = new HashSet<int>();
                Add(ruleSetIds, rule.RuleSetId);
                Add(ruleSetIds, rule.RuleSet?.Id ?? 0);
                if (entry.State != EntityState.Added)
                    Add(ruleSetIds, await context.CfgRuleSetRules.AsNoTracking()
                        .Where(candidate => candidate.Id == rule.Id)
                        .Select(candidate => candidate.RuleSetId)
                        .FirstOrDefaultAsync(ct));
                foreach (var ruleSetId in ruleSetIds)
                {
                    if (await IsPublishedRuleSet(context, ruleSetId, ct))
                        Reject("CONFIG_PUBLISHED_RULE_SET_IMMUTABLE");
                }
            }
        }
    }

    private static async Task<bool> IsAllowedLifecycleChange(
        AchDbContext context, CfgProfile profile, CancellationToken ct)
    {
        var persisted = await context.CfgProfiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == profile.Id, ct);
        if (persisted is null)
            return false;

        var statusChanged = profile.StatusId != persisted.StatusId;
        var effectiveToChanged = profile.EffectiveTo != persisted.EffectiveTo;
        if (!statusChanged && !effectiveToChanged)
        {
            var statusCode = await context.CatConfigStatuses.AsNoTracking()
                .Where(status => status.Id == profile.StatusId)
                .Select(status => status.Code)
                .FirstOrDefaultAsync(ct);
            var expectedAction = statusCode switch
            {
                "INACTIVO" => "INACTIVATE",
                "ARCHIVADO" => "ARCHIVE",
                _ => null
            };
            if (expectedAction is null || !context.ChangeTracker.Entries<HistConfigChange>()
                .Any(change => change.State == EntityState.Added
                    && change.Entity.ProfileId == profile.Id
                    && change.Entity.ChangeType == expectedAction))
                return false;
        }
        if (statusChanged || effectiveToChanged)
        {
            var statusCode = await context.CatConfigStatuses.AsNoTracking()
                .Where(status => status.Id == profile.StatusId)
                .Select(status => status.Code)
                .FirstOrDefaultAsync(ct);
            if (statusChanged && statusCode is not ("INACTIVO" or "ARCHIVADO"))
                return false;
            if (effectiveToChanged && (statusCode != "INACTIVO"
                || persisted.EffectiveTo is not null || profile.EffectiveTo is null))
                return false;
        }

        foreach (var property in context.Entry(profile).Properties)
        {
            var name = property.Metadata.Name;
            if (name is nameof(CfgProfile.StatusId) or nameof(CfgProfile.EffectiveTo)
                or nameof(CfgProfile.UpdatedAt))
                continue;
            var member = property.Metadata.PropertyInfo;
            if (member is null || !Equal(property.CurrentValue, member.GetValue(persisted)))
                return false;
        }
        return true;
    }

    private static bool Equal(object? left, object? right)
        => left is byte[] leftBytes && right is byte[] rightBytes
            ? leftBytes.AsSpan().SequenceEqual(rightBytes)
            : Equals(left, right);

    private static async Task<HashSet<int>> OwnerProfileIds(
        AchDbContext context, EntityEntry entry, CancellationToken ct)
    {
        var ids = new HashSet<int>();
        switch (entry.Entity)
        {
            case CfgProfileTag tag:
                Add(ids, tag.ProfileId);
                Add(ids, tag.Profile?.Id ?? 0);
                if (entry.State != EntityState.Added)
                    Add(ids, await context.CfgProfileTags.AsNoTracking()
                        .Where(row => row.Id == tag.Id).Select(row => row.ProfileId)
                        .FirstOrDefaultAsync(ct));
                break;
            case CfgProfileRecord record:
                Add(ids, record.ProfileId);
                Add(ids, record.Profile?.Id ?? 0);
                if (entry.State != EntityState.Added)
                    Add(ids, await context.CfgProfileRecords.AsNoTracking()
                        .Where(row => row.Id == record.Id).Select(row => row.ProfileId)
                        .FirstOrDefaultAsync(ct));
                break;
            case CfgLayoutVariant variant:
                Add(ids, variant.ProfileId);
                Add(ids, variant.Profile?.Id ?? 0);
                if (entry.State != EntityState.Added)
                    Add(ids, await context.CfgLayoutVariants.AsNoTracking()
                        .Where(row => row.Id == variant.Id).Select(row => row.ProfileId)
                        .FirstOrDefaultAsync(ct));
                break;
            case CfgLayoutField field:
                Add(ids, field.LayoutVariant?.ProfileId ?? 0);
                Add(ids, field.LayoutVariant?.Profile?.Id ?? 0);
                Add(ids, await context.CfgLayoutVariants.AsNoTracking()
                    .Where(row => row.Id == field.LayoutVariantId)
                    .Select(row => row.ProfileId).FirstOrDefaultAsync(ct));
                if (entry.State != EntityState.Added)
                    Add(ids, await context.CfgLayoutFields.AsNoTracking()
                        .Where(row => row.Id == field.Id)
                        .Select(row => row.LayoutVariant.ProfileId)
                        .FirstOrDefaultAsync(ct));
                break;
            case CfgFieldRule rule:
                Add(ids, rule.LayoutField?.LayoutVariant?.ProfileId ?? 0);
                Add(ids, rule.LayoutField?.LayoutVariant?.Profile?.Id ?? 0);
                Add(ids, await context.CfgLayoutFields.AsNoTracking()
                    .Where(row => row.Id == rule.LayoutFieldId)
                    .Select(row => row.LayoutVariant.ProfileId)
                    .FirstOrDefaultAsync(ct));
                if (entry.State != EntityState.Added)
                    Add(ids, await context.CfgFieldRules.AsNoTracking()
                        .Where(row => row.Id == rule.Id)
                        .Select(row => row.LayoutField.LayoutVariant.ProfileId)
                        .FirstOrDefaultAsync(ct));
                break;
        }
        return ids;
    }

    private static Task<bool> IsPublishedRuleSet(AchDbContext context, int ruleSetId, CancellationToken ct)
        => context.CfgProfileRecords.AsNoTracking().AnyAsync(record =>
            record.SemanticRuleSetId == ruleSetId
            && (record.Profile.PublishedAt != null
                || context.HistConfigSnapshots.Any(snapshot =>
                    snapshot.ProfileId == record.ProfileId && snapshot.SnapshotType == Publish)
                || context.HistConfigChanges.Any(change =>
                    change.ProfileId == record.ProfileId && change.ChangeType == Publish)), ct);

    private static void Add(HashSet<int> ids, int id)
    {
        if (id > 0)
            ids.Add(id);
    }

    private static void Reject(string code)
    {
        var message = code switch
        {
            "CONFIG_PUBLISHED_PROFILE_IMMUTABLE" => "El perfil publicado solo admite transiciones de ciclo de vida; cambie la definicion en una nueva version.",
            "CONFIG_PUBLISHED_CHILD_IMMUTABLE" => "La definicion del perfil publicado es inmutable; cambiela en una nueva version.",
            "CONFIG_PUBLISHED_SOURCE_IMMUTABLE" => "La fuente compartida por un perfil publicado es inmutable; cree una fuente nueva.",
            "CONFIG_PUBLISHED_RULE_SET_IMMUTABLE" => "El conjunto de reglas compartido por un perfil publicado es inmutable; cree uno nuevo.",
            "CONFIG_SNAPSHOT_IMMUTABLE" => "Los snapshots de configuracion son historicos e inmutables.",
            "CONFIG_HISTORY_IMMUTABLE" => "El historial de configuracion solo admite nuevas entradas.",
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
        throw new NachaConfigException(code, message, 409);
    }
}
