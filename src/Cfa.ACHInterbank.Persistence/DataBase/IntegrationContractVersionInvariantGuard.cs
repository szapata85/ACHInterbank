using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cfa.ACHInterbank.Persistence.DataBase;

internal static class IntegrationContractVersionInvariantGuard
{
    private static readonly HashSet<string> AuditPropertyNames =
    [
        nameof(IntegrationContractVersion.CreatedAt),
        nameof(IntegrationContractVersion.UpdatedAt),
        nameof(IntegrationContractVersion.Revision)
    ];

    public static async Task ValidateAsync(AchDbContext context, CancellationToken ct)
    {
        var contractEntries = context.ChangeTracker
            .Entries<IntegrationContractVersion>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in contractEntries)
        {
            ValidateContractEntry(entry);
        }

        await ValidateSingleActivePerMethodAsync(context, contractEntries, ct);
        await ValidateParameterImmutabilityAsync(context, contractEntries, ct);
    }

    private static void ValidateContractEntry(EntityEntry<IntegrationContractVersion> entry)
    {
        if (entry.State == EntityState.Added)
        {
            if (entry.Entity.Revision < 1)
            {
                throw new InvalidOperationException("La revisión de una versión contractual debe iniciar en 1 o superior.");
            }

            if (entry.Entity.Status == IntegrationContractVersionStatusEnum.Active)
            {
                EnsureActivationEvidence(entry.Entity);
            }

            return;
        }

        var originalStatus = entry.Property(x => x.Status).OriginalValue;

        if (entry.State == EntityState.Deleted)
        {
            throw new InvalidOperationException("Las versiones contractuales persistidas no se eliminan; deben preservarse como historia.");
        }

        var currentStatus = entry.Entity.Status;
        ValidateLifecycleTransition(originalStatus, currentStatus);

        var changedContractProperties = entry.Properties
            .Where(property => property.IsModified && !AuditPropertyNames.Contains(property.Metadata.Name))
            .Select(property => property.Metadata.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (originalStatus == IntegrationContractVersionStatusEnum.Retired)
        {
            var onlyRollbackReactivation = currentStatus == IntegrationContractVersionStatusEnum.Active
                && changedContractProperties.SetEquals([nameof(IntegrationContractVersion.Status)]);
            if (changedContractProperties.Count > 0 && !onlyRollbackReactivation)
            {
                throw new InvalidOperationException(
                    "Una versión contractual Retired es inmutable; solo puede reactivarse sin reescribir su contenido durante rollback gobernado.");
            }
        }

        if (originalStatus == IntegrationContractVersionStatusEnum.Active)
        {
            var onlyRetirement = currentStatus == IntegrationContractVersionStatusEnum.Retired
                && changedContractProperties.SetEquals([nameof(IntegrationContractVersion.Status)]);
            if (changedContractProperties.Count > 0 && !onlyRetirement)
            {
                throw new InvalidOperationException("Una versión contractual Active es inmutable; solo puede transicionar a Retired.");
            }
        }

        if (originalStatus != currentStatus && currentStatus == IntegrationContractVersionStatusEnum.Active)
        {
            EnsureActivationEvidence(entry.Entity);
        }

        if (changedContractProperties.Count > 0)
        {
            var originalRevision = entry.Property(x => x.Revision).OriginalValue;
            var currentRevision = entry.Entity.Revision;
            if (currentRevision == originalRevision)
            {
                entry.Entity.Revision = checked(originalRevision + 1);
                entry.Property(x => x.Revision).IsModified = true;
            }
            else if (currentRevision != checked(originalRevision + 1))
            {
                throw new InvalidOperationException("La revisión contractual debe avanzar exactamente una unidad por cambio persistido.");
            }
        }
    }

    private static void ValidateLifecycleTransition(
        IntegrationContractVersionStatusEnum original,
        IntegrationContractVersionStatusEnum current)
    {
        var allowed = original switch
        {
            IntegrationContractVersionStatusEnum.Draft => current is IntegrationContractVersionStatusEnum.Draft or IntegrationContractVersionStatusEnum.Validated,
            IntegrationContractVersionStatusEnum.Validated => current is IntegrationContractVersionStatusEnum.Validated or IntegrationContractVersionStatusEnum.Active,
            IntegrationContractVersionStatusEnum.Active => current is IntegrationContractVersionStatusEnum.Active or IntegrationContractVersionStatusEnum.Retired,
            IntegrationContractVersionStatusEnum.Retired => current is IntegrationContractVersionStatusEnum.Retired or IntegrationContractVersionStatusEnum.Active,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Transición contractual no permitida: {original} -> {current}.");
        }
    }

    private static void EnsureActivationEvidence(IntegrationContractVersion contract)
    {
        if (!contract.ActivatedAtUtc.HasValue || string.IsNullOrWhiteSpace(contract.ActivatedBy))
        {
            throw new InvalidOperationException("Una versión contractual Active requiere ActivatedAtUtc y ActivatedBy.");
        }
    }

    private static async Task ValidateSingleActivePerMethodAsync(
        AchDbContext context,
        IReadOnlyCollection<EntityEntry<IntegrationContractVersion>> trackedEntries,
        CancellationToken ct)
    {
        if (trackedEntries.Count == 0)
        {
            return;
        }

        var touchedMethodIds = trackedEntries.Select(entry => entry.Entity.MethodId).Distinct().ToArray();
        var trackedIds = trackedEntries.Select(entry => entry.Entity.Id).ToArray();

        var databaseActives = await context.IntegrationContractVersions
            .AsNoTracking()
            .Where(contract => touchedMethodIds.Contains(contract.MethodId)
                && contract.Status == IntegrationContractVersionStatusEnum.Active
                && !trackedIds.Contains(contract.Id))
            .Select(contract => new { contract.MethodId, contract.Id })
            .ToListAsync(ct);

        var activeCounts = databaseActives
            .GroupBy(contract => contract.MethodId)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var entry in trackedEntries.Where(entry => entry.State != EntityState.Deleted
                     && entry.Entity.Status == IntegrationContractVersionStatusEnum.Active))
        {
            activeCounts[entry.Entity.MethodId] = activeCounts.GetValueOrDefault(entry.Entity.MethodId) + 1;
        }

        var duplicate = activeCounts.FirstOrDefault(pair => pair.Value > 1);
        if (duplicate.Value > 1)
        {
            throw new InvalidOperationException($"Solo puede existir una versión contractual Active por método. MethodId={duplicate.Key}.");
        }
    }

    private static async Task ValidateParameterImmutabilityAsync(
        AchDbContext context,
        IReadOnlyCollection<EntityEntry<IntegrationContractVersion>> trackedContracts,
        CancellationToken ct)
    {
        var parameterEntries = context.ChangeTracker
            .Entries<IntegrationMethodParameter>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (parameterEntries.Count == 0)
        {
            return;
        }

        var trackedById = trackedContracts
            .Where(entry => entry.State != EntityState.Deleted)
            .ToDictionary(entry => entry.Entity.Id, entry => entry);

        var contractIds = parameterEntries
            .Select(entry => entry.Entity.ContractVersionId ?? entry.Property(x => x.ContractVersionId).OriginalValue)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .Where(id => !trackedById.ContainsKey(id))
            .ToArray();

        var persistedContracts = contractIds.Length == 0
            ? new Dictionary<Guid, ContractState>()
            : await context.IntegrationContractVersions
                .AsNoTracking()
                .Where(contract => contractIds.Contains(contract.Id))
                .Select(contract => new ContractState(contract.Id, contract.Status, contract.SourceType))
                .ToDictionaryAsync(contract => contract.Id, ct);

        foreach (var entry in parameterEntries)
        {
            var originalContractId = entry.State == EntityState.Added
                ? null
                : entry.Property(x => x.ContractVersionId).OriginalValue;
            var currentContractId = entry.Entity.ContractVersionId;
            var effectiveContractId = originalContractId ?? currentContractId;
            if (!effectiveContractId.HasValue)
            {
                continue;
            }

            var state = ResolveContractState(effectiveContractId.Value, trackedById, persistedContracts);
            if (state is null)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                if (trackedById.TryGetValue(effectiveContractId.Value, out var parent)
                    && parent.State == EntityState.Added)
                {
                    continue;
                }

                if (state.Status is IntegrationContractVersionStatusEnum.Active or IntegrationContractVersionStatusEnum.Retired)
                {
                    throw new InvalidOperationException("No se pueden agregar parámetros a una versión contractual Active o Retired.");
                }

                continue;
            }

            if (originalContractId is null
                && currentContractId.HasValue
                && state.SourceType == IntegrationContractSourceTypeEnum.BaselineMigration)
            {
                // Transitional legacy bridge: P1.2/P1.3 may attach an existing unversioned row to baseline-v1 once.
                continue;
            }

            if (state.Status is IntegrationContractVersionStatusEnum.Active or IntegrationContractVersionStatusEnum.Retired)
            {
                throw new InvalidOperationException("Los parámetros de una versión contractual Active o Retired son inmutables.");
            }
        }
    }

    private static ContractState? ResolveContractState(
        Guid contractId,
        IReadOnlyDictionary<Guid, EntityEntry<IntegrationContractVersion>> trackedById,
        IReadOnlyDictionary<Guid, ContractState> persistedContracts)
    {
        if (trackedById.TryGetValue(contractId, out var tracked))
        {
            return new ContractState(tracked.Entity.Id, tracked.Entity.Status, tracked.Entity.SourceType);
        }

        return persistedContracts.GetValueOrDefault(contractId);
    }

    private sealed record ContractState(
        Guid Id,
        IntegrationContractVersionStatusEnum Status,
        IntegrationContractSourceTypeEnum SourceType);
}
