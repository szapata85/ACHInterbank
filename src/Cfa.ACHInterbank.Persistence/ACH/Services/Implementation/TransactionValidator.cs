using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionValidator : ITransactionValidator
{
    private readonly AchDbContext _context;
    private static readonly IReadOnlyDictionary<(TransactionTypeEnum Type, AccountTypeEnum Account, bool IsPrenotification, bool IsReturn), string> FallbackTransactionCodeMap
        = new Dictionary<(TransactionTypeEnum, AccountTypeEnum, bool, bool), string>
        {
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false, false), "22" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true, false), "23" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false, true), "21" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, false, false), "27" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, true, false), "28" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Checking, false, true), "26" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, false, false), "32" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, true, false), "33" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.Savings, false, true), "31" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, false, false), "37" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, true, false), "38" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.Savings, false, true), "36" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, false, false), "52" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, true, false), "53" },
            { (TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, false, true), "51" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, false, false), "55" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, true, false), "57" },
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, false, true), "56" }        };

    public TransactionValidator(AchDbContext context)
    {
        _context = context;
    }

    public void ValidateRequest(AchTransactionRequestData request)
    {
        if (request.IsPrenotification && request.IsReturn)
        {
            throw new ArgumentException("Una transacción no puede ser prenotificación y devolución al mismo tiempo.");
        }

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
        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new ArgumentException("El nombre del usuario originador es obligatorio.", nameof(request.CompanyName));
        }

        if (request.CompanyName.Trim().Length > 16)
        {
            throw new ArgumentException("El nombre del usuario originador no puede superar 16 caracteres.", nameof(request.CompanyName));
        }

        if (string.IsNullOrWhiteSpace(request.CompanyIdentification))
        {
            throw new ArgumentException("La identificación del usuario originador es obligatoria.", nameof(request.CompanyIdentification));
        }

        var originatorId = request.CompanyIdentification.Trim();
        if (originatorId.Length is < 4 or > 10)
        {
            throw new ArgumentException("La identificación del usuario originador debe tener entre 4 y 10 caracteres.", nameof(request.CompanyIdentification));
        }

        if (string.IsNullOrWhiteSpace(request.CompanyEntryDescription))
        {
            throw new ArgumentException("La descripción de la entrada es obligatoria.", nameof(request.CompanyEntryDescription));
        }

        if (request.CompanyEntryDescription.Trim().Length > 10)
        {
            throw new ArgumentException("La descripción de la entrada no puede superar 10 caracteres.", nameof(request.CompanyEntryDescription));
        }


        if (request.Type == TransactionTypeEnum.Credit && request.RequiresIdentityValidation && string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            throw new ArgumentException("La identificación del receptor es obligatoria cuando se solicita validación.", nameof(request.RecipientIdNumber));
        }
    }

    public string ResolveTransactionCode(TransactionTypeEnum type, AccountTypeEnum accountType, bool isPrenotification, bool isReturn)
    {
        if (isPrenotification && isReturn)
        {
            throw new ArgumentException("Una transacción no puede ser prenotificación y devolución al mismo tiempo.");
        }

        if (!FallbackTransactionCodeMap.TryGetValue((type, accountType, isPrenotification, isReturn), out var fallbackCode))
        {
            throw new ArgumentOutOfRangeException(nameof(accountType), "Tipo de cuenta no soportado.");
        }

        var normalizedCode = fallbackCode.Trim();
        var configuredCodes = _context.TransactionCodes
            .AsNoTracking()
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (configuredCodes.Count == 0)
        {
            return normalizedCode;
        }

        return configuredCodes.Contains(normalizedCode)
            ? normalizedCode
            : fallbackCode;
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
