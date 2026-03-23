using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using System.Text;

namespace Cfa.ACHInterbank.Application.ACH.Implementation;

public class AchClearingHouseStrategy : IClearingHouseStrategy
{
    public string Name => "ACH Colombia";
    public string OutputFormat => "NACHA-M";
    public IReadOnlyCollection<TransactionTypeEnum> SupportedTransactionTypes =>
        [TransactionTypeEnum.Credit, TransactionTypeEnum.Debit, TransactionTypeEnum.Prenotification, TransactionTypeEnum.Reversal, TransactionTypeEnum.Return];

    public bool ValidateTransaction(AchTransaction transaction)
    {
        if (!SupportedTransactionTypes.Contains(transaction.Type))
        {
            return false;
        }

        if (transaction.Type == TransactionTypeEnum.Prenotification)
        {
            return transaction.Amount == 0;
        }

        if (transaction.Type == TransactionTypeEnum.Reversal)
        {
            return !string.IsNullOrWhiteSpace(transaction.OriginalTraceRef);
        }

        if (transaction.Type == TransactionTypeEnum.Return)
        {
            return !string.IsNullOrWhiteSpace(transaction.ReturnReasonCode) && !string.IsNullOrWhiteSpace(transaction.OriginalTraceRef);
        }

        return transaction.Amount > 0;
    }

    public string BuildFileName(AchCycle cycle, DateTime generatedAtUtc)
    {
        var cycleNumber = ExtractCycleNumber(cycle.CycleName);
        return $"ACH_{cycle.ProcessingDate:yyyyMMdd}_{cycleNumber:00}_{generatedAtUtc:HHmmss}.txt";
    }

    public byte[] GenerateCycleFile(AchCycle cycle)
    {
        var lines = new List<string>
        {
            "CycleName,ProcessingDate,CutoffTime,ClearingHouseId,Format",
            $"{cycle.CycleName},{cycle.ProcessingDate:yyyy-MM-dd},{cycle.CutoffTime},{cycle.ClearingHouseId},{OutputFormat}"
        };

        var content = string.Join(Environment.NewLine, lines);
        return Encoding.UTF8.GetBytes(content);
    }

    private static int ExtractCycleNumber(string? cycleName)
    {
        var digits = new string((cycleName ?? string.Empty).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var cycleNumber) ? cycleNumber : 0;
    }
}
