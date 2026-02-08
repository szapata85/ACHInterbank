using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionValidator
{
    void ValidateRequest(AchTransactionRequestData request);
    string ResolveTransactionCode(TransactionTypeEnum type, AccountTypeEnum accountType, bool isPrenotification, bool isReturn);
    string ValidateAddendaType(string addendaType);
}
