using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Cfa.ACHInterbank.Persistence.ACH.OutgoingTransactionMonitoring;

[Scoped]
public sealed class OutgoingTransactionMonitoringQueryService : IOutgoingTransactionMonitoringQueryService
{
    private static readonly HashSet<int> AllowedPageSizes = [10, 25, 50, 100];
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "amount", "identifier", "lastUpdatedAt"
    };

    private readonly AchDbContext _context;
    private readonly IOutgoingTransactionMonitoringStatusPolicy _statusPolicy;
    private readonly TimeProvider _timeProvider;

    public OutgoingTransactionMonitoringQueryService(
        AchDbContext context,
        IOutgoingTransactionMonitoringStatusPolicy statusPolicy,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _statusPolicy = statusPolicy;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<OutgoingMonitoringPagedResult<OutgoingTransactionMonitoringListItem>> SearchAsync(
        OutgoingTransactionMonitoringQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(query);
        var source = BuildConfirmedOutgoingQuery(normalized);
        source = ApplyFunctionalFilters(source, normalized);

        var totalItems = await source.LongCountAsync(cancellationToken);
        source = ApplyOrdering(source, normalized.SortBy, normalized.SortDirection);

        var rows = await source
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(BuildProjection())
            .ToListAsync(cancellationToken);

        var items = rows.Select(MapListItem).ToArray();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        return new OutgoingMonitoringPagedResult<OutgoingTransactionMonitoringListItem>(
            items,
            normalized.PageNumber,
            normalized.PageSize,
            totalItems,
            totalPages,
            normalized.PageNumber > 1,
            normalized.PageNumber < totalPages);
    }

    public async Task<OutgoingTransactionMonitoringDetail?> GetDetailAsync(
        int transactionId,
        bool includeTechnicalDetail,
        CancellationToken cancellationToken = default)
    {
        if (transactionId <= 0)
            return null;

        var row = await _context.AchTransactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == transactionId
                && transaction.Direction == AchTransactionDirection.Outgoing
                && transaction.ClassificationStatus == AchTransactionClassificationStatus.Determined)
            .Select(BuildProjection())
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        var stateEvents = await _context.AchTransactionStateEvents
            .AsNoTracking()
            .Where(item => item.AchTransactionId == transactionId)
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new StateEventRow(
                item.OccurredAtUtc,
                item.FromState,
                item.ToState,
                item.ReasonCode,
                item.ResolvedReasonDescription))
            .ToListAsync(cancellationToken);

        var attempts = await _context.ContrapartidaDispatchAttempts
            .AsNoTracking()
            .Where(item => item.DispatchItem.AchTransactionId == transactionId)
            .OrderBy(item => item.StartedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new AttemptRow(
                item.AttemptNumber,
                item.StartedAtUtc,
                item.FinishedAtUtc,
                item.IsSuccessful,
                item.IsFunctionalRejection,
                item.IsTechnicalFailure,
                item.RequiresManualReview,
                item.ExternalResponseCode,
                item.ExternalResponseMessage,
                item.ErrorCode,
                item.ErrorMessage,
                item.SoapMethodName,
                item.ExecutionMode,
                item.DurationMs,
                item.CorrelationId))
            .ToListAsync(cancellationToken);

        var files = await _context.AchFileExportTransactions
            .AsNoTracking()
            .Where(item => item.AchTransactionId == transactionId)
            .OrderBy(item => item.IncludedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new FileRow(
                item.AchFileExportId,
                item.AchFileExport.FileName,
                item.AchFileExport.Version,
                item.FileSequence,
                item.IncludedAtUtc,
                item.AchFileExport.GeneratedAtUtc,
                item.AchFileExport.LifecycleStatus,
                item.AchFileExport.TransmissionReference,
                item.AchFileExport.TransmittedAtUtc,
                item.AchFileExport.AcknowledgedAtUtc,
                item.AchFileExport.AcknowledgementCode))
            .ToListAsync(cancellationToken);

        var responses = await _context.AchResponses
            .AsNoTracking()
            .Where(item => item.AchTransactionId == transactionId)
            .OrderBy(item => item.FechaRecepcion)
            .ThenBy(item => item.Id)
            .Select(item => new ResponseRow(
                item.Id,
                item.FechaRecepcion,
                item.TipoRespuesta,
                item.CodigoEstadoExterno,
                item.CodigoCausalExterna,
                item.DescripcionCausal,
                item.CorrelationStatus))
            .ToListAsync(cancellationToken);

        var incomingEvents = await _context.IncomingNachaProcessingEvents
            .AsNoTracking()
            .Where(item => item.AchTransactionId == transactionId)
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new IncomingEventRow(item.OccurredAtUtc, item.EventType, item.EventStatus, item.Message))
            .ToListAsync(cancellationToken);

        var liquidityDecisions = await _context.LiquidityOptimizationDecisions
            .AsNoTracking()
            .Where(item => item.AchTransactionId == transactionId)
            .OrderBy(item => item.DecidedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new LiquidityRow(item.DecidedAtUtc, item.DecisionType, item.DecisionReason, item.FromCycleId, item.ToCycleId))
            .ToListAsync(cancellationToken);

        var timeline = BuildTimeline(row, stateEvents, attempts, files, responses, incomingEvents, liquidityDecisions);
        var fileDetails = files.Select(MapFileDetail).ToArray();
        var returnDetails = stateEvents
            .Where(item => IsReturnState(item.ToState))
            .Select(item => new OutgoingTransactionReturnDetail(
                item.OccurredAtUtc,
                StateDisplay(item.ToState),
                EmptyToNull(item.ReasonCode),
                EmptyToNull(item.ReasonDescription)))
            .ToArray();

        var responseDetails = responses.Select(item => new OutgoingTransactionResponseDetail(
            item.Id,
            item.ReceivedAtUtc,
            item.ResponseType.ToString() == "Prenota" ? "Prenotificación" : "Transacción",
            item.ExternalStatusCode,
            EmptyToNull(item.CauseCode),
            EmptyToNull(item.CauseDescription),
            CorrelationDisplay(item.CorrelationStatus))).ToArray();

        var lastAttempt = attempts.LastOrDefault();
        var warnings = BuildWarnings(row, files).ToArray();
        return new OutgoingTransactionMonitoringDetail
        {
            Summary = MapListItem(row),
            Classification = new OutgoingTransactionClassificationDetail(
                "Salida",
                row.Origin == AchTransactionOrigin.Cfa ? "Originada por CFA" : "No determinado",
                row.MonetaryRoute == AchMonetaryIntegrationRoute.ProcContrapartidas
                    ? "Integración de contrapartidas"
                    : "Revisión requerida",
                "Determinada",
                row.ClassifiedAtUtc,
                row.ClassificationVersion),
            Integration = new OutgoingTransactionIntegrationDetail(
                row.HasDispatchItem,
                attempts.Count,
                MapListItem(row).InitialResultDisplayName,
                EmptyToNull(lastAttempt?.ExternalResponseCode),
                EmptyToNull(lastAttempt?.ExternalResponseMessage),
                lastAttempt?.StartedAtUtc,
                attempts.LastOrDefault(item => item.IsSuccessful)?.FinishedAtUtc),
            Files = fileDetails,
            Responses = responseDetails,
            Returns = returnDetails,
            Timeline = timeline,
            Warnings = warnings,
            TechnicalDetail = includeTechnicalDetail
                ? new OutgoingTransactionTechnicalDetail(
                    transactionId,
                    EmptyToNull(lastAttempt?.MethodName),
                    EmptyToNull(lastAttempt?.ExecutionMode),
                    EmptyToNull(lastAttempt?.ExternalResponseCode) ?? EmptyToNull(lastAttempt?.ErrorCode),
                    lastAttempt?.DurationMs,
                    EmptyToNull(lastAttempt?.CorrelationId))
                : null
        };
    }

    private IQueryable<AchTransaction> BuildConfirmedOutgoingQuery(OutgoingTransactionMonitoringQuery query)
    {
        var source = _context.AchTransactions
            .AsNoTracking()
            .Where(transaction => transaction.Direction == AchTransactionDirection.Outgoing
                && transaction.ClassificationStatus == AchTransactionClassificationStatus.Determined
                && transaction.CreatedAt >= query.FromUtc!.Value
                && transaction.CreatedAt <= query.ToUtc!.Value);

        if (query.ClearingHouseId.HasValue)
            source = source.Where(item => item.AchCycle.ClearingHouseId == query.ClearingHouseId.Value);
        if (!string.IsNullOrEmpty(query.CycleId))
            source = source.Where(item => item.AchCycleId == query.CycleId);
        if (query.DestinationInstitutionId.HasValue)
            source = source.Where(item => item.DestinationInstitutionId == query.DestinationInstitutionId.Value);
        if (!string.IsNullOrEmpty(query.TransactionExternalId))
            source = source.Where(item => item.TransactionExternalId.StartsWith(query.TransactionExternalId));
        if (!string.IsNullOrEmpty(query.TraceNumber))
            source = source.Where(item => item.TraceNumber.StartsWith(query.TraceNumber));
        if (query.TransactionType.HasValue)
            source = source.Where(item => item.Type == query.TransactionType.Value);
        if (query.MinimumAmount.HasValue)
            source = source.Where(item => item.Amount >= query.MinimumAmount.Value);
        if (query.MaximumAmount.HasValue)
            source = source.Where(item => item.Amount <= query.MaximumAmount.Value);

        return source;
    }

    private IQueryable<AchTransaction> ApplyFunctionalFilters(
        IQueryable<AchTransaction> source,
        OutgoingTransactionMonitoringQuery query)
    {
        if (query.HasReturn.HasValue)
            source = query.HasReturn.Value
                ? source.Where(item => item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr))
                : source.Where(item => !item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr));

        if (query.RequiresAttention.HasValue)
        {
            source = query.RequiresAttention.Value
                ? source.Where(item =>
                    _context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id
                        && ((attempt.IsTechnicalFailure && !_context.ContrapartidaDispatchAttempts.Any(success => success.DispatchItem.AchTransactionId == item.Id && success.IsSuccessful))
                            || attempt.RequiresManualReview))
                    || _context.AchResponses.Any(response => response.AchTransactionId == item.Id
                        && (response.CorrelationStatus == AchResponseCorrelationStatus.Ambiguous
                            || response.CorrelationStatus == AchResponseCorrelationStatus.ManualReviewRequired)))
                : source.Where(item =>
                    !_context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id
                        && ((attempt.IsTechnicalFailure && !_context.ContrapartidaDispatchAttempts.Any(success => success.DispatchItem.AchTransactionId == item.Id && success.IsSuccessful))
                            || attempt.RequiresManualReview))
                    && !_context.AchResponses.Any(response => response.AchTransactionId == item.Id
                        && (response.CorrelationStatus == AchResponseCorrelationStatus.Ambiguous
                            || response.CorrelationStatus == AchResponseCorrelationStatus.ManualReviewRequired)));
        }

        source = ApplyProcessFilter(source, query.ProcessStatus);
        source = ApplyInitialResultFilter(source, query.InitialResult);
        source = ApplySubsequentFilter(source, query.SubsequentSituation);
        return source;
    }

    private IQueryable<AchTransaction> ApplyProcessFilter(IQueryable<AchTransaction> source, string? value)
        => value?.ToLowerInvariant() switch
        {
            "created" => source.Where(item => item.ContrapartidaDispatchItem == null
                && !item.StateEvents.Any()
                && !item.FileExportMemberships.Any()),
            "processing" => source.Where(item => item.ContrapartidaDispatchItem != null
                && !_context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id && attempt.IsSuccessful)
                && !item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.AppliedTacitly || evt.ToState == AchTransferStateEnum.Certified
                    || evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)
                && !item.FileExportMemberships.Any()),
            "technicalerror" => source.Where(item =>
                _context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id && attempt.IsTechnicalFailure)
                && !_context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id && attempt.IsSuccessful)),
            "processed" => source.Where(item =>
                _context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id && attempt.IsSuccessful)
                || item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.AppliedTacitly || evt.ToState == AchTransferStateEnum.Certified
                    || evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)
                || item.FileExportMemberships.Any()),
            _ => source
        };

    private IQueryable<AchTransaction> ApplyInitialResultFilter(IQueryable<AchTransaction> source, string? value)
        => value?.ToLowerInvariant() switch
        {
            "certified" => source.Where(item => item.State == AchTransferStateEnum.Certified
                || item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.Certified)),
            "accepted" => source.Where(item => item.State == AchTransferStateEnum.AppliedTacitly
                || item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.AppliedTacitly)),
            "rejected" => source.Where(item => _context.ContrapartidaDispatchAttempts.Any(attempt =>
                attempt.DispatchItem.AchTransactionId == item.Id && attempt.IsFunctionalRejection)),
            "integrationsuccessful" => source.Where(item => _context.ContrapartidaDispatchAttempts.Any(attempt =>
                attempt.DispatchItem.AchTransactionId == item.Id && attempt.IsSuccessful)),
            "notdetermined" => source.Where(item =>
                item.State != AchTransferStateEnum.Certified
                && item.State != AchTransferStateEnum.AppliedTacitly
                && !item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.Certified || evt.ToState == AchTransferStateEnum.AppliedTacitly)
                && !_context.ContrapartidaDispatchAttempts.Any(attempt => attempt.DispatchItem.AchTransactionId == item.Id
                    && (attempt.IsFunctionalRejection || attempt.IsSuccessful))),
            _ => source
        };

    private static IQueryable<AchTransaction> ApplySubsequentFilter(IQueryable<AchTransaction> source, string? value)
        => value?.ToLowerInvariant() switch
        {
            "returnedlater" => source.Where(item => item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)
                && item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.AppliedTacitly || evt.ToState == AchTransferStateEnum.Certified)),
            "returned" => source.Where(item => item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)),
            "none" => source.Where(item => !item.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)),
            _ => source
        };

    private static IQueryable<AchTransaction> ApplyOrdering(
        IQueryable<AchTransaction> source,
        string sortBy,
        string direction)
    {
        var ascending = direction.Equals("asc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "amount" => ascending
                ? source.OrderBy(item => item.Amount).ThenBy(item => item.Id)
                : source.OrderByDescending(item => item.Amount).ThenByDescending(item => item.Id),
            "identifier" => ascending
                ? source.OrderBy(item => item.TransactionExternalId).ThenBy(item => item.Id)
                : source.OrderByDescending(item => item.TransactionExternalId).ThenByDescending(item => item.Id),
            "lastupdatedat" => ascending
                ? source.OrderBy(item => item.UpdatedAt).ThenBy(item => item.Id)
                : source.OrderByDescending(item => item.UpdatedAt).ThenByDescending(item => item.Id),
            _ => ascending
                ? source.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
                : source.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
    }

    private Expression<Func<AchTransaction, MonitoringRow>> BuildProjection()
        => transaction => new MonitoringRow
        {
            Id = transaction.Id,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            TransactionExternalId = transaction.TransactionExternalId,
            TraceNumber = transaction.TraceNumber,
            ClearingHouseCode = transaction.AchCycle.ClearingHouse!.Code,
            ClearingHouseName = transaction.AchCycle.ClearingHouse.Name,
            CycleId = transaction.AchCycleId,
            CycleName = transaction.AchCycle.CycleName,
            DestinationInstitutionName = transaction.DestinationInstitution.Name,
            DestinationAccountNumber = transaction.DestinationAccountNumber,
            TransactionType = transaction.Type,
            Amount = transaction.Amount,
            Origin = transaction.Origin,
            MonetaryRoute = transaction.MonetaryIntegrationRoute,
            ClassifiedAtUtc = transaction.ClassifiedAtUtc,
            ClassificationVersion = transaction.ClassificationVersion,
            HasDispatchItem = transaction.ContrapartidaDispatchItem != null,
            HasSuccessfulIntegration = transaction.ContrapartidaDispatchItem != null
                && transaction.ContrapartidaDispatchItem.Attempts.Any(attempt => attempt.IsSuccessful),
            HasFunctionalRejection = transaction.ContrapartidaDispatchItem != null
                && transaction.ContrapartidaDispatchItem.Attempts.Any(attempt => attempt.IsFunctionalRejection),
            HasTechnicalFailure = transaction.ContrapartidaDispatchItem != null
                && transaction.ContrapartidaDispatchItem.Attempts.Any(attempt => attempt.IsTechnicalFailure),
            HasManualReview = transaction.ContrapartidaDispatchItem != null
                && transaction.ContrapartidaDispatchItem.Attempts.Any(attempt => attempt.RequiresManualReview),
            HasAccepted = transaction.State == AchTransferStateEnum.AppliedTacitly
                || transaction.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.AppliedTacitly),
            HasCertified = transaction.State == AchTransferStateEnum.Certified
                || transaction.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.Certified),
            HasReturn = transaction.StateEvents.Any(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator
                || evt.ToState == AchTransferStateEnum.ReturnedByEpr),
            HasAmbiguousCorrelation = _context.AchResponses.Any(response => response.AchTransactionId == transaction.Id
                && (response.CorrelationStatus == AchResponseCorrelationStatus.Ambiguous
                    || response.CorrelationStatus == AchResponseCorrelationStatus.ManualReviewRequired)),
            HasFileMembership = transaction.FileExportMemberships.Any(),
            LatestStateEventAtUtc = transaction.StateEvents
                .Select(evt => (DateTime?)evt.OccurredAtUtc)
                .Max(),
            LatestAttemptAtUtc = transaction.ContrapartidaDispatchItem == null
                ? null
                : transaction.ContrapartidaDispatchItem.Attempts
                    .Select(attempt => attempt.FinishedAtUtc ?? attempt.StartedAtUtc)
                    .Max(),
            LatestFileEventAtUtc = transaction.FileExportMemberships
                .Select(item => (DateTime?)item.IncludedAtUtc)
                .Max(),
            LatestResponseAtUtc = _context.AchResponses
                .Where(response => response.AchTransactionId == transaction.Id)
                .Select(response => (DateTime?)response.FechaRecepcion)
                .Max(),
            ReturnCode = transaction.StateEvents
                .Where(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)
                .OrderByDescending(evt => evt.OccurredAtUtc)
                .ThenByDescending(evt => evt.Id)
                .Select(evt => evt.ReasonCode)
                .FirstOrDefault(),
            ReturnDescription = transaction.StateEvents
                .Where(evt => evt.ToState == AchTransferStateEnum.ReturnedByOperator || evt.ToState == AchTransferStateEnum.ReturnedByEpr)
                .OrderByDescending(evt => evt.OccurredAtUtc)
                .ThenByDescending(evt => evt.Id)
                .Select(evt => evt.ResolvedReasonDescription)
                .FirstOrDefault(),
            FileName = transaction.FileExportMemberships
                .OrderByDescending(item => item.IncludedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => item.AchFileExport.FileName)
                .FirstOrDefault(),
            FileVersion = transaction.FileExportMemberships
                .OrderByDescending(item => item.IncludedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => item.AchFileExport.Version)
                .FirstOrDefault(),
            FileLifecycleStatus = transaction.FileExportMemberships
                .OrderByDescending(item => item.IncludedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => (AchFileExportLifecycleStatus?)item.AchFileExport.LifecycleStatus)
                .FirstOrDefault(),
            FileTransmissionReference = transaction.FileExportMemberships
                .OrderByDescending(item => item.IncludedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => item.AchFileExport.TransmissionReference)
                .FirstOrDefault(),
            FileTransmittedAtUtc = transaction.FileExportMemberships
                .OrderByDescending(item => item.IncludedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => item.AchFileExport.TransmittedAtUtc)
                .FirstOrDefault()
        };

    private OutgoingTransactionMonitoringListItem MapListItem(MonitoringRow row)
    {
        var facts = new OutgoingTransactionMonitoringFacts(
            row.HasDispatchItem,
            row.HasSuccessfulIntegration,
            row.HasFunctionalRejection,
            row.HasTechnicalFailure,
            row.HasAccepted,
            row.HasCertified,
            row.HasReturn,
            row.HasManualReview,
            row.HasAmbiguousCorrelation,
            row.HasFileMembership);
        var status = _statusPolicy.Consolidate(facts);
        var fileStatus = FileLifecycle(row.FileLifecycleStatus, row.FileTransmissionReference, row.FileTransmittedAtUtc);

        return new OutgoingTransactionMonitoringListItem
        {
            Id = row.Id,
            CreatedAtUtc = row.CreatedAt,
            TransactionExternalId = row.TransactionExternalId,
            TraceNumber = row.TraceNumber,
            ClearingHouseCode = row.ClearingHouseCode,
            ClearingHouseDisplayName = row.ClearingHouseName,
            CycleId = row.CycleId,
            CycleDisplayName = row.CycleName,
            DestinationInstitutionDisplayName = row.DestinationInstitutionName,
            TransactionTypeCode = row.TransactionType.ToString(),
            TransactionTypeDisplayName = TransactionTypeDisplay(row.TransactionType),
            Amount = row.Amount,
            MaskedDestinationAccount = MaskAccount(row.DestinationAccountNumber),
            ProcessStatusCode = status.ProcessStatusCode,
            ProcessStatusDisplayName = status.ProcessStatusDisplayName,
            InitialResultCode = status.InitialResultCode,
            InitialResultDisplayName = status.InitialResultDisplayName,
            SubsequentSituationCode = status.SubsequentSituationCode,
            SubsequentSituationDisplayName = status.SubsequentSituationDisplayName,
            HasReturn = row.HasReturn,
            ReturnCode = EmptyToNull(row.ReturnCode),
            ReturnDescription = EmptyToNull(row.ReturnDescription),
            FileName = EmptyToNull(row.FileName),
            FileVersion = row.FileVersion,
            FileLifecycleStatusCode = fileStatus.Code,
            FileLifecycleStatusDisplayName = fileStatus.Label,
            LastUpdatedAtUtc = LastUpdated(row),
            RequiresAttention = status.RequiresAttention,
            AttentionReason = status.AttentionReason
        };
    }

    private OutgoingTransactionMonitoringQuery Normalize(OutgoingTransactionMonitoringQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var now = _timeProvider.GetUtcNow();
        var from = query.FromUtc ?? now.AddDays(-7);
        var to = query.ToUtc ?? now;
        if (from > to || to - from > TimeSpan.FromDays(90))
            throw new OutgoingTransactionMonitoringException(
                "OUTGOING_MONITOR_INVALID_DATE_RANGE",
                "El rango de fechas debe ser válido y no puede superar 90 días.");
        if (query.PageNumber < 1)
            throw new OutgoingTransactionMonitoringException("OUTGOING_MONITOR_INVALID_PAGE", "La página debe ser mayor o igual a 1.");
        if (!AllowedPageSizes.Contains(query.PageSize))
            throw new OutgoingTransactionMonitoringException(
                "OUTGOING_MONITOR_PAGE_SIZE_EXCEEDED",
                "El tamaño de página permitido es 10, 25, 50 o 100.");
        if (!AllowedSorts.Contains(query.SortBy) || !(query.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            || query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)))
            throw new OutgoingTransactionMonitoringException("OUTGOING_MONITOR_INVALID_SORT", "El orden solicitado no es válido.");
        if (query.MinimumAmount is < 0 || query.MaximumAmount is < 0 || query.MinimumAmount > query.MaximumAmount)
            throw new OutgoingTransactionMonitoringException("OUTGOING_MONITOR_INVALID_AMOUNT_RANGE", "El rango de importes no es válido.");

        return query with
        {
            FromUtc = from,
            ToUtc = to,
            CycleId = NormalizeText(query.CycleId, 64, "ciclo"),
            TransactionExternalId = NormalizeIdentifier(query.TransactionExternalId, 64, "identificador"),
            TraceNumber = NormalizeIdentifier(query.TraceNumber, 32, "número de seguimiento"),
            SortBy = query.SortBy.Trim(),
            SortDirection = query.SortDirection.Trim()
        };
    }

    private static string? NormalizeText(string? value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new OutgoingTransactionMonitoringException("OUTGOING_MONITOR_INVALID_FILTER", $"El filtro de {field} es demasiado largo.");
        return normalized;
    }

    private static string? NormalizeIdentifier(string? value, int maximumLength, string field)
    {
        var normalized = NormalizeText(value, maximumLength, field);
        if (normalized is null) return null;
        if (normalized.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' or '.')))
            throw new OutgoingTransactionMonitoringException("OUTGOING_MONITOR_INVALID_FILTER", $"El filtro de {field} contiene caracteres no permitidos.");
        return normalized;
    }

    private static IReadOnlyList<OutgoingTransactionTimelineEvent> BuildTimeline(
        MonitoringRow row,
        IReadOnlyList<StateEventRow> stateEvents,
        IReadOnlyList<AttemptRow> attempts,
        IReadOnlyList<FileRow> files,
        IReadOnlyList<ResponseRow> responses,
        IReadOnlyList<IncomingEventRow> incomingEvents,
        IReadOnlyList<LiquidityRow> liquidityDecisions)
    {
        var events = new List<OutgoingTransactionTimelineEvent>
        {
            Event(row.CreatedAt.UtcDateTime, "Creation", "Creación", "Transacción creada",
                $"La transacción fue registrada en el ciclo {row.CycleName}.", "Recorded", "Registrada", "info", "AchTransaction")
        };
        if (row.ClassifiedAtUtc.HasValue)
            events.Add(Event(row.ClassifiedAtUtc.Value, "Classification", "Clasificación", "Clasificación confirmada",
                "La transacción quedó clasificada de forma persistida como salida.", "Outgoing", "Salida", "success", "AchTransaction"));

        events.AddRange(attempts.Select(item => Event(
            item.FinishedAtUtc ?? item.StartedAtUtc,
            "MonetaryIntegration",
            "Integración monetaria",
            $"Intento {item.AttemptNumber}",
            item.IsSuccessful ? "La integración monetaria terminó correctamente."
                : item.IsFunctionalRejection ? "La integración informó un rechazo funcional."
                : item.IsTechnicalFailure ? "La integración presentó un error técnico."
                : "El resultado de la integración no está determinado.",
            item.IsSuccessful ? "Successful" : item.IsFunctionalRejection ? "Rejected" : item.IsTechnicalFailure ? "TechnicalError" : "NotDetermined",
            item.IsSuccessful ? "Exitosa" : item.IsFunctionalRejection ? "Rechazada" : item.IsTechnicalFailure ? "Error técnico" : "No determinado",
            item.IsSuccessful ? "success" : item.IsTechnicalFailure ? "error" : "warning",
            "ContrapartidaDispatchAttempt",
            item.IsTechnicalFailure)));

        events.AddRange(files.Select(item => Event(item.IncludedAtUtc, "FileInclusion", "Inclusión en archivo",
            "Incluida en archivo de salida",
            $"Archivo {item.FileName}, versión {(item.Version?.ToString() ?? "no determinada")}, posición {item.FileSequence}.",
            "Included", "Incluida", "success", "AchFileExportTransaction")));

        events.AddRange(stateEvents.Select(item => Event(
            item.OccurredAtUtc,
            IsReturnState(item.ToState) ? "Return" : item.ToState == AchTransferStateEnum.Certified ? "Certification" : "Acceptance",
            IsReturnState(item.ToState) ? "Devolución" : item.ToState == AchTransferStateEnum.Certified ? "Certificación" : "Aceptación",
            StateDisplay(item.ToState),
            EmptyToNull(item.ReasonDescription) ?? EmptyToNull(item.ReasonCode) ?? "Hecho persistido sin descripción adicional.",
            item.ToState.ToString(),
            StateDisplay(item.ToState),
            IsReturnState(item.ToState) ? "warning" : "success",
            "AchTransactionStateEvent")));

        events.AddRange(responses.Select(item => Event(item.ReceivedAtUtc, "DifferentialResponse", "Respuesta diferencial",
            "Respuesta relacionada",
            string.IsNullOrWhiteSpace(item.CauseDescription) ? "Respuesta correlacionada sin descripción causal." : item.CauseDescription!,
            item.ExternalStatusCode,
            "Respuesta registrada",
            item.CorrelationStatus is AchResponseCorrelationStatus.Ambiguous or AchResponseCorrelationStatus.ManualReviewRequired ? "warning" : "info",
            "AchResponse")));

        events.AddRange(incomingEvents.Select(item => Event(item.OccurredAtUtc, "IncomingEvidence", "Procesamiento relacionado",
            IncomingEventTitle(item.EventType), SanitizeMessage(item.Message), item.EventStatus, IncomingEventOutcome(item.EventStatus),
            item.EventStatus.Contains("error", StringComparison.OrdinalIgnoreCase) ? "error" : "info", "IncomingNachaProcessingEvent")));

        events.AddRange(liquidityDecisions.Select(item => Event(item.OccurredAtUtc, "CycleAssignment", "Asignación de ciclo",
            "Decisión de ciclo registrada",
            item.ToCycleId is null ? SanitizeMessage(item.Reason) : $"Cambio de {item.FromCycleId} a {item.ToCycleId}. {SanitizeMessage(item.Reason)}",
            item.DecisionType, "Decisión registrada", "info", "LiquidityOptimizationDecision")));

        return events.OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.StageCode, StringComparer.Ordinal)
            .ToArray();
    }

    private static OutgoingTransactionTimelineEvent Event(DateTime at, string stageCode, string stage, string title,
        string description, string outcomeCode, string outcome, string severity, string source, bool technical = false)
        => new(DateTime.SpecifyKind(at, DateTimeKind.Utc), stageCode, stage, title, description, outcomeCode, outcome, severity, source, technical);

    private static IEnumerable<string> BuildWarnings(MonitoringRow row, IReadOnlyList<FileRow> files)
    {
        if (files.Count == 0)
            yield return "Sin evidencia de inclusión en un archivo de salida.";
        if (files.Any(item => !HasTransmissionEvidence(item.TransmissionReference, item.TransmittedAtUtc)))
            yield return "Sin evidencia de transmisión para uno o más archivos relacionados.";
        if (row.HasAmbiguousCorrelation)
            yield return "Existe una correlación que requiere revisión.";
    }

    private static OutgoingTransactionFileDetail MapFileDetail(FileRow item)
    {
        var status = FileLifecycle(item.LifecycleStatus, item.TransmissionReference, item.TransmittedAtUtc);
        var transmission = HasTransmissionEvidence(item.TransmissionReference, item.TransmittedAtUtc);
        var acknowledgement = transmission && item.AcknowledgedAtUtc.HasValue && !string.IsNullOrWhiteSpace(item.AcknowledgementCode);
        return new OutgoingTransactionFileDetail(item.FileId, item.FileName, item.Version, item.FileSequence,
            item.IncludedAtUtc, status.Code, status.Label, transmission, transmission ? item.TransmittedAtUtc : null,
            acknowledgement, acknowledgement ? item.AcknowledgedAtUtc : null, acknowledgement ? item.AcknowledgementCode : null);
    }

    private static (string Code, string Label) FileLifecycle(AchFileExportLifecycleStatus? lifecycle, string? reference, DateTime? transmittedAt)
    {
        if (HasTransmissionEvidence(reference, transmittedAt))
            return lifecycle >= AchFileExportLifecycleStatus.Acknowledged
                ? ("Acknowledged", "Acuse comprobado")
                : ("Transmitted", "Transmisión comprobada");
        return lifecycle switch
        {
            AchFileExportLifecycleStatus.Protected or AchFileExportLifecycleStatus.AvailableForDelivery => ("Protected", "Protegido; sin evidencia de transmisión"),
            AchFileExportLifecycleStatus.Signed => ("Signed", "Firmado; sin evidencia de transmisión"),
            AchFileExportLifecycleStatus.Validated => ("Validated", "Validado; sin evidencia de transmisión"),
            AchFileExportLifecycleStatus.Generated => ("Generated", "Generado; sin evidencia de transmisión"),
            _ => ("NotDetermined", "No determinado")
        };
    }

    private static bool HasTransmissionEvidence(string? reference, DateTime? transmittedAt)
        => transmittedAt.HasValue && !string.IsNullOrWhiteSpace(reference);

    private static bool IsReturnState(AchTransferStateEnum state)
        => state is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr;

    private static string StateDisplay(AchTransferStateEnum state) => state switch
    {
        AchTransferStateEnum.AppliedTacitly => "Aceptada",
        AchTransferStateEnum.Certified => "Certificada",
        AchTransferStateEnum.ReturnedByOperator => "Devuelta por el operador",
        AchTransferStateEnum.ReturnedByEpr => "Devuelta por la entidad receptora",
        _ => "Pendiente"
    };

    private static string TransactionTypeDisplay(TransactionTypeEnum type) => type switch
    {
        TransactionTypeEnum.Credit => "Crédito",
        TransactionTypeEnum.Debit => "Débito",
        TransactionTypeEnum.Prenotification => "Prenotificación",
        TransactionTypeEnum.Reversal => "Reversión",
        TransactionTypeEnum.Return => "Devolución",
        _ => "No determinado"
    };

    private static string CorrelationDisplay(AchResponseCorrelationStatus status) => status switch
    {
        AchResponseCorrelationStatus.Matched => "Correlacionada",
        AchResponseCorrelationStatus.NotFound => "Sin transacción relacionada",
        AchResponseCorrelationStatus.Ambiguous => "Correlación ambigua",
        AchResponseCorrelationStatus.ManualReviewRequired => "Requiere revisión",
        _ => "No determinada"
    };

    private static string IncomingEventTitle(string eventType) => eventType switch
    {
        "LinkingExitoso" => "Vinculación confirmada",
        "TransicionDisparada" => "Cambio de estado registrado",
        "EventoDuplicadoIgnorado" => "Evento duplicado ignorado",
        "TransicionBloqueada" => "Cambio de estado bloqueado",
        _ => "Hecho relacionado registrado"
    };

    private static string IncomingEventOutcome(string status)
        => status.Equals("Exitoso", StringComparison.OrdinalIgnoreCase) ? "Exitoso" : "Registrado";

    private static string SanitizeMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Sin descripción adicional.";
        var trimmed = value.Trim();
        if (trimmed.Contains('<') || trimmed.Contains('>')) return "Contenido técnico no disponible en el monitor.";
        return trimmed.Length <= 240 ? trimmed : string.Concat(trimmed.AsSpan(0, 237), "...");
    }

    private static string MaskAccount(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return "No disponible";
        var visible = normalized.Length <= 4 ? normalized : normalized[^4..];
        return $"******{visible}";
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset LastUpdated(MonitoringRow row)
    {
        var latest = row.UpdatedAt > row.CreatedAt ? row.UpdatedAt : row.CreatedAt;
        foreach (var related in new[] { row.LatestStateEventAtUtc, row.LatestAttemptAtUtc, row.LatestFileEventAtUtc, row.LatestResponseAtUtc })
        {
            if (!related.HasValue) continue;
            var candidate = new DateTimeOffset(DateTime.SpecifyKind(related.Value, DateTimeKind.Utc));
            if (candidate > latest) latest = candidate;
        }
        return latest;
    }

    private sealed class MonitoringRow
    {
        public int Id { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public string TransactionExternalId { get; init; } = string.Empty;
        public string TraceNumber { get; init; } = string.Empty;
        public string ClearingHouseCode { get; init; } = string.Empty;
        public string ClearingHouseName { get; init; } = string.Empty;
        public string CycleId { get; init; } = string.Empty;
        public string CycleName { get; init; } = string.Empty;
        public string DestinationInstitutionName { get; init; } = string.Empty;
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public TransactionTypeEnum TransactionType { get; init; }
        public decimal Amount { get; init; }
        public AchTransactionOrigin Origin { get; init; }
        public AchMonetaryIntegrationRoute MonetaryRoute { get; init; }
        public DateTime? ClassifiedAtUtc { get; init; }
        public int ClassificationVersion { get; init; }
        public bool HasDispatchItem { get; init; }
        public bool HasSuccessfulIntegration { get; init; }
        public bool HasFunctionalRejection { get; init; }
        public bool HasTechnicalFailure { get; init; }
        public bool HasAccepted { get; init; }
        public bool HasCertified { get; init; }
        public bool HasReturn { get; init; }
        public bool HasManualReview { get; init; }
        public bool HasAmbiguousCorrelation { get; init; }
        public bool HasFileMembership { get; init; }
        public DateTime? LatestStateEventAtUtc { get; init; }
        public DateTime? LatestAttemptAtUtc { get; init; }
        public DateTime? LatestFileEventAtUtc { get; init; }
        public DateTime? LatestResponseAtUtc { get; init; }
        public string? ReturnCode { get; init; }
        public string? ReturnDescription { get; init; }
        public string? FileName { get; init; }
        public int? FileVersion { get; init; }
        public AchFileExportLifecycleStatus? FileLifecycleStatus { get; init; }
        public string? FileTransmissionReference { get; init; }
        public DateTime? FileTransmittedAtUtc { get; init; }
    }

    private sealed record StateEventRow(DateTime OccurredAtUtc, AchTransferStateEnum FromState, AchTransferStateEnum ToState, string? ReasonCode, string? ReasonDescription);
    private sealed record AttemptRow(int AttemptNumber, DateTime StartedAtUtc, DateTime? FinishedAtUtc, bool IsSuccessful,
        bool IsFunctionalRejection, bool IsTechnicalFailure, bool RequiresManualReview, string ExternalResponseCode,
        string ExternalResponseMessage, string ErrorCode, string ErrorMessage, string MethodName, string ExecutionMode,
        long DurationMs, string CorrelationId);
    private sealed record FileRow(int FileId, string FileName, int? Version, int FileSequence, DateTime IncludedAtUtc,
        DateTime GeneratedAtUtc, AchFileExportLifecycleStatus LifecycleStatus, string? TransmissionReference,
        DateTime? TransmittedAtUtc, DateTime? AcknowledgedAtUtc, string? AcknowledgementCode);
    private sealed record ResponseRow(Guid Id, DateTime ReceivedAtUtc, TipoRespuestaAch ResponseType, string ExternalStatusCode,
        string? CauseCode, string? CauseDescription, AchResponseCorrelationStatus CorrelationStatus);
    private sealed record IncomingEventRow(DateTime OccurredAtUtc, string EventType, string EventStatus, string Message);
    private sealed record LiquidityRow(DateTime OccurredAtUtc, string DecisionType, string Reason, string FromCycleId, string? ToCycleId);
}
