using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
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

    public NachaFileBuilder(
        AchDbContext context,
        IBankHoliday holidayService,
        INachaRecordDataProvider recordDataProvider)
    {
        _context = context;
        _holidayService = holidayService;
        _recordDataProvider = recordDataProvider;
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
            .Include(c => c.Batches)
                .ThenInclude(b => b.Transactions)
                    .ThenInclude(t => t.Addendas)
            .FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException($"No existe el ciclo {cycleId}.");

        var nachaHeader = await LoadHeaderAsync(cycle.Id, ct);
        var layoutCache = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .ToDictionaryAsync(l => l.RecordCode!, ct);

        var transactions = cycle.Batches.SelectMany(b => b.Transactions).ToList();
        await ValidateTransactionsForSendAsync(transactions, ct);
        var definitions = await LoadDefinitionsAsync(ct);
        var context = new NachaBuildContext
        {
            Cycle = cycle,
            Batches = cycle.Batches.ToList(),
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

        foreach (var field in fields)
        {
            var prop = typeof(T).GetProperty(field.DbColumn);
            if (prop == null) continue;

            object? raw = prop.GetValue(entity);
            string value = FormatValue(raw, field);

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
            if (!values.TryGetValue(field.DbColumn, out var raw))
            {
                continue;
            }

            var value = FormatValue(raw, field);

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

        if (!orderedBatches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        long totalDebit = 0, totalCredit = 0;
        int recordCount = 0, batchCount = orderedBatches.Count, entryAddendaCount = 0;

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
                        new[] { FileHeaderRecord.From(context.Cycle, header) },
                        context,
                        ct);
                    break;
                case "5":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        orderedBatches.Select(BatchHeaderRecord.From),
                        context,
                        ct);
                    break;
                case "6":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        context.Transactions.OrderBy(t => t.Id).Select(EntryDetailRecord.From),
                        context,
                        ct);
                    entryAddendaCount += context.Transactions.Count;
                    foreach (var tx in context.Transactions)
                    {
                        if (tx.Type == TransactionTypeEnum.Debit)
                            totalDebit += (long)(tx.Amount * 100);
                        else
                            totalCredit += (long)(tx.Amount * 100);
                    }
                    break;
                case "7":
                    recordCount += await AppendCustomOrConfiguredAsync(
                        sb,
                        definition,
                        layoutCache,
                        BuildAddendaRecords(context.Transactions),
                        context,
                        ct);
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
                                SumBatchCredit(batch))),
                        context,
                        ct);
                    break;
                case "9":
                    var totalRecords = recordCount + 1;
                    var blockCount = (int)Math.Ceiling(totalRecords / 10m);
                    var paddingNeeded = (blockCount * 10) - totalRecords;
                    var fileControl = FileControlRecord.From(context.Cycle, batchCount, blockCount, entryAddendaCount, totalDebit, totalCredit);

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

        return sb.ToString();
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
        IReadOnlyList<object> records = definition.SourceType == NachaRecordSourceType.Custom
            ? fallbackRecords.ToList()
            : await _recordDataProvider.GetRecordsAsync(definition, context, ct);

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

    private static IEnumerable<AddendaRecord> BuildAddendaRecords(IEnumerable<AchTransaction> transactions)
    {
        foreach (var tx in transactions)
        {
            foreach (var addenda in BuildAddendasForTransaction(tx))
            {
                yield return AddendaRecord.From(addenda, tx.TraceSequenceNumber);
            }
        }
    }

    private static long SumBatchDebit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type == TransactionTypeEnum.Debit)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private static long SumBatchCredit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type != TransactionTypeEnum.Debit)
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
                Information = string.IsNullOrWhiteSpace(tx.Reference) ? "PAGO" : tx.Reference,
                SequenceNumber = 1
            }
        };
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
        public string? ReferenceCode { get; init; }
        public string? CycleName { get; init; }
        public DateTime ProcessingDate { get; init; }

        public static FileHeaderRecord From(AchCycle cycle, NachaHeader? header)
        {
            var now = DateTime.UtcNow;

            return new FileHeaderRecord
            {
                PriorityCode = header?.PriorityCode,
                ImmediateDestination = header?.ImmediateDestination,
                ImmediateOrigin = header?.ImmediateOrigin,
                FileCreationDate = ParseDate(header?.FileCreationDate) ?? now,
                FileCreationTime = ParseTime(header?.FileCreationTime) ?? now,
                FileIdModifier = header?.FileIdModifier,
                ReferenceCode = header?.ReferenceCode,
                CycleName = cycle.CycleName,
                ProcessingDate = cycle.ProcessingDate
            };
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

    private sealed record BatchHeaderRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;
        public string CompanyEntryDescription { get; init; } = string.Empty;
        public DateTime EffectiveEntryDate { get; init; }
        public string OriginatingDFI { get; init; } = string.Empty;

        public static BatchHeaderRecord From(AchBatch batch)
        {
            return new BatchHeaderRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                CompanyName = batch.CompanyName,
                CompanyIdentification = batch.CompanyIdentification,
                CompanyEntryDescription = batch.CompanyEntryDescription,
                EffectiveEntryDate = batch.EffectiveEntryDate,
                OriginatingDFI = batch.OriginOrOdfi
            };
        }
    }

    private sealed record EntryDetailRecord
    {
        public string TransactionCode { get; init; } = string.Empty;
        public string ReceivingDFI { get; init; } = string.Empty;
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Reference { get; init; } = string.Empty;
        public string RecipientIdNumber { get; init; } = string.Empty;
        public string DiscretionaryData { get; init; } = string.Empty;
        public string AddendumIndicator { get; init; } = "1";
        public string TraceNumber { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;

        public static EntryDetailRecord From(AchTransaction tx)
        {
            return new EntryDetailRecord
            {
                TransactionCode = tx.TransactionCode,
                ReceivingDFI = tx.ReceivingDFI,
                DestinationAccountNumber = tx.DestinationAccountNumber,
                Amount = tx.Amount,
                Reference = tx.Reference,
                RecipientIdNumber = tx.RecipientIdNumber,
                DiscretionaryData = tx.DiscretionaryData,
                AddendumIndicator = "1",
                TraceNumber = tx.TraceNumber,
                CompanyIdentification = tx.CompanyIdentification
            };
        }
    }

    private sealed record AddendaRecord
    {
        public string AddendaType { get; init; } = string.Empty;
        public string Information { get; init; } = string.Empty;
        public int SequenceNumber { get; init; }
        public int EntryDetailSequenceNumber { get; init; }

        public static AddendaRecord From(AchTransactionAddenda addenda, int entrySequence)
        {
            return new AddendaRecord
            {
                AddendaType = addenda.AddendaType,
                Information = addenda.Information,
                SequenceNumber = addenda.SequenceNumber ?? 0,
                EntryDetailSequenceNumber = entrySequence
            };
        }
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
        public string CompanyIdentification { get; init; } = string.Empty;
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CompanyName { get; init; } = string.Empty;

        public static BatchControlRecord From(AchBatch batch, int entryAddendaCount, long batchDebit, long batchCredit)
        {
            return new BatchControlRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                EntryAddendaCount = entryAddendaCount,
                CompanyIdentification = batch.CompanyIdentification,
                TotalDebitAmount = batchDebit,
                TotalCreditAmount = batchCredit,
                CompanyName = batch.CompanyName
            };
        }
    }

    private sealed record FileControlRecord
    {
        public int BatchCount { get; init; }
        public int BlockCount { get; init; }
        public int EntryAddendaCount { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CycleName { get; init; } = string.Empty;

        public static FileControlRecord From(AchCycle cycle, int batchCount, int blockCount, int entryAddendaCount, long totalDebit, long totalCredit)
        {
            return new FileControlRecord
            {
                BatchCount = batchCount,
                BlockCount = blockCount,
                EntryAddendaCount = entryAddendaCount,
                TotalDebitAmount = totalDebit,
                TotalCreditAmount = totalCredit,
                CycleName = cycle.CycleName
            };
        }
    }
}
