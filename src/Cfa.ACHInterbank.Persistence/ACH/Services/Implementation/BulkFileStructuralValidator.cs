using System.Globalization;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class BulkFileStructuralValidator : IBulkFileStructuralValidator
{
    private static readonly string[] RequiredColumns =
    [
        "amount", "reference", "type", "accountType", "destinationInstitutionId",
        "sourceAccountNumber", "destinationAccountNumber", "companyName",
        "companyIdentification", "companyEntryDescriptionId"
    ];

    public StructuralValidationOutcome Validate(ParsedRawItem item)
    {
        var missing = RequiredColumns.Where(c => !item.Fields.ContainsKey(c) || string.IsNullOrWhiteSpace(item.Fields[c])).ToList();
        if (missing.Count > 0)
        {
            return Invalid(item, $"Columnas requeridas faltantes: {string.Join(", ", missing)}");
        }

        if (!TryDecimal(item.Fields["amount"], out var amount))
        {
            return Invalid(item, "amount inválido.");
        }

        if (!TryEnum(item.Fields["type"], out TransactionTypeEnum type))
        {
            return Invalid(item, "type inválido.");
        }

        if (!TryEnum(item.Fields["accountType"], out AccountTypeEnum accountType))
        {
            return Invalid(item, "accountType inválido.");
        }

        if (!TryInt(item.Fields["destinationInstitutionId"], out var destinationInstitutionId))
        {
            return Invalid(item, "destinationInstitutionId inválido.");
        }

        if (!TryInt(item.Fields["companyEntryDescriptionId"], out var companyEntryDescriptionId))
        {
            return Invalid(item, "companyEntryDescriptionId inválido.");
        }

        var request = new BulkAchTransactionItemRequest
        {
            Amount = amount,
            Reference = item.Fields["reference"]!.Trim(),
            Type = type,
            AccountType = accountType,
            IsPrenotification = TryBool(item.Fields.GetValueOrDefault("isPrenotification")),
            DestinationInstitutionId = destinationInstitutionId,
            SourceAccountNumber = item.Fields["sourceAccountNumber"]!.Trim(),
            DestinationAccountNumber = item.Fields["destinationAccountNumber"]!.Trim(),
            RecipientIdNumber = item.Fields.GetValueOrDefault("recipientIdNumber")?.Trim(),
            RecipientName = item.Fields.GetValueOrDefault("recipientName")?.Trim(),
            RequiresIdentityValidation = TryBool(item.Fields.GetValueOrDefault("requiresIdentityValidation")),
            CompanyName = item.Fields["companyName"]!.Trim(),
            CompanyIdentification = item.Fields["companyIdentification"]!.Trim(),
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourcePersonType = item.Fields.GetValueOrDefault("sourcePersonType")?.Trim(),
            RecipientPersonType = item.Fields.GetValueOrDefault("recipientPersonType")?.Trim()
        };

        return new StructuralValidationOutcome
        {
            Index = item.Index,
            IsValid = true,
            Fields = item.Fields,
            NormalizedItem = request
        };
    }

    private static StructuralValidationOutcome Invalid(ParsedRawItem item, string message)
    {
        return new StructuralValidationOutcome
        {
            Index = item.Index,
            IsValid = false,
            ErrorMessage = message,
            Fields = item.Fields
        };
    }

    private static bool TryInt(string? raw, out int value)
        => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static bool TryDecimal(string? raw, out decimal value)
        => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
           || decimal.TryParse(raw, NumberStyles.Number, new CultureInfo("es-CO"), out value);

    private static bool TryEnum<TEnum>(string? raw, out TEnum value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = default;
            return false;
        }

        if (Enum.TryParse<TEnum>(raw, true, out value))
        {
            return true;
        }

        if (int.TryParse(raw, out var numeric) && Enum.IsDefined(typeof(TEnum), numeric))
        {
            value = (TEnum)Enum.ToObject(typeof(TEnum), numeric);
            return true;
        }

        return false;
    }

    private static bool TryBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        return raw.Trim() is "1" or "SI" or "Sí" or "Y";
    }
}
