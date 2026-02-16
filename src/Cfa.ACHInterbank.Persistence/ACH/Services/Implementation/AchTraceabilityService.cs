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
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.StateEvents)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction is null)
        {
            return null;
        }

        return new AchTraceabilityDetailDto
        {
            TransactionId = transaction.Id,
            Reference = transaction.Reference,
            TraceNumber = transaction.TraceNumber,
            OriginalTraceRef = transaction.OriginalTraceRef,
            TransactionCode = transaction.TransactionCode,
            Amount = transaction.Amount,
            EffectiveEntryDate = transaction.EffectiveEntryDate,
            AchCycleId = transaction.AchCycleId,
            SourceInstitutionName = transaction.SourceInstitution?.Name ?? string.Empty,
            DestinationInstitutionName = transaction.DestinationInstitution?.Name ?? string.Empty,
            State = transaction.State,
            StateChangedAtUtc = transaction.StateChangedAtUtc,
            SlaDeadlineAtUtc = transaction.SlaDeadlineAtUtc,
            ReturnReasonCode = transaction.ReturnReasonCode,
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
            var cycleId = achCycleId.Trim();
            query = query.Where(t => t.AchCycleId == cycleId);
        }

        return await query
            .OrderByDescending(t => t.StateChangedAtUtc)
            .Select(t => new AchTraceabilityReportRowDto
            {
                TransactionId = t.Id,
                Reference = t.Reference,
                TraceNumber = t.TraceNumber,
                TransactionCode = t.TransactionCode,
                Amount = t.Amount,
                AchCycleId = t.AchCycleId,
                EffectiveEntryDate = t.EffectiveEntryDate,
                State = t.State,
                StateChangedAtUtc = t.StateChangedAtUtc,
                ReturnReasonCode = t.ReturnReasonCode,
                EventsCount = t.StateEvents.Count,
                SourceInstitutionName = t.SourceInstitution.Name,
                DestinationInstitutionName = t.DestinationInstitution.Name
            })
            .ToListAsync(ct);
    }
}
