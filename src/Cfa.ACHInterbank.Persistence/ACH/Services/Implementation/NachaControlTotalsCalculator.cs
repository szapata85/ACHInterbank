using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaControlTotalsCalculator : INachaControlTotalsCalculator
{
    public NachaControlTotalsResult Calculate(NachaControlTotalsRequest request)
    {
        if (request.BlockSize <= 0)
        {
            throw new NachaGenerationException("NACHA_BLOCK_SIZE_INVALID", $"El blockSize NACHA-M debe ser mayor a cero. Valor recibido: {request.BlockSize}.");
        }

        ValidateLength("Batch EntryHash", request.BatchEntryHashLength);
        ValidateLength("File EntryHash", request.FileEntryHashLength);
        ValidateLength("Batch EntryAddendaCount", request.BatchEntryAddendaCountLength);
        ValidateLength("File EntryAddendaCount", request.FileEntryAddendaCountLength);
        ValidateLength("Batch TotalDebitAmount", request.BatchTotalDebitAmountLength);
        ValidateLength("File TotalDebitAmount", request.FileTotalDebitAmountLength);
        ValidateLength("Batch TotalCreditAmount", request.BatchTotalCreditAmountLength);
        ValidateLength("File TotalCreditAmount", request.FileTotalCreditAmountLength);
        ValidateLength("BatchCount", request.BatchCountLength);
        ValidateLength("BlockCount", request.BlockCountLength);

        if (string.IsNullOrWhiteSpace(request.EntryHashSourceFieldPath))
        {
            throw new NachaGenerationException("NACHA_ENTRY_HASH_SOURCE_MISSING", "Falta el campo fuente configurado para calcular EntryHash.");
        }

        var batchTotals = new List<NachaBatchControlTotals>(request.Batches.Count);
        foreach (var batch in request.Batches)
        {
            var transactions = request.TransactionsByBatchId.TryGetValue(batch.Id, out var txs)
                ? txs
                : Array.Empty<AchTransaction>();
            var addendaCount = request.AddendaRecordCountByBatchId.TryGetValue(batch.Id, out var configuredAddendaCount)
                ? configuredAddendaCount
                : transactions.Sum(tx => tx.Addendas?.Count ?? 0);

            long debit = 0;
            long credit = 0;
            long hash = 0;
            foreach (var transaction in transactions)
            {
                var amountInCents = ToCents(transaction.Amount);
                if (transaction.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                {
                    credit = checked(credit + amountInCents);
                }
                else if (transaction.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
                {
                    debit = checked(debit + amountInCents);
                }

                hash = checked(hash + ResolveEntryHashValue(transaction, request.EntryHashSourceFieldPath));
            }

            var normalizedBatchHash = NormalizeEntryHash(hash, request.BatchEntryHashLength);
            var entryAddendaCount = checked(transactions.Count + addendaCount);
            EnsureFits("EntryAddendaCount batch", entryAddendaCount, request.BatchEntryAddendaCountLength);
            EnsureFits("TotalDebitAmount batch", debit, request.BatchTotalDebitAmountLength);
            EnsureFits("TotalCreditAmount batch", credit, request.BatchTotalCreditAmountLength);

            batchTotals.Add(new NachaBatchControlTotals
            {
                BatchId = batch.Id,
                EntryDetailCount = transactions.Count,
                AddendaCount = addendaCount,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = normalizedBatchHash,
                TotalDebitAmountInCents = debit,
                TotalCreditAmountInCents = credit
            });
        }

        var fileEntryHash = NormalizeEntryHash(batchTotals.Sum(x => x.EntryHash), request.FileEntryHashLength);
        var fileEntryAddendaCount = batchTotals.Sum(x => x.EntryAddendaCount);
        var fileDebit = batchTotals.Sum(x => x.TotalDebitAmountInCents);
        var fileCredit = batchTotals.Sum(x => x.TotalCreditAmountInCents);
        var blockCount = (int)Math.Ceiling(request.PhysicalRecordCountBeforePadding / (decimal)request.BlockSize);
        var physicalRecordCountAfterPadding = blockCount * request.BlockSize;
        var paddingRecordCount = physicalRecordCountAfterPadding - request.PhysicalRecordCountBeforePadding;

        EnsureFits("BatchCount file", request.Batches.Count, request.BatchCountLength);
        EnsureFits("BlockCount file", blockCount, request.BlockCountLength);
        EnsureFits("EntryAddendaCount file", fileEntryAddendaCount, request.FileEntryAddendaCountLength);
        EnsureFits("TotalDebitAmount file", fileDebit, request.FileTotalDebitAmountLength);
        EnsureFits("TotalCreditAmount file", fileCredit, request.FileTotalCreditAmountLength);

        if (physicalRecordCountAfterPadding % request.BlockSize != 0)
        {
            throw new NachaGenerationException("NACHA_BLOCK_ALIGNMENT_INVALID", $"El archivo final no queda alineado a blockSize={request.BlockSize}.");
        }

        return new NachaControlTotalsResult
        {
            BatchTotals = batchTotals,
            FileTotals = new NachaFileControlTotals
            {
                BatchCount = request.Batches.Count,
                BlockCount = blockCount,
                EntryAddendaCount = fileEntryAddendaCount,
                EntryHash = fileEntryHash,
                TotalDebitAmountInCents = fileDebit,
                TotalCreditAmountInCents = fileCredit,
                PhysicalRecordCountBeforePadding = request.PhysicalRecordCountBeforePadding,
                PaddingRecordCount = paddingRecordCount,
                PhysicalRecordCountAfterPadding = physicalRecordCountAfterPadding
            }
        };
    }

    public string ResolveFileIdModifier(int dailySequence)
    {
        if (dailySequence < 1 || dailySequence > 36)
        {
            throw new InvalidOperationException($"El consecutivo diario NACHA-M debe estar entre 001 y 036. Valor recibido: {dailySequence}");
        }

        if (dailySequence <= 26)
        {
            return ((char)('A' + dailySequence - 1)).ToString();
        }

        return (dailySequence - 27).ToString(CultureInfo.InvariantCulture);
    }

    private static long ToCents(decimal amount)
        => decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static long ResolveEntryHashValue(AchTransaction transaction, string sourceFieldPath)
    {
        var property = transaction.GetType().GetProperties()
            .FirstOrDefault(x => string.Equals(x.Name, sourceFieldPath, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            throw new NachaGenerationException("NACHA_ENTRY_HASH_SOURCE_MISSING", $"No se encontró sourceFieldPath {sourceFieldPath} para calcular EntryHash.");
        }

        var text = Convert.ToString(property.GetValue(transaction), CultureInfo.InvariantCulture) ?? string.Empty;
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length == 0 || !long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            throw new NachaGenerationException("NACHA_ENTRY_HASH_NOT_NUMERIC", $"El campo {sourceFieldPath} no puede convertirse a número para calcular EntryHash. Valor recibido: '{text}'.");
        }

        return value;
    }

    private static long NormalizeEntryHash(long value, int length)
    {
        var modulus = Pow10(length);
        return value % modulus;
    }

    private static long Pow10(int length)
    {
        long value = 1;
        for (var i = 0; i < length; i++)
        {
            value = checked(value * 10);
        }

        return value;
    }

    private static void ValidateLength(string fieldName, int length)
    {
        if (length <= 0 || length > 18)
        {
            throw new NachaGenerationException("NACHA_FIELD_LENGTH_INVALID", $"La longitud configurada para {fieldName} es inválida: {length}.");
        }
    }

    private static void EnsureFits(string fieldName, long value, int length)
    {
        if (value < 0 || value.ToString(CultureInfo.InvariantCulture).Length > length)
        {
            throw new NachaGenerationException("NACHA_CONTROL_TOTAL_LENGTH_INVALID", $"El valor calculado {fieldName}={value} excede longitud configurada {length}.");
        }
    }
}
