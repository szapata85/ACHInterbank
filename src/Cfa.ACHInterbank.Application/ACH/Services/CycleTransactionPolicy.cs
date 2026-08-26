using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public sealed class CycleTransactionPolicy(
    IClearingHouseCyclePolicyResolver policyResolver,
    IClearingHouseToPaymentRailMapper railMapper,
    ICycleNumberResolver cycleNumberResolver) : ICycleTransactionPolicy
{
    public const string NotAllowedReasonCode = "CYCLE_TRANSACTION_NOT_ALLOWED";
    public const string UnresolvedReasonCode = "CYCLE_POLICY_UNRESOLVED";
    public const string AmbiguousClassReasonCode = "CYCLE_TRANSACTION_CLASS_AMBIGUOUS";

    public async Task<CycleTransactionPolicyResult> EvaluateAsync(
        CycleTransactionPolicyRequest request,
        CancellationToken ct = default)
    {
        var rail = railMapper.ResolveRail(new PaymentRailResolveRequest(
            request.ClearingHouseId,
            request.ClearingHouseCode,
            request.PaymentRailCode));
        var cycleNumber = cycleNumberResolver.Resolve(request.CycleName);
        var functionalClass = ResolveFunctionalClass(request);

        if (functionalClass == CycleFunctionalClass.Ambiguous)
        {
            return Result(false, AmbiguousClassReasonCode,
                "La clase funcional de la transacción no se puede resolver de forma inequívoca.");
        }

        ResolvedClearingHouseCyclePolicy policy;
        try
        {
            policy = await policyResolver.ResolveAsync(request.ClearingHouseId, request.OperationalDate, ct);
        }
        catch (InvalidOperationException exception)
        {
            return Result(false, UnresolvedReasonCode, exception.Message);
        }

        var candidates = policy.Cycles
            .Where(config => request.ClearingHouseCycleConfigId.HasValue
                ? config.Id == request.ClearingHouseCycleConfigId.Value
                : string.Equals(config.CycleName.Trim(), request.CycleName?.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count != 1)
        {
            return Result(false, UnresolvedReasonCode,
                "La configuración del ciclo no se resolvió de forma única dentro de la política vigente.");
        }

        var config = candidates[0];
        var allowed = functionalClass switch
        {
            CycleFunctionalClass.MonetaryCredit => config.AllowsMonetaryCredit,
            CycleFunctionalClass.MonetaryDebit => config.AllowsMonetaryDebit,
            CycleFunctionalClass.CreditPrenotification => config.AllowsCreditPrenotification,
            CycleFunctionalClass.DebitPrenotification => config.AllowsDebitPrenotification,
            CycleFunctionalClass.Return => config.AllowsReturn,
            CycleFunctionalClass.ReturnOfReturn => config.AllowsReturnOfReturn,
            _ => false
        };

        return allowed
            ? Result(true, "ALLOWED", "La política vigente permite la clase funcional en el ciclo.")
            : Result(false, NotAllowedReasonCode,
                $"La política {policy.PolicyVersion} no permite {FunctionalClassName(functionalClass)} en {config.CycleName}.");

        CycleTransactionPolicyResult Result(bool isAllowed, string reasonCode, string message)
            => new(isAllowed, reasonCode, message, rail.RailCode, cycleNumber, FunctionalClassName(functionalClass));
    }

    private static CycleFunctionalClass ResolveFunctionalClass(CycleTransactionPolicyRequest request)
    {
        if (request.IsReturnOfReturn)
        {
            return CycleFunctionalClass.ReturnOfReturn;
        }

        var isReturn = request.TransactionType == TransactionTypeEnum.Return
            || !string.IsNullOrWhiteSpace(request.ReturnReasonCode)
            || !string.IsNullOrWhiteSpace(request.OriginalTraceRef);
        if (isReturn)
        {
            return CycleFunctionalClass.Return;
        }

        var isPrenotification = request.IsPrenotification
            || request.TransactionType == TransactionTypeEnum.Prenotification;
        if (isPrenotification)
        {
            var direction = request.TransactionType == TransactionTypeEnum.Prenotification
                ? NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(request.TransactionCode)
                : request.TransactionType;
            return direction switch
            {
                TransactionTypeEnum.Credit => CycleFunctionalClass.CreditPrenotification,
                TransactionTypeEnum.Debit => CycleFunctionalClass.DebitPrenotification,
                _ => CycleFunctionalClass.Ambiguous
            };
        }

        return request.TransactionType switch
        {
            TransactionTypeEnum.Credit => CycleFunctionalClass.MonetaryCredit,
            TransactionTypeEnum.Debit => CycleFunctionalClass.MonetaryDebit,
            _ => CycleFunctionalClass.Ambiguous
        };
    }

    private static string FunctionalClassName(CycleFunctionalClass value) => value switch
    {
        CycleFunctionalClass.MonetaryCredit => "MonetaryCredit",
        CycleFunctionalClass.MonetaryDebit => "MonetaryDebit",
        CycleFunctionalClass.CreditPrenotification => "CreditPrenotification",
        CycleFunctionalClass.DebitPrenotification => "DebitPrenotification",
        CycleFunctionalClass.Return => "Return",
        CycleFunctionalClass.ReturnOfReturn => "ReturnOfReturn",
        _ => "Ambiguous"
    };

    private enum CycleFunctionalClass
    {
        Ambiguous,
        MonetaryCredit,
        MonetaryDebit,
        CreditPrenotification,
        DebitPrenotification,
        Return,
        ReturnOfReturn
    }
}
