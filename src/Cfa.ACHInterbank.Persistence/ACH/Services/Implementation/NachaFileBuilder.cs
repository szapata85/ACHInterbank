using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Nacha.Dtos;
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

        var sb = new StringBuilder(capacity: 10240);
        var cycle = batches.First().AchCycle!;

        var layoutCache = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .ToDictionaryAsync(l => l.RecordCode!, ct);

        // 1️⃣ Encabezado de archivo (registro tipo 1)
        sb.Append(await BuildRecordInternalAsync("1", cycle, layoutCache["1"]));

        long totalDebit = 0, totalCredit = 0;
        int recordCount = 1, batchCount = 0, entryAddendaCount = 0;

        // 2️⃣ Procesar lotes
        foreach (var batch in batches.OrderBy(b => b.Id))
        {
            batchCount++;
            sb.Append(await BuildRecordInternalAsync("5", batch, layoutCache["5"]));
            recordCount++;

            long batchDebit = 0, batchCredit = 0;
            int batchEntries = 0;

            // 3️⃣ Transacciones
            foreach (var tx in batch.Transactions.OrderBy(t => t.Id))
            {
                sb.Append(await BuildRecordInternalAsync("6", tx, layoutCache["6"]));
                recordCount++;
                batchEntries++;

                if (tx.Type == TransactionTypeEnum.Debit)
                    batchDebit += (long)(tx.Amount * 100);
                else
                    batchCredit += (long)(tx.Amount * 100);

                // 4️⃣ Addendas
                var addendasOrdered = tx.Addendas?
                    .OrderBy(a => a.SequenceNumber)
                    ?? Enumerable.Empty<AchTransactionAddenda>();

                foreach (var addenda in addendasOrdered)
                {
                    sb.Append(await BuildRecordInternalAsync("7", addenda, layoutCache["7"]));
                    recordCount++;
                    batchEntries++;
                }
            }

            // 5️⃣ Control de lote (tipo 8)
            sb.Append(await BuildRecordInternalAsync("8", batch, layoutCache["8"]));
            recordCount++;

            totalDebit += batchDebit;
            totalCredit += batchCredit;
            entryAddendaCount += batchEntries;
        }

        // 6️⃣ Control de archivo (tipo 9)
        sb.Append(await BuildRecordInternalAsync("9", cycle, layoutCache["9"]));

        return sb.ToString();
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

        var sb = new StringBuilder(capacity: 10240);
        var layoutCache = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .ToDictionaryAsync(l => l.RecordCode!, ct);

        sb.Append(await BuildRecordInternalAsync("1", cycle, layoutCache["1"]));

        foreach (var batch in cycle.Batches.OrderBy(b => b.Id))
        {
            sb.Append(await BuildRecordInternalAsync("5", batch, layoutCache["5"]));

            foreach (var tx in batch.Transactions.OrderBy(t => t.Id))
            {
                sb.Append(await BuildRecordInternalAsync("6", tx, layoutCache["6"]));

                var addendasOrdered = tx.Addendas?
                    .OrderBy(a => a.SequenceNumber)
                    ?? Enumerable.Empty<AchTransactionAddenda>();

                foreach (var addenda in addendasOrdered)
                    sb.Append(await BuildRecordInternalAsync("7", addenda, layoutCache["7"]));
            }

            sb.Append(await BuildRecordInternalAsync("8", batch, layoutCache["8"]));
        }

        sb.Append(await BuildRecordInternalAsync("9", cycle, layoutCache["9"]));
        return sb.ToString();
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
}
