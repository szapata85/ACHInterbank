using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchReturnsService(AchDbContext context) : IAchReturnsService
{
    private const int MaxCyclesForReturn = 4;
    private const string ImmediateDestinationAchColombia = "000101006";

    public async Task<IReadOnlyList<ReturnEligibleTransactionDto>> GetTransactionsByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var cycle = await context.AchCycles.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo seleccionado.");

        var transactions = await context.AchTransactions
            .AsNoTracking()
            .Where(t => t.AchCycleId == cycleId)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        var cycleOrder = await GetCycleOrderAsync(cycle.ClearingHouseId, ct);
        cycleOrder.TryGetValue(cycle.Id, out var selectedCycleOrder);

        var alreadyReturned = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Select(r => r.OriginalTransactionId)
            .ToHashSetAsync(ct);

        return transactions.Select(tx =>
        {
            var isEligible = true;
            string? message = null;

            if (alreadyReturned.Contains(tx.Id))
            {
                isEligible = false;
                message = "La transacción ya tiene devolución generada.";
            }

            if (!cycleOrder.TryGetValue(tx.AchCycleId, out var txCycleOrder))
            {
                isEligible = false;
                message = "No fue posible validar la antigüedad por ciclo.";
            }
            else if ((selectedCycleOrder - txCycleOrder) > MaxCyclesForReturn)
            {
                isEligible = false;
                message = "La transacción supera el máximo de 4 ciclos para devolución.";
            }

            return new ReturnEligibleTransactionDto(
                tx.Id,
                tx.TraceNumber,
                tx.Amount,
                tx.TransactionCode,
                tx.Reference,
                tx.SourceAccountNumber,
                tx.DestinationAccountNumber,
                tx.OriginatingDFI,
                tx.ReceivingDFI,
                tx.AchCycleId,
                tx.EffectiveEntryDate,
                tx.IsPrenotification,
                isEligible,
                message);
        }).ToList();
    }

    public async Task<GenerateReturnsFileResponse> GenerateReturnsFileAsync(GenerateReturnsFileRequest request, CancellationToken ct = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos una transacción para devolver.");
        }

        var cycle = await context.AchCycles
            .Include(c => c.ClearingHouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo de operación.");

        var selectedIds = request.Items.Select(i => i.TransactionId).Distinct().ToList();
        var transactions = await context.AchTransactions
            .Where(t => selectedIds.Contains(t.Id))
            .ToListAsync(ct);

        if (transactions.Count != selectedIds.Count)
        {
            throw new InvalidOperationException("Algunas transacciones seleccionadas no existen.");
        }

        if (transactions.Any(t => t.AchCycleId != request.CycleId))
        {
            throw new InvalidOperationException("No se permite mezclar transacciones de ciclos distintos en el mismo archivo de devolución.");
        }

        var reasonList = await context.ReturnReasons.AsNoTracking().ToListAsync(ct);
        var reasons = reasonList.ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);
        var cycleOrder = await GetCycleOrderAsync(cycle.ClearingHouseId, ct);
        cycleOrder.TryGetValue(request.CycleId, out var selectedCycleOrder);

        var now = DateTime.UtcNow;
        var generatedRows = new List<AchReturnGenerated>();
        var entryLines = new List<string>();
        var addendaLines = new List<string>();

        foreach (var item in request.Items)
        {
            var tx = transactions.First(t => t.Id == item.TransactionId);

            if (!cycleOrder.TryGetValue(tx.AchCycleId, out var txCycleOrder) || (selectedCycleOrder - txCycleOrder) > MaxCyclesForReturn)
            {
                throw new InvalidOperationException($"La transacción {tx.Id} excede la ventana máxima de 4 ciclos para devolución.");
            }

            if (!reasons.TryGetValue(item.ReturnReasonCode, out var reason) || !reason.Code.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"La causal {item.ReturnReasonCode} no es válida para devolución ACH.");
            }

            var amount = tx.IsPrenotification ? 0m : tx.Amount;
            var newSequence = await GenerateNewReturnSequenceAsync(tx.ReceivingDFI, now.Date, ct);
            var originalSequence = NormalizeDigits(tx.TraceNumber, 15);
            var receiverEntity = NormalizeDigits(tx.OriginatingDFI, 8);
            var originEntity = NormalizeDigits(tx.ReceivingDFI, 8);

            entryLines.Add(BuildType6ReturnLine(tx, receiverEntity, amount, newSequence));
            addendaLines.Add(BuildType7ReturnAddendaLine(reason.Code, originalSequence));

            generatedRows.Add(new AchReturnGenerated
            {
                OriginalTransactionId = tx.Id,
                ReturnCycleId = request.CycleId,
                ReturnReasonCode = reason.Code.ToUpperInvariant(),
                Amount = amount,
                NewSequenceNumber = newSequence,
                OriginalSequenceNumber = originalSequence,
                ReceiverEntityCode = receiverEntity,
                OriginatorEntityCode = originEntity,
                FileName = string.Empty,
                GeneratedAtUtc = now
            });
        }

        var totalDebit = generatedRows.Sum(r => r.Amount);
        var totalCredit = 0m;
        var hash = generatedRows.Sum(r => long.Parse(r.ReceiverEntityCode));
        var fileName = $"RET_{request.CycleId}_{now:yyyyMMddHHmmss}.RET";

        var lines = new List<string>
        {
            BuildType1HeaderLine(cycle, now),
            BuildType5HeaderLine(cycle, now),
        };

        for (int i = 0; i < entryLines.Count; i++)
        {
            lines.Add(entryLines[i]);
            lines.Add(addendaLines[i]);
        }

        lines.Add(BuildType8ControlLine(entryLines.Count, addendaLines.Count, hash, totalDebit, totalCredit));
        lines.Add(BuildType9ControlLine(1, lines.Count + 1, entryLines.Count + addendaLines.Count, hash, totalDebit, totalCredit));

        foreach (var row in generatedRows)
        {
            row.FileName = fileName;
        }

        context.Set<AchReturnGenerated>().AddRange(generatedRows);
        await context.SaveChangesAsync(ct);

        var fileContent = string.Join(Environment.NewLine, lines);
        return new GenerateReturnsFileResponse(fileName, "text/plain", Encoding.UTF8.GetBytes(fileContent), lines.Count, generatedRows.Count);
    }

    private async Task<Dictionary<string, int>> GetCycleOrderAsync(int clearingHouseId, CancellationToken ct)
    {
        var cycles = await context.AchCycles
            .AsNoTracking()
            .Where(c => c.ClearingHouseId == clearingHouseId)
            .OrderBy(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .Select(c => c.Id)
            .ToListAsync(ct);

        return cycles.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateNewReturnSequenceAsync(string receivingDfi, DateTime day, CancellationToken ct)
    {
        var entityCode = NormalizeDigits(receivingDfi, 8);

        var last = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Where(r => r.GeneratedAtUtc.Date == day && r.NewSequenceNumber.StartsWith(entityCode))
            .Select(r => r.NewSequenceNumber)
            .ToListAsync(ct);

        var next = last
            .Select(s => int.TryParse(s[^7..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{entityCode}{next:0000000}";
    }

    private static string BuildType1HeaderLine(AchCycle cycle, DateTime nowUtc)
    {
        var originCode = NormalizeDigits(cycle.ClearingHouse?.OriginCode, 9);
        var text = new StringBuilder(106);
        text.Append('1');
        text.Append(PadNum("01", 2));
        text.Append(PadNum(ImmediateDestinationAchColombia, 9));
        text.Append(PadNum(originCode, 9));
        text.Append(nowUtc.ToString("yyMMdd"));
        text.Append(nowUtc.ToString("HHmm"));
        text.Append('A');
        text.Append("09410");
        text.Append(PadAlpha("ACH-RET", 23));
        text.Append(PadAlpha(cycle.ClearingHouse?.Name ?? "BANCO ORIGEN", 23));
        text.Append(PadAlpha("RET", 30));
        return Ensure106(text.ToString());
    }

    private static string BuildType5HeaderLine(AchCycle cycle, DateTime nowUtc)
    {
        var text = new StringBuilder(106);
        text.Append('5');
        text.Append("200");
        text.Append(PadAlpha("DEVOLUCIONES", 16));
        text.Append(PadAlpha(string.Empty, 20));
        text.Append(PadAlpha("BANCORET", 10));
        text.Append("PPD");
        text.Append(PadAlpha("RETORNO", 10));
        text.Append(nowUtc.ToString("yyyyMMdd"));
        text.Append(nowUtc.ToString("yyyyMMdd"));
        text.Append("000");
        text.Append('1');
        text.Append(PadNum(NormalizeDigits(cycle.ClearingHouse?.OriginCode, 8), 8));
        text.Append("0000001");
        return Ensure106(text.ToString());
    }

    private static string BuildType6ReturnLine(AchTransaction tx, string receiverEntityCode, decimal amount, string newSequence)
    {
        var text = new StringBuilder(106);
        text.Append('6');
        text.Append(PadNum(tx.TransactionCode, 2));
        text.Append(PadNum(receiverEntityCode, 8));
        text.Append('0');
        text.Append(PadAlpha(tx.SourceAccountNumber, 17));
        text.Append(PadNum(((long)(amount * 100)).ToString(), 18));
        text.Append(PadAlpha(tx.CompanyIdentification, 15));
        text.Append(PadAlpha(tx.CompanyName, 22));
        text.Append(PadAlpha("R", 2));
        text.Append('1');
        text.Append(PadNum(newSequence, 15));
        return Ensure106(text.ToString());
    }

    private static string BuildType7ReturnAddendaLine(string reasonCode, string originalSequence)
    {
        var text = new StringBuilder(106);
        text.Append('7');
        text.Append("99");
        text.Append(PadAlpha($"{reasonCode} ORIGSEQ:{originalSequence}", 80));
        text.Append(PadNum("1", 4));
        text.Append(PadNum(originalSequence[^7..], 7));
        text.Append(PadAlpha(string.Empty, 12));
        return Ensure106(text.ToString());
    }

    private static string BuildType8ControlLine(int entryCount, int addendaCount, long hash, decimal totalDebit, decimal totalCredit)
    {
        var text = new StringBuilder(106);
        text.Append('8');
        text.Append("200");
        text.Append(PadNum((entryCount + addendaCount).ToString(), 6));
        text.Append(PadNum((hash % 10_000_000_000L).ToString(), 10));
        text.Append(PadNum(((long)(totalDebit * 100)).ToString(), 18));
        text.Append(PadNum(((long)(totalCredit * 100)).ToString(), 18));
        text.Append(PadAlpha("BANCORET", 25));
        text.Append(PadNum("0000001", 7));
        text.Append(PadAlpha(string.Empty, 19));
        return Ensure106(text.ToString());
    }

    private static string BuildType9ControlLine(int batchCount, int totalRecords, int entryAddendaCount, long hash, decimal totalDebit, decimal totalCredit)
    {
        var blockCount = (int)Math.Ceiling(totalRecords / 10m);
        var text = new StringBuilder(106);
        text.Append('9');
        text.Append(PadNum(batchCount.ToString(), 6));
        text.Append(PadNum(blockCount.ToString(), 6));
        text.Append(PadNum(entryAddendaCount.ToString(), 8));
        text.Append(PadNum((hash % 10_000_000_000L).ToString(), 10));
        text.Append(PadNum(((long)(totalDebit * 100)).ToString(), 18));
        text.Append(PadNum(((long)(totalCredit * 100)).ToString(), 18));
        text.Append(PadAlpha(string.Empty, 40));
        return Ensure106(text.ToString());
    }

    private static string PadAlpha(string? value, int length)
    {
        var val = (value ?? string.Empty).Trim();
        if (val.Length > length)
        {
            val = val[..length];
        }

        return val.PadRight(length, ' ');
    }

    private static string PadNum(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }

    private static string NormalizeDigits(string? value, int length)
    {
        return PadNum(value, length);
    }

    private static string Ensure106(string value)
    {
        if (value.Length > 106)
        {
            return value[..106];
        }

        return value.PadRight(106, ' ');
    }
}
