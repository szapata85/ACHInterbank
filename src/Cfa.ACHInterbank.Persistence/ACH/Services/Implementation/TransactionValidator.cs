using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionValidator : ITransactionValidator
{
    private readonly AchDbContext _context;
    private static readonly Regex ReturnReasonRegex = new(@"^(R\d{2}|DEV14)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ExternalIdRegex = new(@"^[A-Za-z0-9\-_/.]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ReferenceRegex = new(@"^[A-Za-z0-9\-_/]{1,30}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly IReadOnlyDictionary<(TransactionTypeEnum Type, AccountTypeEnum Account, bool IsPrenotification), string> FallbackTransactionCodeMap
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
            { (TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, true), "57" },
            { (TransactionTypeEnum.Prenotification, AccountTypeEnum.Checking, true), "23" },
            { (TransactionTypeEnum.Prenotification, AccountTypeEnum.Savings, true), "33" },
            { (TransactionTypeEnum.Prenotification, AccountTypeEnum.ElectronicDeposits, true), "53" },
            { (TransactionTypeEnum.Return, AccountTypeEnum.Checking, false), "27" },
            { (TransactionTypeEnum.Return, AccountTypeEnum.Savings, false), "37" },
            { (TransactionTypeEnum.Return, AccountTypeEnum.ElectronicDeposits, false), "55" },
            { (TransactionTypeEnum.Reversal, AccountTypeEnum.Checking, false), "27" },
            { (TransactionTypeEnum.Reversal, AccountTypeEnum.Savings, false), "37" },
            { (TransactionTypeEnum.Reversal, AccountTypeEnum.ElectronicDeposits, false), "55" }
        };

    public TransactionValidator(AchDbContext context)
    {
        _context = context;
    }

    private HashSet<string>? _configuredCodesCache;

    public void ValidateRequest(AchTransactionRequestData request, IReadOnlySet<int>? validCompanyEntryDescriptionIds = null)
    {
        var effectiveType = ResolveEffectiveType(request.Type, request.IsPrenotification);

        if (effectiveType == TransactionTypeEnum.Prenotification)
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

        var normalizedExternalId = request.TransactionExternalId?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedExternalId) && !ExternalIdRegex.IsMatch(normalizedExternalId))
        {
            throw new ArgumentException("transactionExternalId solo puede contener caracteres alfanuméricos y -_/.", nameof(request.TransactionExternalId));
        }

        if (!string.IsNullOrWhiteSpace(request.Reference) && !ReferenceRegex.IsMatch(request.Reference.Trim()))
        {
            throw new ArgumentException("La referencia solo puede contener caracteres alfanuméricos y -_/ .", nameof(request.Reference));
        }

        if (string.IsNullOrWhiteSpace(normalizedExternalId) && string.IsNullOrWhiteSpace(request.Reference))
        {
            throw new ArgumentException("Debe enviar transactionExternalId o reference (legado).", nameof(request.TransactionExternalId));
        }

        ValidateAccountNumber(request.SourceAccountNumber, nameof(request.SourceAccountNumber));
        ValidateAccountNumber(request.DestinationAccountNumber, nameof(request.DestinationAccountNumber));

        if (string.Equals(request.SourceAccountNumber?.Trim(), request.DestinationAccountNumber?.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("La cuenta origen y destino no pueden ser iguales.", nameof(request.DestinationAccountNumber));
        }

        ValidateParticipantIdentity(request.SourcePersonType, request.CompanyIdentification, nameof(request.CompanyIdentification));

        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            ValidateParticipantIdentity(request.RecipientPersonType, request.RecipientIdNumber, nameof(request.RecipientIdNumber));
        }

        if (effectiveType is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal && string.IsNullOrWhiteSpace(request.RecipientIdNumber))
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

        if (request.CompanyEntryDescriptionId <= 0)
        {
            throw new ArgumentException("El concepto de lote es obligatorio.", nameof(request.CompanyEntryDescriptionId));
        }

        var existsInCatalog = validCompanyEntryDescriptionIds is not null
            ? validCompanyEntryDescriptionIds.Contains(request.CompanyEntryDescriptionId)
            : _context.CompanyEntryDescriptionCatalogs
                .AsNoTracking()
                .Any(item => item.Id == request.CompanyEntryDescriptionId && item.IsActive);

        if (!existsInCatalog)
        {
            throw new ArgumentException("El concepto de lote seleccionado no existe en el catálogo permitido.", nameof(request.CompanyEntryDescriptionId));
        }

        if (effectiveType == TransactionTypeEnum.Credit && request.RequiresIdentityValidation && string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            throw new ArgumentException("La identificación del receptor es obligatoria cuando se solicita validación.", nameof(request.RecipientIdNumber));
        }

        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber) && string.IsNullOrWhiteSpace(request.RecipientName))
        {
            throw new ArgumentException("El nombre del receptor es obligatorio cuando se diligencia identificación de receptor.", nameof(request.RecipientName));
        }
    }

    public string ResolveTransactionCode(TransactionTypeEnum type, AccountTypeEnum accountType, bool isPrenotification)
    {
        if (!FallbackTransactionCodeMap.TryGetValue((type, accountType, isPrenotification), out var fallbackCode))
        {
            throw new ArgumentOutOfRangeException(nameof(accountType), "Tipo de cuenta no soportado.");
        }

        var normalizedCode = fallbackCode.Trim();
        _configuredCodesCache ??= _context.TransactionCodes
            .AsNoTracking()
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_configuredCodesCache.Count == 0)
        {
            return normalizedCode;
        }

        return _configuredCodesCache.Contains(normalizedCode)
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

    public AddendaDto NormalizeAndValidateAddenda(AddendaDto addenda, TransactionTypeEnum transactionType, bool isPrenotification, string batchDescription)
    {
        ArgumentNullException.ThrowIfNull(addenda);

        var normalizedType = ValidateAddendaType(addenda.AddendaType);
        var businessType = addenda.BusinessType ?? ResolveBusinessType(transactionType, normalizedType);
        var normalizedBatchDescription = NormalizeAlpha(batchDescription, 10);

        var normalized = new AddendaDto
        {
            AddendaType = normalizedType,
            BusinessType = businessType,
            Information = NormalizeFreeText(addenda.Information, 80),
            Purpose = NormalizeAlpha(addenda.Purpose, 10),
            Reference = NormalizeReference(addenda.Reference),
            CollectorId = NormalizeDigits(addenda.CollectorId, 13),
            ReceiverCustomerCode = NormalizeAlpha(addenda.ReceiverCustomerCode, 30),
            ServiceDescription = NormalizeAlpha(addenda.ServiceDescription, 15),
            ReturnReasonCode = NormalizeReturnReason(addenda.ReturnReasonCode),
            OriginalTraceNumber = NormalizeTrace(addenda.OriginalTraceNumber),
            NewTraceNumber = NormalizeTrace(addenda.NewTraceNumber)
        };

        switch (businessType)
        {
            case AchAddendaBusinessType.Credit:
                if (normalizedType != "05")
                {
                    throw new ArgumentException("Las addendas de crédito/prenotificación deben utilizar código 05.", nameof(addenda.AddendaType));
                }

                normalized.Purpose = string.IsNullOrWhiteSpace(normalized.Purpose)
                    ? normalizedBatchDescription
                    : normalized.Purpose;

                if (!string.Equals(normalized.Purpose, normalizedBatchDescription, StringComparison.Ordinal))
                {
                    throw new ArgumentException("La addenda de crédito debe reflejar exactamente la descripción de lote del registro tipo 5.", nameof(addenda.Purpose));
                }

                normalized.Reference = NormalizeReference(normalized.Reference ?? normalized.Information ?? string.Empty);
                if (string.IsNullOrWhiteSpace(normalized.Reference))
                {
                    normalized.Reference = new string('0', 53);
                }
                break;

            case AchAddendaBusinessType.Debit:
                if (normalizedType != "05")
                {
                    throw new ArgumentException("Las addendas de débito/prenotificación deben utilizar código 05.", nameof(addenda.AddendaType));
                }

                if (string.IsNullOrWhiteSpace(normalized.CollectorId))
                {
                    throw new ArgumentException("CollectorId (NIT/EAN-13) es obligatorio para addendas de débito.", nameof(addenda.CollectorId));
                }

                if (string.IsNullOrWhiteSpace(normalized.ReceiverCustomerCode))
                {
                    throw new ArgumentException("ReceiverCustomerCode es obligatorio para addendas de débito.", nameof(addenda.ReceiverCustomerCode));
                }

                if (string.IsNullOrWhiteSpace(normalized.ServiceDescription))
                {
                    throw new ArgumentException("ServiceDescription es obligatorio para addendas de débito.", nameof(addenda.ServiceDescription));
                }
                break;

            case AchAddendaBusinessType.Return:
                if (normalizedType != "99")
                {
                    throw new ArgumentException("Las addendas de devolución deben utilizar código 99.", nameof(addenda.AddendaType));
                }

                if (string.IsNullOrWhiteSpace(normalized.ReturnReasonCode))
                {
                    throw new ArgumentException("ReturnReasonCode es obligatorio para addendas de devolución.", nameof(addenda.ReturnReasonCode));
                }

                if (string.IsNullOrWhiteSpace(normalized.OriginalTraceNumber))
                {
                    throw new ArgumentException("OriginalTraceNumber es obligatorio y debe tener 15 dígitos.", nameof(addenda.OriginalTraceNumber));
                }

                if (string.IsNullOrWhiteSpace(normalized.NewTraceNumber))
                {
                    throw new ArgumentException("NewTraceNumber es obligatorio y debe tener 15 dígitos.", nameof(addenda.NewTraceNumber));
                }
                break;
        }

        if (isPrenotification && businessType == AchAddendaBusinessType.Return)
        {
            throw new ArgumentException("Las prenotificaciones no deben registrar addendas de devolución.", nameof(addenda.BusinessType));
        }

        return normalized;
    }

    private static void ValidateAccountNumber(string? accountNumber, string paramName)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || !Regex.IsMatch(accountNumber.Trim(), @"^\d{6,18}$"))
        {
            throw new ArgumentException("La cuenta debe tener entre 6 y 18 dígitos numéricos.", paramName);
        }
    }

    private static void ValidateParticipantIdentity(string? personType, string? idNumber, string paramName)
    {
        var normalizedPersonType = (personType ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedId = (idNumber ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedId))
        {
            throw new ArgumentException("La identificación es obligatoria.", paramName);
        }

        var isNumeric = Regex.IsMatch(normalizedId, @"^\d{5,20}$");
        var isTaxId = Regex.IsMatch(normalizedId, @"^[A-Z0-9]{4,20}$");

        if (normalizedPersonType == "PN" && !isNumeric)
        {
            throw new ArgumentException("Las personas naturales deben identificarse con un número de 5 a 20 dígitos.", paramName);
        }

        if (normalizedPersonType == "PJ" && !isTaxId)
        {
            throw new ArgumentException("Las personas jurídicas deben identificarse con un NIT/identificador alfanumérico válido.", paramName);
        }
    }

    private static TransactionTypeEnum ResolveEffectiveType(TransactionTypeEnum requestedType, bool isPrenotification)
    {
        if (requestedType == TransactionTypeEnum.Prenotification || isPrenotification)
        {
            return TransactionTypeEnum.Prenotification;
        }

        return requestedType;
    }

    private static AchAddendaBusinessType ResolveBusinessType(TransactionTypeEnum transactionType, string addendaType)
    {
        if (addendaType == "99")
        {
            return AchAddendaBusinessType.Return;
        }

        return transactionType is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
            ? AchAddendaBusinessType.Debit
            : AchAddendaBusinessType.Credit;
    }

    private static string? NormalizeReturnReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!ReturnReasonRegex.IsMatch(normalized))
        {
            throw new ArgumentException("El código de retorno debe cumplir el formato ^R\\d{2}$ o DEV14.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeTrace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 15)
        {
            throw new ArgumentException("Los números de secuencia (trace) deben ser numéricos de 15 dígitos.", nameof(value));
        }

        return digits;
    }

    private static string? NormalizeDigits(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            throw new ArgumentException("El campo debe contener únicamente caracteres numéricos.", nameof(value));
        }

        return digits.Length <= maxLength ? digits : digits[..maxLength];
    }

    private static string? NormalizeReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeAlpha(value, 53);
    }

    private static string? NormalizeAlpha(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(maxLength);
        foreach (var character in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character) || character == ' ' || character is '.' or ',' or '-' or '/' or '&')
            {
                builder.Append(character);
            }
        }

        var compact = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= maxLength ? compact : compact[..maxLength];
    }

    private static string? NormalizeFreeText(string? value, int maxLength)
    {
        return NormalizeAlpha(value, maxLength);
    }
}
