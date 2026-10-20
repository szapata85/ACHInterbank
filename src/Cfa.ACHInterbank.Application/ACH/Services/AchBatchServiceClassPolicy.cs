using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public static class AchBatchServiceClassPolicy
{
    public static string Resolve(IEnumerable<TransactionTypeEnum> transactionTypes)
    {
        var types = transactionTypes.ToArray();
        if (types.Length == 0)
        {
            throw new InvalidOperationException("ACH_BATCH_SERVICE_CLASS_EMPTY: no hay transacciones para resolver la clase de servicio del lote.");
        }

        var allCredits = types.All(type => type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification);
        var allDebits = types.All(type => type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal);
        return allCredits ? "220" : allDebits ? "225" : "200";
    }

    public static IReadOnlyList<string> ResolveReachableOrdinary(
        TransactionTypeEnum requestedType,
        bool isPrenotification)
    {
        var persistedType = isPrenotification ? TransactionTypeEnum.Prenotification : requestedType;
        var pureBatchService = Resolve([persistedType]);
        return pureBatchService == "200" ? ["200"] : ["200", pureBatchService];
    }
}
