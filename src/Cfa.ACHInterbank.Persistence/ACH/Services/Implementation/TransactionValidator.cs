using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionValidator : ITransactionValidator
{
    private static readonly IReadOnlyDictionary<(TransactionTypeEnum Type, AccountTypeEnum Account, bool IsPrenotification), string> TransactionCodeMap
        = new Dictionary<(TransactionTypeEnum, AccountTypeEnum, bool), string>
        {
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false), "22" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true), "23" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, false), "27" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, true), "28" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, false), "32" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, true), "33" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, false), "37" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, true), "38" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, false), "52" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, true), "53" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, false), "55" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, true), "57" }
        };

    public void ValidateRequest(AchTransactionRequestData request)
    {
        if (request.IsPrenotification)
        {
            if (request.Amount != 0)
            {
                throw new ArgumentException("Las prenotificaciones deben tener monto cero.", nameof(request.Amount));
            }
        }
        else if (request.Amount <= 0)
        {
            throw new ArgumentException("El monto debe ser mayor a cero.", nameof(request.Amount));
        }

        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            throw new ArgumentException("La referencia es obligatoria.", nameof(request.Reference));
        }

        if (request.Type == TransactionTypeEnum.Debit && string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            throw new ArgumentException("La identificación del receptor es obligatoria para débitos.", nameof(request.RecipientIdNumber));
        }

        if (request.Type == TransactionTypeEnum.Credit && request.RequiresIdentityValidation && string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            throw new ArgumentException("La identificación del receptor es obligatoria cuando se solicita validación.", nameof(request.RecipientIdNumber));
        }
    }

    public string ResolveTransactionCode(TransactionTypeEnum type, AccountTypeEnum accountType, bool isPrenotification)
    {
        if (TransactionCodeMap.TryGetValue((type, accountType, isPrenotification), out var code))
        {
            return code;
        }

        throw new ArgumentOutOfRangeException(nameof(accountType), "Tipo de cuenta no soportado.");
    }

    public string ValidateAddendaType(string addendaType)
    {
        var normalized = addendaType.Trim();
        if (normalized is "05" or "99")
        {
            return normalized;
        }

        throw new ArgumentException("D12: El código de tipo de registro adenda es incorrecto.", nameof(addendaType));
    }
}
