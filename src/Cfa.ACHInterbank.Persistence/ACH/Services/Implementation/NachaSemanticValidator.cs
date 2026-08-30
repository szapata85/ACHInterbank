using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaSemanticValidator : INachaSemanticValidator
{
    private const int RecordLength = 106;
    private const int BatchHeaderDescriptionStart = 53;
    private const int BatchHeaderDescriptionLength = 10;
    private const int ReturnReasonStart = 3;
    private const int ReturnReasonLength = 5;
    private const int OriginalTraceStart = 8;
    private const int OriginalTraceLength = 15;
    private const int NewTraceStart = 81;
    private const int NewTraceLength = 15;
    private const int EntrySequenceStart = 99;
    private const int EntrySequenceLength = 7;
    private const int AddendaIndicatorStart = 86;
    private const int TraceSuffixStart = 95;
    private const int AddendaTraceSuffixStart = 87;
    private const int TraceSuffixLength = 7;
    private const string RequiredMassCreditDescription = "MULTICREDIT";

    public void Validate(string fileContent, NachaBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            throw new InvalidOperationException("El archivo NACHA no puede generarse vacío.");
        }

        if (fileContent.Length % RecordLength != 0)
        {
            throw new InvalidOperationException("El archivo NACHA debe estar compuesto por registros de 106 caracteres.");
        }

        var records = Enumerable.Range(0, fileContent.Length / RecordLength)
            .Select(index => fileContent.Substring(index * RecordLength, RecordLength))
            .ToList();

        if (records[0][0] != '1' || !records.Any(record => record[0] == '5')
                                  || !records.Any(record => record[0] == '8')
                                  || !records.Any(record => record[0] == '9'))
        {
            throw new InvalidOperationException("El archivo NACHA debe contener T1, T5, T8 y T9 en la secuencia esperada.");
        }

        var orderedBatches = context.Batches.ToList();
        if (records.Count(record => record[0] == '5') != orderedBatches.Count)
        {
            throw new InvalidOperationException("La cantidad de T5 no coincide con los lotes exportados.");
        }

        var currentRecordIndex = 1;
        for (var batchOrdinal = 0; batchOrdinal < orderedBatches.Count; batchOrdinal++)
        {
            var batch = orderedBatches[batchOrdinal];
            EnsureRecordType(records, currentRecordIndex, '5', $"Se esperaba T5 para el lote ordinal {batchOrdinal + 1}.");
            var batchHeaderRecord = records[currentRecordIndex++];

            var batchTransactions = (batch.Transactions.Count > 0
                    ? batch.Transactions
                    : context.Transactions.Where(transaction => transaction.AchBatchId == batch.Id))
                .OrderBy(transaction => transaction.Id)
                .ToList();
            if (batchTransactions.Count == 0)
            {
                throw new InvalidOperationException($"El lote ordinal {batchOrdinal + 1} no contiene entradas exportables.");
            }

            ValidateBatchSemantics(batch, batchTransactions, batchHeaderRecord, batchOrdinal);

            if (MatchesInterleavedShape(records, currentRecordIndex, batchTransactions.Count))
            {
                ValidateInterleavedEntries(records, ref currentRecordIndex, batchTransactions, batchOrdinal);
            }
            else
            {
                // Compatibility is restricted to DEVELOPMENT by NachaFileBuilder's LIVE gate.
                // It remains readable only for isolated legacy/shadow diagnostics.
                ValidateLegacyGroupedEntries(records, ref currentRecordIndex, batchTransactions, batchOrdinal);
            }

            EnsureRecordType(records, currentRecordIndex, '8', $"El lote ordinal {batchOrdinal + 1} debe cerrar con T8.");
            currentRecordIndex++;
        }

        EnsureRecordType(records, currentRecordIndex, '9', "El archivo NACHA debe cerrar con T9 de control.");
        currentRecordIndex++;

        for (var index = currentRecordIndex; index < records.Count; index++)
        {
            if (records[index] != new string('9', RecordLength))
            {
                throw new InvalidOperationException("El padding posterior a T9 debe contener únicamente el carácter 9.");
            }
        }
    }

    private static void ValidateBatchSemantics(
        Domain.Models.ACH.AchBatch batch,
        IReadOnlyList<Domain.Models.ACH.AchTransaction> transactions,
        string batchHeaderRecord,
        int batchOrdinal)
    {
        var creditLikeTransactions = transactions.Where(IsCreditLike).ToList();
        var exportedDescription = batchHeaderRecord.Substring(BatchHeaderDescriptionStart, BatchHeaderDescriptionLength).Trim();
        var expectedDescription = RequiredMassCreditDescription[..BatchHeaderDescriptionLength];

        if (creditLikeTransactions.Count > 1
            && !string.Equals(exportedDescription, expectedDescription, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"El lote ordinal {batchOrdinal + 1} debe usar MULTICREDIT para créditos/prenotificaciones masivas.");
        }

        if (creditLikeTransactions.Count > 0 && batch.EffectiveEntryDate == default)
        {
            throw new InvalidOperationException($"El lote ordinal {batchOrdinal + 1} requiere Fecha Descriptiva en T5 para el flujo vigente.");
        }
    }

    private static bool MatchesInterleavedShape(
        IReadOnlyList<string> records,
        int startIndex,
        int transactionCount)
    {
        var index = startIndex;
        for (var entryOrdinal = 0; entryOrdinal < transactionCount; entryOrdinal++)
        {
            if (index >= records.Count || records[index][0] != '6')
            {
                return false;
            }

            var indicator = records[index][AddendaIndicatorStart];
            index++;
            if (indicator == '1')
            {
                if (index >= records.Count || records[index][0] != '7')
                {
                    return false;
                }

                while (index < records.Count && records[index][0] == '7')
                {
                    index++;
                }
            }
            else if (indicator != '0')
            {
                return false;
            }
        }

        return index < records.Count && records[index][0] == '8';
    }

    private static void ValidateInterleavedEntries(
        IReadOnlyList<string> records,
        ref int currentRecordIndex,
        IReadOnlyList<Domain.Models.ACH.AchTransaction> transactions,
        int batchOrdinal)
    {
        for (var entryOrdinal = 0; entryOrdinal < transactions.Count; entryOrdinal++)
        {
            var transaction = transactions[entryOrdinal];
            EnsureRecordType(records, currentRecordIndex, '6', $"La entrada ordinal {entryOrdinal + 1} del lote {batchOrdinal + 1} debe generar T6.");
            var entryRecord = records[currentRecordIndex++];
            ValidateEntryAmount(transaction, batchOrdinal, entryOrdinal);

            var indicator = entryRecord[AddendaIndicatorStart];
            if (indicator == '0')
            {
                if (currentRecordIndex < records.Count && records[currentRecordIndex][0] == '7')
                {
                    throw new InvalidOperationException($"ACHCOL-T6-ADDENDA-INDICATOR falló en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
                }

                continue;
            }

            if (indicator != '1')
            {
                throw new InvalidOperationException($"ACHCOL-T6-ADDENDA-INDICATOR contiene un valor no permitido en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
            }

            var addendaOrdinal = 0;
            while (currentRecordIndex < records.Count && records[currentRecordIndex][0] == '7')
            {
                var addendaRecord = records[currentRecordIndex++];
                ValidateTraceSuffix(entryRecord, addendaRecord, batchOrdinal, entryOrdinal);
                if (addendaOrdinal < transaction.Addendas.Count)
                {
                    var addenda = transaction.Addendas.ElementAt(addendaOrdinal);
                    ValidateAddendaCompatibility(transaction, addenda, batchOrdinal, entryOrdinal);
                    ValidateAddendaRecord(addenda, addendaRecord, batchOrdinal, entryOrdinal);
                }

                addendaOrdinal++;
            }

            if (addendaOrdinal == 0)
            {
                throw new InvalidOperationException($"El indicador T6 declara addenda sin T7 asociado en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
            }

            if (transaction.Addendas.Count > 0 && addendaOrdinal != transaction.Addendas.Count)
            {
                throw new InvalidOperationException($"La cantidad de T7 no coincide con el modelo canónico en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
            }
        }
    }

    private static void ValidateLegacyGroupedEntries(
        IReadOnlyList<string> records,
        ref int currentRecordIndex,
        IReadOnlyList<Domain.Models.ACH.AchTransaction> transactions,
        int batchOrdinal)
    {
        var entryRecords = new List<string>(transactions.Count);
        for (var entryOrdinal = 0; entryOrdinal < transactions.Count; entryOrdinal++)
        {
            EnsureRecordType(records, currentRecordIndex, '6', $"La entrada ordinal {entryOrdinal + 1} del lote {batchOrdinal + 1} debe generar T6.");
            entryRecords.Add(records[currentRecordIndex++]);
            ValidateEntryAmount(transactions[entryOrdinal], batchOrdinal, entryOrdinal);
        }

        for (var entryOrdinal = 0; entryOrdinal < transactions.Count; entryOrdinal++)
        {
            var transaction = transactions[entryOrdinal];
            foreach (var addenda in transaction.Addendas)
            {
                EnsureRecordType(records, currentRecordIndex, '7', $"La entrada ordinal {entryOrdinal + 1} debe generar T7 para cada addenda.");
                var addendaRecord = records[currentRecordIndex++];
                ValidateAddendaCompatibility(transaction, addenda, batchOrdinal, entryOrdinal);
                ValidateAddendaRecord(addenda, addendaRecord, batchOrdinal, entryOrdinal);
            }
        }
    }

    private static void ValidateEntryAmount(
        Domain.Models.ACH.AchTransaction transaction,
        int batchOrdinal,
        int entryOrdinal)
    {
        if (transaction.IsPrenotification && transaction.Amount != 0)
        {
            throw new InvalidOperationException($"La entrada ordinal {entryOrdinal + 1} del lote {batchOrdinal + 1} es prenotificación y requiere monto cero.");
        }

        if (!transaction.IsPrenotification && transaction.Amount <= 0)
        {
            throw new InvalidOperationException($"La entrada ordinal {entryOrdinal + 1} del lote {batchOrdinal + 1} requiere monto mayor a cero.");
        }
    }

    private static void ValidateTraceSuffix(string entryRecord, string addendaRecord, int batchOrdinal, int entryOrdinal)
    {
        if (addendaRecord.Substring(1, 2) != "05")
        {
            return;
        }

        if (!string.Equals(
                entryRecord.Substring(TraceSuffixStart, TraceSuffixLength),
                addendaRecord.Substring(AddendaTraceSuffixStart, TraceSuffixLength),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"ACHCOL-T7-TRACE-SUFFIX-MATCH falló en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }
    }

    private static void ValidateAddendaCompatibility(
        Domain.Models.ACH.AchTransaction transaction,
        Domain.Models.ACH.AchTransactionAddenda addenda,
        int batchOrdinal,
        int entryOrdinal)
    {
        if (addenda.BusinessType == AchAddendaBusinessType.Return
            && transaction.Type is not (TransactionTypeEnum.Return or TransactionTypeEnum.Reversal))
        {
            throw new InvalidOperationException($"La variante de devolución es incompatible en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }

        if (addenda.AddendaType == "99"
            && transaction.Type is not (TransactionTypeEnum.Return or TransactionTypeEnum.Reversal))
        {
            throw new InvalidOperationException($"La addenda 99 no aplica en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }

        if (addenda.AddendaType == "05" && transaction.Type == TransactionTypeEnum.Return)
        {
            throw new InvalidOperationException($"La variante de addenda no aplica en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }
    }

    private static void ValidateAddendaRecord(
        Domain.Models.ACH.AchTransactionAddenda addenda,
        string addendaRecord,
        int batchOrdinal,
        int entryOrdinal)
    {
        var addendaType = addendaRecord.Substring(1, 2);
        if (!string.Equals(addendaType, addenda.AddendaType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"El tipo de addenda no coincide en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }

        if (addendaType != "99")
        {
            return;
        }

        var returnReason = addendaRecord.Substring(ReturnReasonStart, ReturnReasonLength).Trim();
        var originalTrace = addendaRecord.Substring(OriginalTraceStart, OriginalTraceLength).Trim();
        var newTrace = addendaRecord.Substring(NewTraceStart, NewTraceLength).Trim();
        var entrySequence = addendaRecord.Substring(EntrySequenceStart, EntrySequenceLength).Trim();

        if (!(returnReason.Length == 3 && returnReason.StartsWith('R'))
            && !string.Equals(returnReason, "DEV14", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"La causal de devolución es inválida en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }

        if (originalTrace.Length != 15 || originalTrace.Any(character => !char.IsDigit(character)))
        {
            throw new InvalidOperationException($"El trace original no cumple longitud/tipo en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }

        if (newTrace.Length != 15 || newTrace.Any(character => !char.IsDigit(character)))
        {
            throw new InvalidOperationException($"El nuevo trace no cumple longitud/tipo en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }

        if (entrySequence.Length != 7 || entrySequence.Any(character => !char.IsDigit(character)))
        {
            throw new InvalidOperationException($"La secuencia asociada no cumple longitud/tipo en lote ordinal {batchOrdinal + 1}, entrada ordinal {entryOrdinal + 1}.");
        }
    }

    private static void EnsureRecordType(IReadOnlyList<string> records, int index, char expectedType, string errorMessage)
    {
        if (index >= records.Count || records[index][0] != expectedType)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static bool IsCreditLike(Domain.Models.ACH.AchTransaction transaction)
        => transaction.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification;
}
