using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaType7LegacyRenderer : INachaType7LegacyRenderer
{
    public string Render(AchBatch batch, AchTransaction transaction, AchTransactionAddenda addenda)
    {
        ValidateAddendaCompatibility(transaction, addenda);
        return addenda.BusinessType switch
        {
            AchAddendaBusinessType.Debit => BuildDebitType7Record(transaction, addenda),
            AchAddendaBusinessType.Return => BuildReturnType7Record(transaction, addenda),
            _ => BuildCreditType7Record(batch, transaction, addenda)
        };
    }

    private static void ValidateAddendaCompatibility(AchTransaction transaction, AchTransactionAddenda addenda)
    {
        var txType = transaction.Type;
        var addendaType = (addenda.AddendaType ?? string.Empty).Trim();

        switch (addenda.BusinessType)
        {
            case AchAddendaBusinessType.Credit:
                if (addendaType != "05")
                {
                    throw new InvalidOperationException($"La transacción {transaction.Id} con addenda de crédito debe utilizar AddendaType=05.");
                }

                if (txType is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
                {
                    throw new InvalidOperationException($"La transacción {transaction.Id} ({txType}) no puede serializar addenda de crédito.");
                }
                break;

            case AchAddendaBusinessType.Debit:
                if (addendaType != "05")
                {
                    throw new InvalidOperationException($"La transacción {transaction.Id} con addenda de débito debe utilizar AddendaType=05.");
                }

                if (txType is TransactionTypeEnum.Credit)
                {
                    throw new InvalidOperationException($"La transacción {transaction.Id} ({txType}) no puede serializar addenda de débito.");
                }
                break;

            case AchAddendaBusinessType.Return:
                if (addendaType != "99")
                {
                    throw new InvalidOperationException($"La transacción {transaction.Id} con addenda de devolución debe utilizar AddendaType=99.");
                }

                if (txType is not (TransactionTypeEnum.Return or TransactionTypeEnum.Reversal))
                {
                    throw new InvalidOperationException($"La transacción {transaction.Id} ({txType}) no puede serializar addenda de devolución.");
                }
                break;
        }
    }

    private static string BuildCreditType7Record(AchBatch batch, AchTransaction transaction, AchTransactionAddenda addenda)
    {
        var purpose = FormatAlpha(addenda.Purpose ?? batch.CompanyEntryDescription, 10);
        var batchDescription = FormatAlpha(batch.CompanyEntryDescription, 10);
        if (!string.Equals(purpose, batchDescription, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"La addenda de crédito de la transacción {transaction.Id} debe reflejar la descripción del lote tipo 5.");
        }

        var reference = string.IsNullOrWhiteSpace(addenda.Reference)
            ? new string('0', 53)
            : FormatAlpha(addenda.Reference, 53);

        var buffer = CreateBlankRecord('7');
        WriteValue(buffer, 2, "05");
        WriteValue(buffer, 21, purpose);
        WriteValue(buffer, 31, reference);
        WriteValue(buffer, 84, FormatNumeric((addenda.SequenceNumber ?? 1).ToString(), 4));
        WriteValue(buffer, 88, FormatNumeric(GetTraceSuffix(transaction.TraceNumber), 7));
        return new string(buffer);
    }

    private static string BuildDebitType7Record(AchTransaction transaction, AchTransactionAddenda addenda)
    {
        var collectorId = FormatNumeric(addenda.CollectorId, 13);
        var receiverCustomerCode = FormatAlpha(addenda.ReceiverCustomerCode, 30);
        var serviceDescription = FormatAlpha(addenda.ServiceDescription, 15);

        if (string.IsNullOrWhiteSpace(collectorId.Trim('0')))
        {
            throw new InvalidOperationException($"La transacción débito {transaction.Id} requiere CollectorId en la addenda tipo 7.");
        }

        if (string.IsNullOrWhiteSpace(receiverCustomerCode.Trim()))
        {
            throw new InvalidOperationException($"La transacción débito {transaction.Id} requiere ReceiverCustomerCode en la addenda tipo 7.");
        }

        if (string.IsNullOrWhiteSpace(serviceDescription.Trim()))
        {
            throw new InvalidOperationException($"La transacción débito {transaction.Id} requiere ServiceDescription en la addenda tipo 7.");
        }

        var buffer = CreateBlankRecord('7');
        WriteValue(buffer, 2, "05");
        WriteValue(buffer, 4, collectorId);
        WriteValue(buffer, 17, receiverCustomerCode);
        WriteValue(buffer, 47, serviceDescription);
        WriteValue(buffer, 84, FormatNumeric((addenda.SequenceNumber ?? 1).ToString(), 4));
        WriteValue(buffer, 88, FormatNumeric(GetTraceSuffix(transaction.TraceNumber), 7));
        return new string(buffer);
    }

    private static string BuildReturnType7Record(AchTransaction transaction, AchTransactionAddenda addenda)
    {
        var returnReasonCode = FormatAlpha(addenda.ReturnReasonCode, 5);
        var originalTraceNumber = FormatNumeric(addenda.OriginalTraceNumber, 15);
        var newTraceNumber = FormatNumeric(addenda.NewTraceNumber, 15);

        var buffer = CreateBlankRecord('7');
        WriteValue(buffer, 2, "99");
        WriteValue(buffer, 4, returnReasonCode);
        WriteValue(buffer, 9, originalTraceNumber);
        WriteValue(buffer, 82, newTraceNumber);
        WriteValue(buffer, 100, FormatNumeric(GetTraceSuffix(transaction.TraceNumber), 7));
        return new string(buffer);
    }

    private static char[] CreateBlankRecord(char recordType)
    {
        var buffer = new char[106];
        Array.Fill(buffer, ' ');
        buffer[0] = recordType;
        return buffer;
    }

    private static void WriteValue(char[] buffer, int startPosition, string value)
    {
        var start = startPosition - 1;
        value.CopyTo(0, buffer, start, value.Length);
    }

    private static string FormatAlpha(string? value, int length)
    {
        var normalized = new string((value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch is '.' or ',' or '-' or '/' or '&')
            .ToArray());
        if (normalized.Length > length)
        {
            normalized = normalized[..length];
        }

        return normalized.PadRight(length, ' ');
    }

    private static string FormatNumeric(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }

    private static string GetTraceSuffix(string? traceNumber)
    {
        var digits = new string((traceNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 7 ? digits : digits[^7..];
    }
}
