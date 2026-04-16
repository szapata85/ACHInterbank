using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchTraceabilityService : IAchTraceabilityService
{
    private readonly AchDbContext _context;
    private readonly IAchStateTransitionService _stateTransitionService;

    public AchTraceabilityService(AchDbContext context, IAchStateTransitionService stateTransitionService)
    {
        _context = context;
        _stateTransitionService = stateTransitionService;
    }

    public async Task<AchTransaction> CertifySol02Async(
        int transactionId,
        string? certificationReference,
        string? notes,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            operation = "SOL02",
            certificationReference,
            notes
        });

        return await _stateTransitionService.TransitionAsync(
            transactionId,
            AchTransferStateEnum.Certified,
            AchStateEventSourceEnum.Claims,
            reasonCode: "SOL02",
            payloadJson: payload,
            ct: ct);
    }

    public async Task<AchTraceabilityDetailDto?> GetTransactionTraceabilityAsync(int transactionId, CancellationToken ct = default)
    {
        var transaction = await _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchCycle)
                .ThenInclude(c => c!.ClearingHouse)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.StateEvents)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction is null)
        {
            return null;
        }

        var latestNachaFile = await _context.AchFileExports
            .AsNoTracking()
            .Where(export => export.AchCycleId == transaction.AchCycleId && export.ExportKind == "NACHA")
            .OrderByDescending(export => export.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

        var latestReturnFile = await _context.AchReturnsGenerated
            .AsNoTracking()
            .Where(row => row.OriginalTransactionId == transactionId)
            .OrderByDescending(row => row.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

        return new AchTraceabilityDetailDto
        {
            TransactionId = transaction.Id,
            TransactionExternalId = transaction.TransactionExternalId,
            Reference = transaction.Reference,
            TraceNumber = transaction.TraceNumber,
            OriginalTraceRef = transaction.OriginalTraceRef,
            TransactionCode = transaction.TransactionCode,
            Amount = transaction.Amount,
            EffectiveEntryDate = transaction.EffectiveEntryDate,
            AchCycleId = transaction.AchCycleId,
            AchCycleName = transaction.AchCycle?.CycleName ?? string.Empty,
            ClearingHouseName = transaction.AchCycle?.ClearingHouse?.Name ?? string.Empty,
            ClearingHouseCode = transaction.AchCycle?.ClearingHouse?.Code ?? string.Empty,
            CurrentNachaFileName = latestNachaFile?.FileName ?? string.Empty,
            CurrentNachaGeneratedAtUtc = latestNachaFile?.GeneratedAtUtc,
            ReturnFileName = latestReturnFile?.FileName ?? string.Empty,
            ReturnCycleId = latestReturnFile?.ReturnCycleId ?? string.Empty,
            ReturnOriginalTransactionId = latestReturnFile?.OriginalTransactionId,
            ReturnGeneratedAtUtc = latestReturnFile?.GeneratedAtUtc,
            SourceInstitutionName = transaction.SourceInstitution?.Name ?? string.Empty,
            DestinationInstitutionName = transaction.DestinationInstitution?.Name ?? string.Empty,
            State = transaction.State,
            StateChangedAtUtc = transaction.StateChangedAtUtc,
            SlaDeadlineAtUtc = transaction.SlaDeadlineAtUtc,
            ReturnReasonCode = !string.IsNullOrWhiteSpace(transaction.ReturnReasonCode) ? transaction.ReturnReasonCode : latestReturnFile?.ReturnReasonCode ?? string.Empty,
            Events = transaction.StateEvents
                .OrderBy(e => e.CreatedAt)
                .Select(e => new AchTraceabilityEventDto
                {
                    Id = e.Id,
                    CreatedAt = e.CreatedAt,
                    FromState = e.FromState,
                    ToState = e.ToState,
                    Source = e.Source,
                    ReasonCode = e.ReasonCode,
                    PayloadJson = e.PayloadJson
                })
                .ToList()
        };
    }

    public async Task<IReadOnlyList<AchTraceabilityReportRowDto>> GetTraceabilityReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        AchTransferStateEnum? state,
        string? achCycleId,
        CancellationToken ct = default)
    {
        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchCycle)
                .ThenInclude(c => c!.ClearingHouse)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.StateEvents)
            .AsQueryable();

        if (fromUtc.HasValue)
        {
            var from = fromUtc.Value.ToUniversalTime();
            query = query.Where(t => t.StateChangedAtUtc >= from);
        }

        if (toUtc.HasValue)
        {
            var to = toUtc.Value.ToUniversalTime();
            query = query.Where(t => t.StateChangedAtUtc <= to);
        }

        if (state.HasValue)
        {
            query = query.Where(t => t.State == state.Value);
        }

        if (!string.IsNullOrWhiteSpace(achCycleId))
        {
            var cycleIds = achCycleId
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (cycleIds.Length == 1)
            {
                query = query.Where(t => t.AchCycleId == cycleIds[0]);
            }
            else if (cycleIds.Length > 1)
            {
                query = query.Where(t => cycleIds.Contains(t.AchCycleId));
            }
        }

        var latestExportsByCycle = await _context.AchFileExports
            .AsNoTracking()
            .Where(export => export.ExportKind == "NACHA")
            .GroupBy(export => export.AchCycleId)
            .Select(group => group.OrderByDescending(export => export.GeneratedAtUtc).First())
            .ToDictionaryAsync(export => export.AchCycleId, export => export.FileName, ct);

        var rows = await query
            .OrderByDescending(t => t.StateChangedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.TransactionExternalId,
                t.Reference,
                t.TraceNumber,
                t.TransactionCode,
                t.Amount,
                t.AchCycleId,
                AchCycleName = t.AchCycle.CycleName,
                ClearingHouseName = t.AchCycle.ClearingHouse != null ? t.AchCycle.ClearingHouse.Name : string.Empty,
                ClearingHouseCode = t.AchCycle.ClearingHouse != null ? t.AchCycle.ClearingHouse.Code : string.Empty,
                t.EffectiveEntryDate,
                t.State,
                t.StateChangedAtUtc,
                t.ReturnReasonCode,
                EventsCount = t.StateEvents.Count,
                SourceInstitutionName = t.SourceInstitution.Name,
                DestinationInstitutionName = t.DestinationInstitution.Name
            })
            .ToListAsync(ct);

        return rows
            .Select(t => new AchTraceabilityReportRowDto
            {
                TransactionId = t.Id,
                TransactionExternalId = t.TransactionExternalId,
                Reference = t.Reference,
                TraceNumber = t.TraceNumber,
                TransactionCode = t.TransactionCode,
                Amount = t.Amount,
                AchCycleId = t.AchCycleId,
                AchCycleName = t.AchCycleName,
                ClearingHouseName = t.ClearingHouseName,
                ClearingHouseCode = t.ClearingHouseCode,
                CurrentNachaFileName = latestExportsByCycle.TryGetValue(t.AchCycleId, out var fileName) ? fileName : string.Empty,
                EffectiveEntryDate = t.EffectiveEntryDate,
                State = t.State,
                StateChangedAtUtc = t.StateChangedAtUtc,
                ReturnReasonCode = t.ReturnReasonCode,
                EventsCount = t.EventsCount,
                SourceInstitutionName = t.SourceInstitutionName,
                DestinationInstitutionName = t.DestinationInstitutionName
            })
            .ToList();
    }
}
