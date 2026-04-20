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

        if (records[0][0] != '1' || !records.Any(r => r[0] == '5') || !records.Any(r => r[0] == '8') || !records.Any(r => r[0] == '9'))
        {
            throw new InvalidOperationException("El archivo NACHA debe contener registros tipo 1, 5, 8 y 9 en la secuencia esperada.");
        }

        var orderedBatches = context.Batches.OrderBy(batch => batch.Id).ToList();
        if (records.Count(record => record[0] == '5') != orderedBatches.Count)
        {
            throw new InvalidOperationException("La cantidad de encabezados de lote tipo 5 no coincide con los lotes exportados.");
        }

        var currentRecordIndex = 1;
        foreach (var batch in orderedBatches)
        {
            EnsureRecordType(records, currentRecordIndex, '5', $"Se esperaba encabezado tipo 5 para el lote {batch.Id}.");
            var batchHeaderRecord = records[currentRecordIndex];
            currentRecordIndex++;

            var batchTransactions = context.Transactions
                .Where(tx => tx.AchBatchId == batch.Id)
                .OrderBy(tx => tx.Id)
                .ToList();
            if (!batchTransactions.Any())
            {
                throw new InvalidOperationException($"El lote {batch.Id} no contiene transacciones exportables.");
            }

            var creditLikeTransactions = batchTransactions.Where(IsCreditLike).ToList();
            var exportedDescription = batchHeaderRecord.Substring(BatchHeaderDescriptionStart, BatchHeaderDescriptionLength).Trim();
            var expectedDescription = RequiredMassCreditDescription.Length > BatchHeaderDescriptionLength
                ? RequiredMassCreditDescription[..BatchHeaderDescriptionLength]
                : RequiredMassCreditDescription;

            if (creditLikeTransactions.Count > 1 && !string.Equals(exportedDescription, expectedDescription, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"El lote {batch.Id} debe usar la descripción MULTICREDIT para créditos/prenotificaciones masivas.");
            }

            if (creditLikeTransactions.Any() && batch.EffectiveEntryDate == default)
            {
                throw new InvalidOperationException($"El lote {batch.Id} requiere Fecha Descriptiva en tipo 5 para créditos y prenotificaciones crédito.");
            }

            if (batchTransactions.Any(tx => tx.Addendas.Count == 0))
            {
                throw new InvalidOperationException($"El lote {batch.Id} contiene transacciones sin addenda asociada.");
            }

            foreach (var tx in batchTransactions)
            {
                EnsureRecordType(records, currentRecordIndex, '6', $"La transacción {tx.Id} debe generar un registro tipo 6.");
                currentRecordIndex++;

                if (tx.IsPrenotification && tx.Amount != 0)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} es prenotificación y debe exportarse con monto cero.");
                }

                if (!tx.IsPrenotification && tx.Amount <= 0)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} debe exportarse con monto monetario mayor a cero.");
                }
            }

            foreach (var tx in batchTransactions)
            {
                foreach (var addenda in tx.Addendas)
                {
                    EnsureRecordType(records, currentRecordIndex, '7', $"La transacción {tx.Id} debe generar un registro tipo 7 por cada addenda.");
                    var addendaRecord = records[currentRecordIndex];
                    currentRecordIndex++;

                    if (addenda.BusinessType == AchAddendaBusinessType.Return && tx.Type != TransactionTypeEnum.Return)
                    {
                        throw new InvalidOperationException($"La transacción {tx.Id} tiene una addenda de devolución incompatible con su tipo efectivo.");
                    }

                    if (addenda.AddendaType == "99" && tx.Type != TransactionTypeEnum.Return)
                    {
                        throw new InvalidOperationException($"La transacción {tx.Id} solo puede usar addenda 99 cuando el tipo efectivo es Return.");
                    }

                    if (addenda.AddendaType == "05" && tx.Type == TransactionTypeEnum.Return)
                    {
                        throw new InvalidOperationException($"La transacción {tx.Id} de tipo Return debe usar addenda 99.");
                    }

                    ValidateAddendaRecord(tx, addenda, addendaRecord);
                }
            }

            EnsureRecordType(records, currentRecordIndex, '8', $"El lote {batch.Id} debe cerrar con un registro tipo 8.");
            currentRecordIndex++;
        }

        EnsureRecordType(records, currentRecordIndex, '9', "El archivo NACHA debe cerrar con un registro tipo 9 de control.");
        currentRecordIndex++;

        for (var index = currentRecordIndex; index < records.Count; index++)
        {
            if (records[index] != new string('9', RecordLength))
            {
                throw new InvalidOperationException("Los registros de padding posteriores al tipo 9 deben contener únicamente el carácter 9.");
            }
        }
    }

    private static void ValidateAddendaRecord(Domain.Models.ACH.AchTransaction transaction, Domain.Models.ACH.AchTransactionAddenda addenda, string addendaRecord)
    {
        var addendaType = addendaRecord.Substring(1, 2);
        if (!string.Equals(addendaType, addenda.AddendaType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"La transacción {transaction.Id} tiene inconsistencia entre la addenda declarada ({addenda.AddendaType}) y el registro tipo 7 exportado ({addendaType}).");
        }

        if (addendaType == "99")
        {
            var returnReason = addendaRecord.Substring(ReturnReasonStart, ReturnReasonLength).Trim();
            var originalTrace = addendaRecord.Substring(OriginalTraceStart, OriginalTraceLength).Trim();
            var newTrace = addendaRecord.Substring(NewTraceStart, NewTraceLength).Trim();
            var entrySequence = addendaRecord.Substring(EntrySequenceStart, EntrySequenceLength).Trim();

            if (!(returnReason.Length == 3 && returnReason.StartsWith('R')) && !string.Equals(returnReason, "DEV14", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} debe exportar una causal de devolución Rxx o DEV14 en la addenda tipo 99.");
            }

            if (originalTrace.Length != 15 || originalTrace.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} debe exportar el trace original de 15 dígitos en la addenda tipo 99.");
            }

            if (newTrace.Length != 15 || newTrace.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} debe exportar el nuevo trace de 15 dígitos en la addenda tipo 99.");
            }

            if (entrySequence.Length != 7 || entrySequence.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} debe exportar la secuencia tipo 6 de 7 dígitos en la addenda tipo 99.");
            }
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
