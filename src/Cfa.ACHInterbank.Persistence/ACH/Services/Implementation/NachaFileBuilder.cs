using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileBuilder : INachaFileBuilder
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly INachaRecordDataProvider _recordDataProvider;
    private readonly INachaSemanticValidator _nachaSemanticValidator;

    public NachaFileBuilder(
        AchDbContext context,
        IBankHoliday holidayService,
        INachaRecordDataProvider recordDataProvider,
        INachaSemanticValidator nachaSemanticValidator)
    {
        _context = context;
        _holidayService = holidayService;
        _recordDataProvider = recordDataProvider;
        _nachaSemanticValidator = nachaSemanticValidator;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO PRINCIPAL: Generar archivo NACHA-M por lotes
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct = default)
    {
        var batches = await _context.AchBatches
            .AsNoTracking()
            .Include(b => b.AchCycle)
                .ThenInclude(c => c!.ClearingHouse)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.Addendas)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.SourceInstitution)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.DestinationInstitution)
            .Where(b => batchIds.Contains(b.Id))
            .ToListAsync(ct);

        if (!batches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var cycle = batches.First().AchCycle!;
        var nachaHeader = await LoadHeaderAsync(cycle.Id, ct);

        var layoutCache = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .ToDictionaryAsync(l => l.RecordCode!, ct);

        var transactions = batches.SelectMany(b => b.Transactions).ToList();
        await ValidateTransactionsForSendAsync(transactions, ct);
        var definitions = await LoadDefinitionsAsync(ct);
        var context = new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO ALTERNATIVO: Generar NACHA-M por ciclo
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException($"No existe el ciclo {cycleId}.");

        var cycleBatches = await _context.AchBatches
            .AsNoTracking()
            .Where(b => b.AchCycleId == cycleId)
            .ToListAsync(ct);

        var transactions = await _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.Addendas)
            .Include(t => t.AchBatch)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Where(t => t.AchCycleId == cycleId)
            .ToListAsync(ct);

        var transactionBatches = transactions
            .Where(t => t.AchBatch is not null)
            .Select(t => t.AchBatch!)
            .GroupBy(b => b.Id)
            .Select(g => g.First())
            .ToList();

        var batches = cycleBatches
            .Concat(transactionBatches)
            .GroupBy(b => b.Id)
            .Select(g => g.First())
            .ToList();

        if (transactions.Count == 0)
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene transacciones para exportar.");

        if (batches.Count == 0)
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene lotes asociados para exportar.");

        var nachaHeader = await LoadHeaderAsync(cycle.Id, ct);
        var layoutCache = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .ToDictionaryAsync(l => l.RecordCode!, ct);

        await ValidateTransactionsForSendAsync(transactions, ct);
        var definitions = await LoadDefinitionsAsync(ct);
        var context = new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO DE INTERFAZ: Cumple contrato de INachaFileBuilder
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct = default)
    {
        var layout = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .FirstOrDefaultAsync(l => l.RecordCode == recordType, ct)
            ?? throw new InvalidOperationException($"Layout no encontrado para '{recordType}'.");

        return await BuildRecordInternalAsync(recordType, entity, layout);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO INTERNO OPTIMIZADO
    // ─────────────────────────────────────────────────────────────────────────────
    private static Task<string> BuildRecordInternalAsync<T>(string recordType, T entity, NachaRecordLayout layout)
    {
        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        var entityType = entity?.GetType() ?? typeof(T);

        foreach (var field in fields)
        {
            object? raw;
            if (TryResolveConstant(field.DbColumn, out raw))
            {
                string constantValue = FormatValue(raw, field);

                if (constantValue.Length > field.Length)
                    constantValue = constantValue.Substring(0, field.Length);

                constantValue = field.Justification == 'R'
                    ? constantValue.PadLeft(field.Length, field.PadChar)
                    : constantValue.PadRight(field.Length, field.PadChar);

                int constantStart = field.StartPosition - 1;
                constantValue.CopyTo(0, buffer, constantStart, constantValue.Length);
                continue;
            }

            var prop = ResolveProperty(entityType, field.DbColumn)
                ?? ResolveProperty(entityType, field.FieldName);
            if (prop == null) continue;

            raw = prop.GetValue(entity);
            string value = FormatValue(raw, field);

            if (recordType == "5" && string.Equals(field.FieldName, "SettlementDate", StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeBatchSettlementDate(value);
            }

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            value.CopyTo(0, buffer, start, value.Length);
        }

        return Task.FromResult(new string(buffer));
    }

    private static Task<string> BuildRecordInternalAsync(
        string recordType,
        IReadOnlyDictionary<string, object?> values,
        NachaRecordLayout layout)
    {
        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        foreach (var field in fields)
        {
            object? raw;
            if (!TryResolveConstant(field.DbColumn, out raw) &&
                !TryResolveValue(values, field.DbColumn, out raw) &&
                !TryResolveValue(values, field.FieldName, out raw))
            {
                continue;
            }

            var value = FormatValue(raw, field);

            if (recordType == "5" && string.Equals(field.FieldName, "SettlementDate", StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeBatchSettlementDate(value);
            }

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            value.CopyTo(0, buffer, start, value.Length);
        }

        return Task.FromResult(new string(buffer));
    }

    private static string NormalizeBatchSettlementDate(string? value)
    {
        var validation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(value);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Error Fatal 65 en Fecha de Compensación Juliana.");
        }

        return validation.FormattedValue;
    }

    private static System.Reflection.PropertyInfo? ResolveProperty(Type type, string? dbColumn)
    {
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return null;
        }

        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var candidate in EnumerateIdentifierCandidates(dbColumn))
        {
            var property = type.GetProperty(candidate,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (property is not null)
            {
                return property;
            }

            var normalizedTarget = NormalizeIdentifier(candidate);
            property = properties.FirstOrDefault(p => NormalizeIdentifier(p.Name) == normalizedTarget);
            if (property is not null)
            {
                return property;
            }
        }

        return null;
    }

    private static bool TryResolveConstant(string? dbColumn, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return false;
        }

        if (!dbColumn.StartsWith("CONST:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        raw = dbColumn[6..];
        return true;
    }

    private static bool TryResolveValue(IReadOnlyDictionary<string, object?> values, string? dbColumn, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(dbColumn))
        {
            return false;
        }

        foreach (var candidate in EnumerateIdentifierCandidates(dbColumn))
        {
            if (values.TryGetValue(candidate, out raw))
            {
                return true;
            }

            var exactIgnoreCase = values.FirstOrDefault(kv => string.Equals(kv.Key, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(exactIgnoreCase.Key))
            {
                raw = exactIgnoreCase.Value;
                return true;
            }

            var normalizedTarget = NormalizeIdentifier(candidate);
            var normalizedMatch = values.FirstOrDefault(kv => NormalizeIdentifier(kv.Key) == normalizedTarget);
            if (!string.IsNullOrEmpty(normalizedMatch.Key))
            {
                raw = normalizedMatch.Value;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateIdentifierCandidates(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            yield break;
        }

        yield return trimmed;

        var separators = new[] { '.', ':', '/' };
        var segments = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length > 1)
        {
            yield return segments[^1];
        }
    }

    private static string NormalizeIdentifier(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // AUXILIAR: Formateo de valores
    // ─────────────────────────────────────────────────────────────────────────────
    private static string FormatValue(object? raw, NachaRecordField field)
    {
        if (raw == null) return string.Empty;

        return raw switch
        {
            DateTime dt => dt.ToString(field.Format ?? "yyyyMMdd"),
            decimal d => ((long)(d * 100)).ToString(),
            bool b => b ? "1" : "0",
            _ => raw.ToString() ?? string.Empty
        };
    }

    private async Task<string> BuildFileAsync(
        NachaBuildContext context,
        IReadOnlyList<NachaRecordDefinition> definitions,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaHeader? header,
        CancellationToken ct)
    {
        var sb = new StringBuilder(capacity: 10240);
        var orderedBatches = context.Batches.OrderBy(b => b.Id).ToList();
        var batchSequenceById = orderedBatches
            .Select((batch, index) => new { batch.Id, BatchNumber = index + 1 })
            .ToDictionary(item => item.Id, item => item.BatchNumber);

        if (!orderedBatches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        long totalDebit = 0, totalCredit = 0;
        int recordCount = 0, batchCount = orderedBatches.Count, entryAddendaCount = 0;

        var companyEntryDescriptionCatalog = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToListAsync(ct);

        foreach (var definition in definitions)
        {
            if (!definition.IsEnabled)
            {
                continue;
            }

            switch (definition.RecordCode)
            {
                case "1":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        new[] { FileHeaderRecord.From(context.Cycle, context.Transactions, header) },
                        context,
                        ct);
                    break;
                case "5":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        orderedBatches.Select(batch =>
                        {
                            var batchTransactions = context.Transactions.Where(t => t.AchBatchId == batch.Id).ToList();
                            string secCode = ResolveStandardEntryClassCode(batch, batchTransactions, companyEntryDescriptionCatalog);
                            var batchDescription = ResolveBatchEntryDescription(batch, batchTransactions);
                            return BatchHeaderRecord.From(batch, secCode, batchSequenceById[batch.Id], batchDescription);
                        }),
                        context,
                        ct);
                    break;
                case "6":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        await BuildEntryDetailRecordsAsync(context.Transactions, ct),
                        context,
                        ct);
                    entryAddendaCount += context.Transactions.Count;
                    foreach (var tx in context.Transactions)
                    {
                        if (tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
                            totalDebit += (long)(tx.Amount * 100);
                        else
                            totalCredit += (long)(tx.Amount * 100);
                    }
                    break;
                case "7":
                    recordCount += AppendTypedAddendaRecords(sb, orderedBatches);
                    entryAddendaCount += context.Transactions.Sum(tx => BuildAddendasForTransaction(tx).Count());
                    break;
                case "8":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        orderedBatches.Select(batch =>
                            BatchControlRecord.From(
                                batch,
                                batch.Transactions.Count + batch.Transactions.Sum(t => BuildAddendasForTransaction(t).Count()),
                                SumBatchDebit(batch),
                                SumBatchCredit(batch),
                                batchSequenceById[batch.Id])),
                        context,
                        ct);
                    break;
                case "9":
                    var totalRecords = recordCount + 1;
                    var blockCount = (int)Math.Ceiling(totalRecords / 10m);
                    var paddingNeeded = (blockCount * 10) - totalRecords;
                    var fileControl = FileControlRecord.From(context.Cycle, orderedBatches, batchCount, blockCount, entryAddendaCount, totalDebit, totalCredit);

                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        new[] { fileControl },
                        context,
                        ct);

                    if (paddingNeeded > 0)
                    {
                        var paddingRecord = new string('9', layoutCache["9"].TotalLength);
                        for (int i = 0; i < paddingNeeded; i++)
                        {
                            sb.Append(paddingRecord);
                        }
                    }
                    break;
            }
        }

        var fileContent = sb.ToString();
        _nachaSemanticValidator.Validate(fileContent, context);
        return fileContent;
    }

    private async Task<int> AppendCustomOrConfiguredAsync(
        StringBuilder sb,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        IEnumerable<object> fallbackRecords,
        NachaBuildContext context,
        CancellationToken ct)
    {
        var layout = layoutCache[definition.RecordCode];
        var fallbackList = fallbackRecords.ToList();

        var forceFallback = definition.RecordCode is "1" or "5" or "8" or "9";
        IReadOnlyList<object> records = (definition.SourceType == NachaRecordSourceType.Custom || forceFallback)
            ? fallbackList
            : await _recordDataProvider.GetRecordsAsync(definition, context, ct);

        if (records.Count == 0 && fallbackList.Count > 0)
        {
            records = fallbackList;
        }

        var count = 0;
        foreach (var record in records)
        {
            count++;
            if (record is IReadOnlyDictionary<string, object?> dict)
            {
                sb.Append(await BuildRecordInternalAsync(definition.RecordCode, dict, layout));
            }
            else
            {
                sb.Append(await BuildRecordInternalAsync(definition.RecordCode, record, layout));
            }
        }

        return count;
    }

    private static long SumBatchDebit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private static long SumBatchCredit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private async Task<IReadOnlyList<NachaRecordDefinition>> LoadDefinitionsAsync(CancellationToken ct)
    {
        var definitions = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .OrderBy(d => d.Sequence)
            .ToListAsync(ct);

        return definitions.Count > 0 ? definitions : BuildDefaultDefinitions();
    }

    private static IReadOnlyList<NachaRecordDefinition> BuildDefaultDefinitions()
    {
        return new List<NachaRecordDefinition>
        {
            new()
            {
                RecordCode = "1",
                Sequence = 10,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "5",
                Sequence = 20,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "6",
                Sequence = 30,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransaction),
                FilterKey = "BatchId",
                IsEnabled = true
            },
            new()
            {
                RecordCode = "7",
                Sequence = 40,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransactionAddenda),
                FilterKey = "BatchId",
                IsEnabled = true
            },
            new()
            {
                RecordCode = "8",
                Sequence = 50,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "9",
                Sequence = 60,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            }
        };
    }

    private static IEnumerable<AchTransactionAddenda> BuildAddendasForTransaction(AchTransaction tx)
    {
        if (tx.Addendas != null && tx.Addendas.Any())
        {
            return tx.Addendas.OrderBy(a => a.SequenceNumber);
        }

        return new[]
        {
            new AchTransactionAddenda
            {
                AddendaType = "05",
                BusinessType = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? AchAddendaBusinessType.Debit
                    : AchAddendaBusinessType.Credit,
                Purpose = tx.AchBatch?.CompanyEntryDescription,
                // Transición: no propagar referencia legado de transacción como semántica funcional.
                Reference = new string('0', 53),
                CollectorId = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal ? tx.CompanyIdentification : null,
                ReceiverCustomerCode = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal ? tx.RecipientIdNumber : null,
                ServiceDescription = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? tx.AchBatch?.CompanyEntryDescription
                    : null,
                SequenceNumber = 1
            }
        };
    }

    private static int AppendTypedAddendaRecords(StringBuilder sb, IReadOnlyList<AchBatch> orderedBatches)
    {
        var count = 0;
        foreach (var batch in orderedBatches)
        {
            foreach (var transaction in batch.Transactions.OrderBy(t => t.Id))
            {
                foreach (var addenda in BuildAddendasForTransaction(transaction))
                {
                    sb.Append(BuildType7Record(batch, transaction, addenda));
                    count++;
                }
            }
        }

        return count;
    }

    private static string BuildType7Record(AchBatch batch, AchTransaction transaction, AchTransactionAddenda addenda)
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

    private async Task<NachaHeader?> LoadHeaderAsync(string cycleId, CancellationToken ct)
    {
        return await _context.NachaHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.AchCycleId == cycleId, ct);
    }

    private sealed record FileHeaderRecord
    {
        public string? PriorityCode { get; init; }
        public string? ImmediateDestination { get; init; }
        public string? ImmediateOrigin { get; init; }
        public DateTime FileCreationDate { get; init; }
        public DateTime FileCreationTime { get; init; }
        public string? FileIdModifier { get; init; }
        public string? RecordSize { get; init; }
        public string? BlockingFactor { get; init; }
        public string? FormatCode { get; init; }
        public string? ImmediateDestinationName { get; init; }
        public string? ImmediateOriginName { get; init; }
        public string? ReferenceCode { get; init; }
        public string? CycleName { get; init; }
        public DateTime ProcessingDate { get; init; }

        public static FileHeaderRecord From(AchCycle cycle, IReadOnlyCollection<AchTransaction> transactions, NachaHeader? header)
        {
            var now = DateTime.UtcNow;
            var firstTransaction = transactions
                .OrderBy(t => t.Id)
                .FirstOrDefault();

            var destinationDfi = CoalesceNonEmpty(
                header?.ImmediateDestination,
                cycle.ClearingHouse?.OriginCode,
                firstTransaction?.ReceivingDFI);

            var originDfi = CoalesceNonEmpty(
                header?.ImmediateOrigin,
                firstTransaction?.OriginatingDFI);

            var destinationName = CoalesceNonEmpty(
                header?.ImmediateDestinationName,
                cycle.ClearingHouse?.Name,
                firstTransaction?.DestinationInstitution?.Name);

            var originName = CoalesceNonEmpty(
                header?.ImmediateOriginName,
                firstTransaction?.SourceInstitution?.Name);

            return new FileHeaderRecord
            {
                PriorityCode = CoalesceNonEmpty(header?.PriorityCode, "01"),
                ImmediateDestination = destinationDfi,
                ImmediateOrigin = originDfi,
                FileCreationDate = ParseDate(header?.FileCreationDate) ?? now,
                FileCreationTime = ParseTime(header?.FileCreationTime) ?? now,
                FileIdModifier = CoalesceNonEmpty(header?.FileIdModifier, "A"),
                RecordSize = string.IsNullOrWhiteSpace(header?.RecordSize) ? "106" : header!.RecordSize,
                BlockingFactor = string.IsNullOrWhiteSpace(header?.BlockingFactor) ? "10" : header!.BlockingFactor,
                FormatCode = string.IsNullOrWhiteSpace(header?.FormatCode) ? "1" : header!.FormatCode,
                ImmediateDestinationName = destinationName,
                ImmediateOriginName = originName,
                ReferenceCode = CoalesceNonEmpty(header?.ReferenceCode, cycle.CycleName),
                CycleName = cycle.CycleName,
                ProcessingDate = cycle.ProcessingDate
            };
        }

        private static string? CoalesceNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        }

        private static DateTime? ParseDate(string? value)
        {
            return DateTime.TryParse(value, out var parsed) ? parsed : null;
        }

        private static DateTime? ParseTime(string? value)
        {
            return DateTime.TryParse(value, out var parsed) ? parsed : null;
        }
    }

    private sealed record CompanyEntryDescriptionCatalogItem(string Term, string StandardEntryClassCode);

    private static string ResolveBatchEntryDescription(AchBatch batch, IReadOnlyCollection<AchTransaction> batchTransactions)
    {
        var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
        var creditLikeCount = batchTransactions.Count(tx => tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification);
        return creditLikeCount > 1 ? "MULTICREDIT" : description;
    }


    private sealed record BatchHeaderRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public string CompanyDiscretionaryData { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;
        public string StandardEntryClassCode { get; init; } = "PPD";
        public string CompanyEntryDescription { get; init; } = string.Empty;
        public DateTime CompanyDescriptiveDate { get; init; }
        public DateTime EffectiveEntryDate { get; init; }
        public string SettlementDate { get; init; } = string.Empty;
        public string OriginatorStatusCode { get; init; } = "1";
        public string OriginatingDFI { get; init; } = string.Empty;
        public int BatchNumber { get; init; }

        public static BatchHeaderRecord From(AchBatch batch, string standardEntryClassCode, int batchNumber, string companyEntryDescription)
        {
            if (standardEntryClassCode is not ("PPD" or "CCD"))
            {
                throw new InvalidOperationException("Error Fatal ID 20: Tipo de servicio del lote inválido. Solo se permite PPD o CCD.");
            }

            return new BatchHeaderRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                CompanyName = batch.CompanyName,
                CompanyDiscretionaryData = string.Empty,
                CompanyIdentification = batch.CompanyIdentification,
                StandardEntryClassCode = standardEntryClassCode,
                CompanyEntryDescription = companyEntryDescription,
                CompanyDescriptiveDate = batch.EffectiveEntryDate,
                EffectiveEntryDate = batch.EffectiveEntryDate,
                SettlementDate = string.Empty,
                OriginatorStatusCode = "1",
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }
    }

    private static string ResolveStandardEntryClassCode(
        AchBatch batch,
        IReadOnlyCollection<AchTransaction> batchTransactions,
        IReadOnlyCollection<CompanyEntryDescriptionCatalogItem> catalog)
    {
        string description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
        var termMatch = catalog.FirstOrDefault(item => string.Equals(item.Term, description, StringComparison.OrdinalIgnoreCase));
        if (termMatch is not null)
        {
            return termMatch.StandardEntryClassCode;
        }

        throw new InvalidOperationException("Error Fatal ID 20: Tipo de servicio del lote inválido. El concepto no está parametrizado en el catálogo.");
    }

    private sealed record EntryDetailRecord
    {
        public string TransactionCode { get; init; } = string.Empty;
        public string ReceivingDFI { get; init; } = string.Empty;
        public string CheckDigit { get; init; } = string.Empty;
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string RecipientIdNumber { get; init; } = string.Empty;
        public string ReceiverName { get; init; } = string.Empty;
        public string DiscretionaryData { get; init; } = string.Empty;
        public string AddendumIndicator { get; init; } = "1";
        public string TraceNumber { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;

        public static EntryDetailRecord From(AchTransaction tx, string receiverName)
        {
            var receivingDfi = (tx.ReceivingDFI ?? string.Empty).Trim();
            if (receivingDfi.Length != 8 || receivingDfi.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException("Error Fatal ID 35: el Código Entidad Participante Receptor (posiciones 4-11) debe contener 8 dígitos numéricos para calcular el dígito de chequeo.");
            }

            var expectedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingDfi);
            var destinationCheckDigit = tx.DestinationInstitution?.CheckDigit?.Trim();
            var checkDigit = string.IsNullOrWhiteSpace(destinationCheckDigit) ? expectedCheckDigit : destinationCheckDigit;

            if (!string.Equals(checkDigit, expectedCheckDigit, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Error Fatal ID 35: inconsistencia de dígito de chequeo para la entidad receptora {receivingDfi}. Base de datos={checkDigit}, calculado={expectedCheckDigit}.");
            }

            return new EntryDetailRecord
            {
                TransactionCode = tx.TransactionCode,
                ReceivingDFI = receivingDfi,
                CheckDigit = checkDigit,
                DestinationAccountNumber = tx.DestinationAccountNumber,
                Amount = tx.Amount,
                RecipientIdNumber = tx.RecipientIdNumber,
                ReceiverName = receiverName,
                DiscretionaryData = tx.DiscretionaryData,
                AddendumIndicator = "1",
                TraceNumber = tx.TraceNumber,
                CompanyIdentification = tx.CompanyIdentification
            };
        }
    }

    private async Task<IReadOnlyList<EntryDetailRecord>> BuildEntryDetailRecordsAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var records = new List<EntryDetailRecord>(transactions.Count);

        foreach (var transaction in transactions.OrderBy(t => t.Id))
        {
            var receiverName = await ResolveReceiverNameForType6Async(transaction, ct);
            var normalizedReceiverName = ResolveReceiverNameWithFallback(transaction, receiverName);

            if (string.IsNullOrWhiteSpace(normalizedReceiverName))
            {
                throw new InvalidOperationException($"Error Fatal ID 22: la transacción {transaction.Id} no tiene Nombre del Usuario Receptor válido para posiciones 63-84 del registro tipo 6. Revise Cliente/Tercero asociado y número de cuenta destino.");
            }

            records.Add(EntryDetailRecord.From(transaction, normalizedReceiverName));
        }

        return records;
    }

    private async Task<string?> ResolveReceiverNameForType6Async(AchTransaction transaction, CancellationToken ct)
    {
        var destinationAccount = (transaction.DestinationAccountNumber ?? string.Empty).Trim();
        var recipientId = (transaction.RecipientIdNumber ?? string.Empty).Trim();

        Customer? customer;
        if (!string.IsNullOrWhiteSpace(recipientId))
        {
            customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.DocumentNumber == recipientId && c.Accounts.Any(a => a.AccountNumber == destinationAccount), ct);

            if (customer is not null)
            {
                return BuildReceiverName(customer);
            }
        }

        var accountMatches = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Accounts)
            .Where(c => c.Accounts.Any(a => a.AccountNumber == destinationAccount))
            .ToListAsync(ct);

        if (accountMatches.Count == 1)
        {
            return BuildReceiverName(accountMatches[0]);
        }

        return null;
    }

    private static string BuildReceiverName(Customer customer)
    {
        if (string.Equals(customer.PersonType, "PJ", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            return customer.CompanyName;
        }

        var parts = new[] { customer.FirstName, customer.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var fullName = string.Join(' ', parts).Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return customer.CompanyName ?? string.Empty;
    }

    private static string ResolveReceiverNameWithFallback(AchTransaction transaction, string? receiverName)
    {
        var normalizedReceiverName = NachaReceiverNameHelper.SanitizeForType6(receiverName);
        if (!string.IsNullOrWhiteSpace(normalizedReceiverName))
        {
            return normalizedReceiverName;
        }

        normalizedReceiverName = NachaReceiverNameHelper.SanitizeForType6(transaction.RecipientIdNumber);
        if (!string.IsNullOrWhiteSpace(normalizedReceiverName))
        {
            return normalizedReceiverName;
        }

        normalizedReceiverName = NachaReceiverNameHelper.SanitizeForType6($"USUARIO {transaction.Id}");
        return normalizedReceiverName;
    }

    private async Task ValidateTransactionsForSendAsync(IEnumerable<AchTransaction> transactions, CancellationToken ct)
    {
        foreach (var tx in transactions)
        {
            if (tx.IsPrenotification && tx.Amount != 0)
            {
                throw new InvalidOperationException($"La prenotificación {tx.Id} debe tener valor 0.");
            }

            if (!tx.IsPrenotification)
            {
                var prenoteDate = await GetPrenoteDateAsync(tx, ct);
                if (prenoteDate is null)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} no tiene prenotificación previa.");
                }

                var minDate = AddBusinessDays(prenoteDate.Value.Date, 3);
                if (tx.EffectiveEntryDate.Date < minDate)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} no cumple los 3 días hábiles desde la prenotificación.");
                }
            }
        }
    }

    private async Task<DateTime?> GetPrenoteDateAsync(AchTransaction tx, CancellationToken ct)
    {
        var prenoteCode = ResolvePrenoteCode(tx.TransactionCode);
        if (string.IsNullOrWhiteSpace(prenoteCode))
        {
            return null;
        }

        return await _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.IsPrenotification
                        && t.DestinationInstitutionId == tx.DestinationInstitutionId
                        && t.DestinationAccountNumber == tx.DestinationAccountNumber
                        && t.TransactionCode == prenoteCode)
            .OrderByDescending(t => t.EffectiveEntryDate)
            .Select(t => (DateTime?)t.EffectiveEntryDate.Date)
            .FirstOrDefaultAsync(ct);
    }

    private static string? ResolvePrenoteCode(string transactionCode)
    {
        return transactionCode switch
        {
            "22" => "23",
            "27" => "28",
            "32" => "33",
            "37" => "38",
            "52" => "53",
            "55" => "57",
            _ => null
        };
    }

    private DateTime AddBusinessDays(DateTime start, int days)
    {
        var date = start;
        var remaining = days;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
            .Select(h => h.Date)
            .ToHashSet();

        while (remaining > 0)
        {
            date = date.AddDays(1);

            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                    .Select(h => h.Date)
                    .ToHashSet();
            }

            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            if (!isWeekend && !isHoliday)
            {
                remaining--;
            }
        }

        return date;
    }

    private sealed record BatchControlRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public int EntryAddendaCount { get; init; }
        public long EntryHash { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CompanyIdentification { get; init; } = string.Empty;
        public string MessageAuthenticationCode { get; init; } = string.Empty;
        public string OriginatingDFI { get; init; } = string.Empty;
        public int BatchNumber { get; init; }

        public static BatchControlRecord From(AchBatch batch, int entryAddendaCount, long batchDebit, long batchCredit, int batchNumber)
        {
            return new BatchControlRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = ComputeEntryHash(batch.Transactions),
                TotalDebitAmount = batchDebit,
                TotalCreditAmount = batchCredit,
                CompanyIdentification = batch.CompanyIdentification,
                MessageAuthenticationCode = string.Empty,
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }
    }

    private sealed record FileControlRecord
    {
        public int BatchCount { get; init; }
        public int BlockCount { get; init; }
        public int EntryAddendaCount { get; init; }
        public long EntryHash { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CycleName { get; init; } = string.Empty;

        public static FileControlRecord From(AchCycle cycle, IEnumerable<AchBatch> batches, int batchCount, int blockCount, int entryAddendaCount, long totalDebit, long totalCredit)
        {
            return new FileControlRecord
            {
                BatchCount = batchCount,
                BlockCount = blockCount,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = ComputeEntryHash(batches.SelectMany(b => b.Transactions)),
                TotalDebitAmount = totalDebit,
                TotalCreditAmount = totalCredit,
                CycleName = cycle.CycleName
            };
        }
    }

    private static long ComputeEntryHash(IEnumerable<AchTransaction> transactions)
    {
        const long maxHash = 10_000_000_000L;
        long hash = 0;

        foreach (var tx in transactions)
        {
            var dfi = new string((tx.ReceivingDFI ?? string.Empty).Where(char.IsDigit).ToArray());
            if (dfi.Length == 0)
            {
                continue;
            }

            var first8 = dfi.Length >= 8 ? dfi[..8] : dfi;
            if (long.TryParse(first8, out var value))
            {
                hash = (hash + value) % maxHash;
            }
        }

        return hash;
    }
}
