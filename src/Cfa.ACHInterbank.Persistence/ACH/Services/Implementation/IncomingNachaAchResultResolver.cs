using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class IncomingNachaAchResultResolver : IIncomingNachaAchResultResolver
{
    private readonly AchDbContext _context;

    public IncomingNachaAchResultResolver(AchDbContext context) => _context = context;

    public async Task<IncomingNachaAchResultResolution> ResolveAsync(
        IncomingNachaAchResultRequest request,
        CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return Unresolved(code, "ACH_RESULT_CODE_EMPTY");
        }

        var processDate = request.ProcessedAtUtc.Date;
        var candidates = await _context.AchReturnCodes.AsNoTracking()
            .Where(x => x.ClearingHouseId == request.ClearingHouseId
                && x.Code == code
                && x.IsActive
                && x.EffectiveFrom.Date <= processDate
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= processDate)
                && (x.FlowType == AchReturnFlowType.Any || x.FlowType == request.FlowType))
            .Where(x => (request.IsDebit && x.AppliesToDebit)
                || (request.IsCredit && x.AppliesToCredit)
                || (request.IsPrenotification && x.AppliesToPrenotification)
                || (request.IsReturn && x.AppliesToReturn))
            .OrderByDescending(x => x.FlowType == request.FlowType)
            .ThenByDescending(x => x.EffectiveFrom)
            .Take(2)
            .ToListAsync(ct);

        if (candidates.Count != 1)
        {
            return Unresolved(code, candidates.Count == 0 ? "ACH_RESULT_CODE_NOT_FOUND" : "ACH_RESULT_CODE_AMBIGUOUS");
        }

        var match = candidates[0];
        return new IncomingNachaAchResultResolution(
            true,
            match.Id,
            match.Code,
            match.Description,
            match.BusinessOutcome,
            "ACH_RESULT_CODE_RESOLVED");
    }

    private static IncomingNachaAchResultResolution Unresolved(string code, string reason)
        => new(false, null, code, string.Empty, IncomingNachaBusinessOutcome.PendingResponse, reason);
}
