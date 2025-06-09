using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using System.Text;

namespace Cfa.ACHInterbank.Application.ACH.Implementation;

public class AchClearingHouseStrategy : IClearingHouseStrategy
{
    public bool ValidateTransaction(AchTransaction transaction)
    {
        return transaction.Type == "Credit" && transaction.Amount > 0;
    }

    public byte[] GenerateCycleFile(AchCycle cycle)
    {
        var lines = new List<string>
        {
            "CycleName,ProcessingDate,CutoffTime,ClearingHouseId",
            $"{cycle.CycleName},{cycle.ProcessingDate:yyyy-MM-dd},{cycle.CutoffTime},{cycle.ClearingHouseId}"
        };

        var content = string.Join(Environment.NewLine, lines);
        return Encoding.UTF8.GetBytes(content);
    }
}
