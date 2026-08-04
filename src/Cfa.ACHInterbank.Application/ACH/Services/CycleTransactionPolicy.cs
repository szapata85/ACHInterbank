using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public sealed class CycleTransactionPolicy(
    IClearingHouseToPaymentRailMapper railMapper,
    ICycleNumberResolver cycleNumberResolver) : ICycleTransactionPolicy
{
    public const string OrdinaryDebitNotAllowedReasonCode = "ACHCOL_CYCLE5_ORDINARY_DEBIT_NOT_ALLOWED";

    public CycleTransactionPolicyResult Evaluate(CycleTransactionPolicyRequest request)
    {
        var rail = railMapper.ResolveRail(new PaymentRailResolveRequest(
            null,
            request.ClearingHouseCode,
            request.PaymentRailCode));
        var cycleNumber = cycleNumberResolver.Resolve(request.CycleName);

        var isReturn = request.TransactionType == TransactionTypeEnum.Return
            || !string.IsNullOrWhiteSpace(request.ReturnReasonCode)
            || !string.IsNullOrWhiteSpace(request.OriginalTraceRef);
        var isPrenotification = request.IsPrenotification
            || request.TransactionType == TransactionTypeEnum.Prenotification;

        var functionalClass = isReturn
            ? isPrenotification ? "PrenotificationReturn" : "Return"
            : isPrenotification ? "Prenotification"
            : request.TransactionType == TransactionTypeEnum.Debit ? "OrdinaryMonetaryDebit"
            : request.TransactionType == TransactionTypeEnum.Credit ? "MonetaryCredit"
            : request.TransactionType.ToString();

        var rejected = rail.IsKnownRail
            && string.Equals(rail.RailCode, PaymentRailCodes.AchColombia, StringComparison.OrdinalIgnoreCase)
            && cycleNumber == 5
            && request.TransactionType == TransactionTypeEnum.Debit
            && !isPrenotification
            && !isReturn;

        return rejected
            ? new CycleTransactionPolicyResult(
                false,
                OrdinaryDebitNotAllowedReasonCode,
                "ACH Colombia no admite débitos monetarios ordinarios originados en el Ciclo 5.",
                rail.RailCode,
                cycleNumber,
                functionalClass)
            : new CycleTransactionPolicyResult(
                true,
                "ALLOWED",
                "La regla regulatoria de débito ordinario de ACH Colombia Ciclo 5 no bloquea la operación.",
                rail.RailCode,
                cycleNumber,
                functionalClass);
    }
}

