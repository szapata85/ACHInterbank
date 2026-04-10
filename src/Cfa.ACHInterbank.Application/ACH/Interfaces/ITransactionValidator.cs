using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionValidator
{
    void ValidateRequest(AchTransactionRequestData request, IReadOnlySet<int>? validCompanyEntryDescriptionIds = null);
    string ResolveTransactionCode(TransactionTypeEnum type, AccountTypeEnum accountType, bool isPrenotification);
    string ValidateAddendaType(string addendaType);
    AddendaDto NormalizeAndValidateAddenda(AddendaDto addenda, TransactionTypeEnum transactionType, bool isPrenotification, string batchDescription);
}
