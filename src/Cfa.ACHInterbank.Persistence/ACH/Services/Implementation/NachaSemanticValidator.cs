using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaSemanticValidator : INachaSemanticValidator
{
    private const int RecordLength = 106;
    private const int BatchHeaderDescriptionStart = 53;
    private const int BatchHeaderDescriptionLength = 10;

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

        var batchHeaderRecords = records.Where(record => record[0] == '5').ToList();
        if (batchHeaderRecords.Count != context.Batches.Count)
        {
            throw new InvalidOperationException("La cantidad de encabezados de lote tipo 5 no coincide con los lotes exportados.");
        }

        for (var batchIndex = 0; batchIndex < context.Batches.Count; batchIndex++)
        {
            var batch = context.Batches[batchIndex];
            var batchHeaderRecord = batchHeaderRecords[batchIndex];
            var batchTransactions = context.Transactions.Where(tx => tx.AchBatchId == batch.Id).ToList();
            if (!batchTransactions.Any())
            {
                throw new InvalidOperationException($"El lote {batch.Id} no contiene transacciones exportables.");
            }

            var creditLikeTransactions = batchTransactions.Where(IsCreditLike).ToList();
            var exportedDescription = batchHeaderRecord.Substring(BatchHeaderDescriptionStart, BatchHeaderDescriptionLength).Trim();
            if (creditLikeTransactions.Count > 1 && !string.Equals(exportedDescription, "MULTICREDIT", StringComparison.OrdinalIgnoreCase))
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
                foreach (var addenda in tx.Addendas)
                {
                    if (addenda.AddendaType == "99" && tx.Type != TransactionTypeEnum.Return)
                    {
                        throw new InvalidOperationException($"La transacción {tx.Id} solo puede usar addenda 99 cuando el tipo efectivo es Return.");
                    }

                    if (addenda.AddendaType == "05" && tx.Type == TransactionTypeEnum.Return)
                    {
                        throw new InvalidOperationException($"La transacción {tx.Id} de tipo Return debe usar addenda 99.");
                    }
                }
            }
        }
    }

    private static bool IsCreditLike(Domain.Models.ACH.AchTransaction transaction)
        => transaction.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification;
}
