using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchReturnsService : IAchReturnsService
{
    private readonly AchDbContext _context;
    private readonly IAchReturnGenerationLockService _returnGenerationLockService;
    private readonly TimeProvider _timeProvider;
    private readonly AchReturnOriginalCorrelationComponent _correlation;
    private readonly AchReturnEligibilityPolicyComponent _eligibilityPolicy;
    private readonly AchReturnCausePolicyComponent _causePolicy;
    private readonly AchReturnBatchComposerComponent _batchAssembly;
    private readonly AchReturnArtifactGenerationComponent _artifactGeneration;
    private readonly AchReturnAuditMonitoringComponent _auditMonitoring;
    private readonly AchReturnStateTransitionComponent _stateTransitions;

    public AchReturnsService(
        AchDbContext context,
        TimeProvider? timeProvider = null,
        IAchRegulatoryCatalogService? regulatoryCatalogService = null,
        IAchReturnEligibilityService? returnEligibilityService = null,
        IAchReturnGenerationLockService? returnGenerationLockService = null,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        IExternalFileNamePolicy? externalFileNamePolicy = null,
        IAchCauseCodePolicy? causeCodePolicy = null,
        ILogger<AchReturnsService>? logger = null,
        IAchStateTransitionService? stateTransitionService = null,
        INachaFileBuilder? nachaFileBuilder = null,
        IAchReturnTraceSequenceService? returnTraceSequenceService = null,
        ICenitIncomingReturnPolicy? cenitReturnPolicy = null)
    {
        _context = context;
        _ = regulatoryCatalogService
            ?? throw new InvalidOperationException("IAchRegulatoryCatalogService es requerido para gobernanza regulatoria de devoluciones.");
        var resolvedEligibilityService = returnEligibilityService
            ?? throw new InvalidOperationException("IAchReturnEligibilityService es requerido para evaluar elegibilidad de devoluciones.");
        _returnGenerationLockService = returnGenerationLockService
            ?? throw new InvalidOperationException("IAchReturnGenerationLockService es requerido para control de concurrencia en devoluciones.");
        _timeProvider = timeProvider ?? TimeProvider.System;
        var resolvedLogger = logger ?? NullLogger<AchReturnsService>.Instance;

        // Preserve the legacy constructor fallback behavior for direct composition/tests while
        // keeping the extracted R7 components dependent only on explicit collaborators.
        var resolvedStateTransitionService = stateTransitionService ?? new AchStateTransitionService(context);
        var resolvedTraceSequenceService = returnTraceSequenceService ?? new AchReturnTraceSequenceService(context);

        _correlation = new AchReturnOriginalCorrelationComponent(context);
        _eligibilityPolicy = new AchReturnEligibilityPolicyComponent(context, resolvedEligibilityService, cenitReturnPolicy);
        _causePolicy = new AchReturnCausePolicyComponent(causeCodePolicy, resolvedLogger);
        _batchAssembly = new AchReturnBatchComposerComponent(resolvedTraceSequenceService);
        _artifactGeneration = new AchReturnArtifactGenerationComponent(context, nachaFileBuilder, externalFileNamePolicy, resolvedLogger);
        _auditMonitoring = new AchReturnAuditMonitoringComponent(
            paymentRailContextService,
            strategyResolver,
            shadowCompareService,
            resolvedLogger);
        _stateTransitions = new AchReturnStateTransitionComponent(resolvedStateTransitionService, _auditMonitoring);
    }

    public Task<IReadOnlyList<ReturnEligibleTransactionDto>> GetTransactionsByCycleAsync(
        string cycleId,
        CancellationToken ct = default)
        => _correlation.GetTransactionsByCycleAsync(cycleId, ct);

    public async Task<GenerateReturnsFileResponse> GenerateReturnsFileAsync(
        GenerateReturnsFileRequest request,
        CancellationToken ct = default)
    {
        AchReturnOriginalCorrelationComponent.ValidateRequest(request);
        var scope = await _correlation.ResolveScopeAsync(request, ct);
        _eligibilityPolicy.ValidateScopeRequest(scope, request);

        var selectedIds = request.Items.Select(i => i.TransactionId).Distinct().ToList();
        await using var generationLock = await _returnGenerationLockService.AcquireAsync(selectedIds, ct);
        var correlation = await _correlation.LoadAsync(scope, request, ct);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var prepared = new List<AchReturnPreparedItem>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var transaction = correlation.TransactionsById[item.TransactionId];
            await _eligibilityPolicy.ValidatePreEligibilityAsync(transaction, scope, item, ct);
            var reasonCode = await _eligibilityPolicy.ResolveNormalizedReasonAsync(transaction, scope, item, now, ct);
            await _causePolicy.ValidateAsync(transaction, scope, reasonCode, ct);

            var amount = transaction.IsPrenotification ? 0m : transaction.Amount;
            _eligibilityPolicy.ValidateCenitOperationalPolicy(
                transaction,
                scope,
                correlation,
                item,
                reasonCode,
                amount);

            var cenitSource = scope.IsCenit ? correlation.CenitSources[transaction.Id] : null;
            prepared.Add(_batchAssembly.Prepare(
                transaction,
                reasonCode,
                amount,
                scope,
                cenitSource,
                item.AchColombiaR10Basis));

            _auditMonitoring.CompareReturnShadow(
                scope.ReturnCycle.ClearingHouseId,
                scope.ReturnCycle.ClearingHouse?.Code,
                scope.PaymentRailCode,
                scope.ReturnCycle.Id,
                transaction.EffectiveEntryDate.Date,
                $"RETURN_GENERATED:{reasonCode}",
                legacyOperationSucceeded: true);
        }

        _artifactGeneration.EnsureAvailable();
        var orderedPrepared = _batchAssembly.OrderAndValidate(prepared);
        await using var databaseTransaction = _context.Database.IsRelational()
            && _context.Database.CurrentTransaction is null
            ? await _context.Database.BeginTransactionAsync(ct)
            : null;

        try
        {
            var allocated = await _batchAssembly.AllocateAsync(orderedPrepared, scope, now, ct);

            _context.Set<AchReturnGenerated>().AddRange(allocated.GeneratedRows);
            if (_context.Database.IsRelational())
            {
                await _context.SaveChangesAsync(ct);
            }

            var assembly = _batchAssembly.BuildAssembly(
                orderedPrepared,
                scope,
                correlation.CenitSources,
                allocated,
                now);
            var artifact = await _artifactGeneration.GenerateAsync(scope, request, assembly, now, ct);
            foreach (var row in assembly.GeneratedRows)
            {
                row.FileName = artifact.FileName;
            }

            await _context.SaveChangesAsync(ct);

            var preparedById = orderedPrepared.ToDictionary(item => item.Transaction.Id);
            await _stateTransitions.ApplyAsync(
                assembly,
                correlation.TransactionsById,
                preparedById,
                scope,
                artifact,
                now,
                ct);

            if (databaseTransaction is not null)
            {
                await databaseTransaction.CommitAsync(ct);
            }

            return new GenerateReturnsFileResponse(
                artifact.FileName,
                "text/plain",
                Encoding.UTF8.GetBytes(artifact.Content),
                artifact.RecordCount,
                assembly.GeneratedRows.Count);
        }
        catch (Exception ex)
        {
            if (databaseTransaction is not null)
            {
                await databaseTransaction.RollbackAsync(CancellationToken.None);
            }

            if (ex is DbUpdateException && RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                _context.ChangeTracker.Clear();
                var conflictingIds = await _context.Set<AchReturnGenerated>()
                    .AsNoTracking()
                    .Where(row => selectedIds.Contains(row.OriginalTransactionId))
                    .Select(row => row.OriginalTransactionId)
                    .ToListAsync(CancellationToken.None);
                if (conflictingIds.Count > 0)
                {
                    throw new AchReturnAlreadyGeneratedException(conflictingIds);
                }
            }

            throw;
        }
    }
}
