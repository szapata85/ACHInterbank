using Cfa.ACHInterbank.Application.ACH.Interfaces;
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

    public NachaFileBuilder(AchDbContext context)
    {
        _context = context;
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

        return await BuildFileAsync(cycle, batches, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO ALTERNATIVO: Generar NACHA-M por ciclo
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileByCycleAsync(int cycleId, CancellationToken ct = default)
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

        return await BuildFileAsync(cycle, cycle.Batches, layoutCache, nachaHeader, ct);
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
        AchCycle cycle,
        IEnumerable<AchBatch> batches,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaHeader? header,
        CancellationToken ct)
    {
        var sb = new StringBuilder(capacity: 10240);
        var orderedBatches = batches.OrderBy(b => b.Id).ToList();

        if (!orderedBatches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var fileHeader = FileHeaderRecord.From(cycle, header);
        sb.Append(await BuildRecordInternalAsync("1", fileHeader, layoutCache["1"]));

        long totalDebit = 0, totalCredit = 0;
        int recordCount = 1, batchCount = 0, entryAddendaCount = 0;

        foreach (var batch in orderedBatches)
        {
            batchCount++;
            var batchHeader = BatchHeaderRecord.From(batch);
            sb.Append(await BuildRecordInternalAsync("5", batchHeader, layoutCache["5"]));
            recordCount++;

            long batchDebit = 0, batchCredit = 0;
            int batchEntries = 0;

            foreach (var tx in batch.Transactions.OrderBy(t => t.Id))
            {
                var entry = EntryDetailRecord.From(tx);
                sb.Append(await BuildRecordInternalAsync("6", entry, layoutCache["6"]));
                recordCount++;
                batchEntries++;

                if (tx.Type == TransactionTypeEnum.Debit)
                    batchDebit += (long)(tx.Amount * 100);
                else
                    batchCredit += (long)(tx.Amount * 100);

                var addendasOrdered = tx.Addendas?
                    .OrderBy(a => a.SequenceNumber)
                    ?? Enumerable.Empty<AchTransactionAddenda>();

                foreach (var addenda in addendasOrdered)
                {
                    var addendaRecord = AddendaRecord.From(addenda, tx.TraceSequenceNumber);
                    sb.Append(await BuildRecordInternalAsync("7", addendaRecord, layoutCache["7"]));
                    recordCount++;
                    batchEntries++;
                }
            }

            var batchControl = BatchControlRecord.From(batch, batchEntries, batchDebit, batchCredit);
            sb.Append(await BuildRecordInternalAsync("8", batchControl, layoutCache["8"]));
            recordCount++;

            totalDebit += batchDebit;
            totalCredit += batchCredit;
            entryAddendaCount += batchEntries;
        }

        int totalRecords = recordCount + 1; // incluye el control de archivo
        int blockCount = (int)Math.Ceiling(totalRecords / 10m);
        var fileControl = FileControlRecord.From(cycle, batchCount, blockCount, entryAddendaCount, totalDebit, totalCredit);

        sb.Append(await BuildRecordInternalAsync("9", fileControl, layoutCache["9"]));

        return sb.ToString();
    }

    private async Task<NachaHeader?> LoadHeaderAsync(int cycleId, CancellationToken ct)
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
