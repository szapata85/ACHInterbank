using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchCauseCodePolicy(AchDbContext context) : IAchCauseCodePolicy
{
    public async Task<AchCauseCodePolicyResult> EvaluateAsync(AchCauseCodePolicyRequest request, CancellationToken ct = default)
    {
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var issues = new List<AchCauseCodePolicyIssue>();
        if (string.IsNullOrWhiteSpace(code))
            return new(false, AchCauseCodeRail.Unknown, AchCauseCodeKind.Unknown, true, [new("CODE_REQUIRED", "Code is required.", AchCauseCodePolicySeverity.Error)]);

        var kind = ClassifyKind(code);
        var rail = ResolveRail(request.ClearingHouseCode);

        if (code == "DXX-LIQ")
        {
            var allowedInternal = request.Flow == AchCauseCodeFlow.InternalOnly;
            if (!allowedInternal) issues.Add(new("INTERNAL_CODE_EXTERNAL_FLOW", "Internal code cannot be used on external flows.", AchCauseCodePolicySeverity.Error));
            return new(allowedInternal, AchCauseCodeRail.Internal, AchCauseCodeKind.Internal, true, issues);
        }

        if (kind == AchCauseCodeKind.FileRejection)
        {
            var allowed = request.Flow is AchCauseCodeFlow.FileRejectTotal or AchCauseCodeFlow.FileRejectPartial or AchCauseCodeFlow.CommandCenter;
            if (!allowed) issues.Add(new("FLOW_MISMATCH_DXX", "Dxx cannot be used as return reason flow.", AchCauseCodePolicySeverity.Error));
            return new(allowed, rail == AchCauseCodeRail.Unknown ? AchCauseCodeRail.Sta : rail, kind, true, issues);
        }

        if (kind == AchCauseCodeKind.TechnicalIntegration)
        {
            var allowed = request.Flow is AchCauseCodeFlow.OperatorResponse or AchCauseCodeFlow.CommandCenter;
            if (!allowed) issues.Add(new("FLOW_MISMATCH_IXXX", "Ixxx cannot be used as return reason flow.", AchCauseCodePolicySeverity.Error));
            return new(allowed, rail == AchCauseCodeRail.Unknown ? AchCauseCodeRail.Sta : rail, kind, true, issues);
        }

        var isReturnFlow = request.Flow is AchCauseCodeFlow.OutboundReturn or AchCauseCodeFlow.IncomingReturn or AchCauseCodeFlow.ReturnOfReturn;
        if (!isReturnFlow)
            issues.Add(new("FLOW_MISMATCH_RXX", "Rxx/DEVxx should not be used as technical rejection flow.", AchCauseCodePolicySeverity.Error));

        var query = context.AchReturnCodes.AsNoTracking().Where(x => x.IsActive && x.Code == code);
        if (request.ClearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == request.ClearingHouseId.Value);
        var existsForRail = await query.AnyAsync(ct);
        if (!existsForRail)
            issues.Add(new("RAIL_MISMATCH_OR_NOT_CONFIGURED", "Code is not configured for requested rail.", AchCauseCodePolicySeverity.Error));

        if (request.Flow == AchCauseCodeFlow.ReturnOfReturn && !string.IsNullOrWhiteSpace(request.OriginalReasonCode))
        {
            var ror = await context.AchReturnOfReturnPolicies.AsNoTracking().AnyAsync(x =>
                x.IsActive &&
                (!request.ClearingHouseId.HasValue || x.ClearingHouseId == request.ClearingHouseId.Value) &&
                x.OriginalReturnCode == request.OriginalReasonCode!.Trim().ToUpperInvariant() &&
                x.AllowedNewReturnCodesCsv.Contains(code), ct);
            if (!ror) issues.Add(new("ROR_POLICY_REJECTED", "Return-of-return policy does not allow this reason for rail.", AchCauseCodePolicySeverity.Error));
        }

        var normativePending = kind is AchCauseCodeKind.ReturnReason or AchCauseCodeKind.ReturnOfReturnReason;
        if (normativePending)
            issues.Add(new("NORMATIVE_PENDING", "Current catalog is technically valid but pending normative/productive approval.", AchCauseCodePolicySeverity.Warning));

        var allowedResult = issues.All(x => x.Severity != AchCauseCodePolicySeverity.Error);
        return new(allowedResult, rail, request.Flow == AchCauseCodeFlow.ReturnOfReturn ? AchCauseCodeKind.ReturnOfReturnReason : kind, normativePending, issues);
    }

    private static AchCauseCodeKind ClassifyKind(string code)
    {
        if (code == "DXX-LIQ") return AchCauseCodeKind.Internal;
        if (code == "DEV14" || (code.Length == 3 && code.StartsWith('R') && char.IsDigit(code[1]) && char.IsDigit(code[2]))) return AchCauseCodeKind.ReturnReason;
        if (code.StartsWith('D') && code.Length >= 3) return AchCauseCodeKind.FileRejection;
        if (code.StartsWith('I')) return AchCauseCodeKind.TechnicalIntegration;
        return AchCauseCodeKind.Unknown;
    }

    private static AchCauseCodeRail ResolveRail(string? clearingHouseCode)
    {
        var c = (clearingHouseCode ?? string.Empty).Trim().ToUpperInvariant();
        return c switch
        {
            "ACH" => AchCauseCodeRail.AchColombia,
            "CENIT" => AchCauseCodeRail.Cenit,
            "STA" => AchCauseCodeRail.Sta,
            _ => AchCauseCodeRail.Unknown
        };
    }
}
