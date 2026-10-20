using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class NachaProfileDimensionResolver
{
    public static string ResolveFlowCode(IReadOnlyList<AchTransaction> transactions)
    {
        if (transactions.Count > 0
            && transactions.All(x => x.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal))
        {
            return "RETORNO";
        }

        if (transactions.Count > 0
            && transactions.All(x => x.Type == TransactionTypeEnum.Prenotification))
        {
            return "PRENOTIFICACION";
        }

        return "ORIGINAL";
    }

    public static string ResolveDirectionCode(IReadOnlyList<AchTransaction> transactions)
        => transactions.Count > 0
           && transactions.All(x => x.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            ? "ENTRADA"
            : "SALIDA";
}
