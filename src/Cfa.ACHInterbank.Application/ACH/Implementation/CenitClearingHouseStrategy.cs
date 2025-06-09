using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using System.Text;

namespace Cfa.ACHInterbank.Application.ACH.Implementation;

public class CenitClearingHouseStrategy : IClearingHouseStrategy
{
    public bool ValidateTransaction(AchTransaction transaction)
    {
        return (transaction.Type == "Credit" || transaction.Type == "Debit") && transaction.Amount >= 1000;
    }

    public byte[] GenerateCycleFile(AchCycle cycle)
    {
        var xml = $@"
<AchCycle>
    <Name>{cycle.CycleName}</Name>
    <Date>{cycle.ProcessingDate:yyyy-MM-dd}</Date>
    <Cutoff>{cycle.CutoffTime}</Cutoff>
    <ClearingHouseId>{cycle.ClearingHouseId}</ClearingHouseId>
</AchCycle>";

        return Encoding.UTF8.GetBytes(xml.Trim());
    }
}
