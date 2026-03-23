using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using System.Text;

namespace Cfa.ACHInterbank.Application.ACH.Implementation;

public class CenitClearingHouseStrategy : IClearingHouseStrategy
{
    public string Name => "CENIT";
    public string OutputFormat => "CENIT";
    public IReadOnlyCollection<TransactionTypeEnum> SupportedTransactionTypes =>
        [TransactionTypeEnum.Credit, TransactionTypeEnum.Debit, TransactionTypeEnum.Prenotification, TransactionTypeEnum.Return];

    public bool ValidateTransaction(AchTransaction transaction)
    {
        if (!SupportedTransactionTypes.Contains(transaction.Type))
        {
            return false;
        }

        return transaction.Type switch
        {
            TransactionTypeEnum.Prenotification => transaction.Amount == 0,
            TransactionTypeEnum.Return => !string.IsNullOrWhiteSpace(transaction.ReturnReasonCode) && !string.IsNullOrWhiteSpace(transaction.OriginalTraceRef),
            TransactionTypeEnum.Debit => transaction.Amount >= 1000,
            _ => transaction.Amount > 0
        };
    }

    public string BuildFileName(AchCycle cycle, DateTime generatedAtUtc)
    {
        var originCode = string.IsNullOrWhiteSpace(cycle.ClearingHouse?.OriginCode)
            ? "00000000"
            : cycle.ClearingHouse.OriginCode.Trim();
        var cycleNumber = ExtractCycleNumber(cycle.CycleName);
        return $"{originCode}.{cycleNumber}.1";
    }

    public byte[] GenerateCycleFile(AchCycle cycle)
    {
        var xml = $@"
<CenitCycle format=\"{OutputFormat}\">
    <Name>{cycle.CycleName}</Name>
    <Date>{cycle.ProcessingDate:yyyy-MM-dd}</Date>
    <Cutoff>{cycle.CutoffTime}</Cutoff>
    <ClearingHouseId>{cycle.ClearingHouseId}</ClearingHouseId>
</CenitCycle>";

        return Encoding.UTF8.GetBytes(xml.Trim());
    }

    private static int ExtractCycleNumber(string? cycleName)
    {
        var digits = new string((cycleName ?? string.Empty).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var cycleNumber) ? cycleNumber : 0;
    }
}
