namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class AchReturnAlreadyGeneratedException : InvalidOperationException
{
    public const string ErrorCode = "ACH_RETURN_ALREADY_GENERATED";

    public AchReturnAlreadyGeneratedException(IEnumerable<int> transactionIds)
        : base(BuildMessage(transactionIds))
    {
        TransactionIds = transactionIds.Distinct().OrderBy(id => id).ToArray();
    }

    public IReadOnlyList<int> TransactionIds { get; }

    private static string BuildMessage(IEnumerable<int> transactionIds)
    {
        var ids = transactionIds.Distinct().OrderBy(id => id).ToArray();
        return ids.Length == 1
            ? $"La transacción {ids[0]} ya cuenta con una devolución registrada."
            : $"Una o más transacciones ya cuentan con devolución registrada: {string.Join(", ", ids)}.";
    }
}
