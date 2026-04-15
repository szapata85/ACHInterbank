using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPriorityPolicy : ITransactionPriorityPolicy
{
    public int ResolvePriority(AchTransaction transaction)
    {
        if (transaction.IsPrenotification && transaction.Type == TransactionTypeEnum.Debit)
        {
            return 100;
        }

        return transaction.Type switch
        {
            TransactionTypeEnum.Return => 90,
            TransactionTypeEnum.Credit => 80,
            TransactionTypeEnum.Debit => 70,
            TransactionTypeEnum.Reversal => 60,
            TransactionTypeEnum.Prenotification => 50,
            _ => 10
        };
    }
}
