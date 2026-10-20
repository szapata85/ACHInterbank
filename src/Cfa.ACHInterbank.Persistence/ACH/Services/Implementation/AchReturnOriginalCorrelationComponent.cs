using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnOriginalCorrelationComponent(AchDbContext context)
{
    public async Task<IReadOnlyList<ReturnEligibleTransactionDto>> GetTransactionsByCycleAsync(
        string cycleId,
        CancellationToken ct = default)
    {
        _ = await context.AchCycles.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo seleccionado.");

        var transactions = await context.AchTransactions
            .AsNoTracking()
            .Where(t => t.AchCycleId == cycleId)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

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

    public static void ValidateRequest(GenerateReturnsFileRequest request)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos una transacción para devolver.");
        }

        var duplicateSelections = request.Items
            .GroupBy(item => item.TransactionId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSelections is not null)
        {
            throw new InvalidOperationException($"La transacción {duplicateSelections.Key} está repetida en la solicitud de devolución.");
        }
    }

    public async Task<AchReturnGenerationScope> ResolveScopeAsync(
        GenerateReturnsFileRequest request,
        CancellationToken ct)
    {
        var originalCycle = await context.AchCycles
            .Include(c => c.ClearingHouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo de operación.");

        var returnCycleId = string.IsNullOrWhiteSpace(request.ReturnCycleId)
            ? request.CycleId
            : request.ReturnCycleId.Trim();
        var cycle = string.Equals(returnCycleId, originalCycle.Id, StringComparison.OrdinalIgnoreCase)
            ? originalCycle
            : await context.AchCycles
                .Include(c => c.ClearingHouse)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == returnCycleId, ct)
                ?? throw new InvalidOperationException("No se encontró el ciclo de devolución.");

        if (cycle.ClearingHouseId != originalCycle.ClearingHouseId)
        {
            throw new InvalidOperationException("No se permite generar una devolución en una cámara distinta de la operación original.");
        }

        var paymentRailCode = cycle.ClearingHouseId is int clearingHouseId
            ? await context.ClearingHouseConfigs
                .AsNoTracking()
                .Where(config => config.ClearingHouseId == clearingHouseId)
                .Select(config => config.PaymentRailCode)
                .FirstOrDefaultAsync(ct)
            : null;

        var isAchColombia = IsAchColombia(cycle.ClearingHouse?.Code);
        var isCenit = IsCenit(cycle.ClearingHouse?.Code);
        if (!isAchColombia && !isCenit)
        {
            throw new InvalidOperationException("RETURN_OUT_CLEARING_HOUSE_NOT_SUPPORTED: la cámara no dispone de un perfil Return Out implementado.");
        }

        return new AchReturnGenerationScope(originalCycle, cycle, paymentRailCode, isAchColombia, isCenit);
    }

    public async Task<AchReturnCorrelationResult> LoadAsync(
        AchReturnGenerationScope scope,
        GenerateReturnsFileRequest request,
        CancellationToken ct)
    {
        var selectedIds = request.Items.Select(i => i.TransactionId).Distinct().ToList();
        var transactions = await context.AchTransactions
            .Include(t => t.AchCycle)
                .ThenInclude(cycle => cycle!.ClearingHouseCycleConfig)
            .Where(t => selectedIds.Contains(t.Id))
            .ToListAsync(ct);

        if (transactions.Count != selectedIds.Count)
        {
            throw new InvalidOperationException("Algunas transacciones seleccionadas no existen.");
        }

        var isAchColombiaR10Batch = scope.IsAchColombia
            && request.Items.All(item => string.Equals(item.ReturnReasonCode?.Trim(), "R10", StringComparison.OrdinalIgnoreCase));
        if (transactions.Any(transaction => transaction.AchCycle?.ClearingHouseId != scope.ReturnCycle.ClearingHouseId)
            || (!isAchColombiaR10Batch && transactions.Any(transaction => transaction.AchCycleId != request.CycleId)))
        {
            throw new InvalidOperationException("No se permite mezclar transacciones de ciclos distintos en el mismo archivo de devolución.");
        }

        var cenitCycleEvidence = scope.IsCenit
            ? await ResolveCenitCycleEvidenceAsync(scope.OriginalCycle, scope.ReturnCycle, ct)
            : null;
        var cenitSources = scope.IsCenit
            ? await LoadCenitOriginalSourcesAsync(selectedIds, ct)
            : new Dictionary<int, AchReturnCenitOriginalSource>();

        var alreadyReturnedTransactions = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Where(r => selectedIds.Contains(r.OriginalTransactionId))
            .Select(r => r.OriginalTransactionId)
            .ToHashSetAsync(ct);
        if (alreadyReturnedTransactions.Count > 0)
        {
            throw new AchReturnAlreadyGeneratedException(alreadyReturnedTransactions);
        }

        return new AchReturnCorrelationResult(
            scope,
            selectedIds,
            transactions.ToDictionary(tx => tx.Id),
            cenitSources,
            cenitCycleEvidence);
    }

    private async Task<Dictionary<int, AchReturnCenitOriginalSource>> LoadCenitOriginalSourcesAsync(
        IReadOnlyCollection<int> transactionIds,
        CancellationToken ct)
    {
        var rows = await context.IncomingNachaTransactionLinks
            .AsNoTracking()
            .Where(link => link.IsFinal
                && link.AchTransactionId.HasValue
                && transactionIds.Contains(link.AchTransactionId.Value)
                && link.EntryDetail != null
                && link.EntryDetail.NachaHeader != null
                && link.EntryDetail.BatchHeader != null)
            .Select(link => new
            {
                TransactionId = link.AchTransactionId!.Value,
                StandardEntryClassCode = link.EntryDetail!.BatchHeader!.StandardEntryClassCode,
                ImmediateDestination = link.EntryDetail.NachaHeader!.ImmediateDestination,
                ImmediateOrigin = link.EntryDetail.NachaHeader.ImmediateOrigin,
                ImmediateDestinationName = link.EntryDetail.NachaHeader.ImmediateDestinationName,
                ImmediateOriginName = link.EntryDetail.NachaHeader.ImmediateOriginName
            })
            .ToListAsync(ct);

        var result = new Dictionary<int, AchReturnCenitOriginalSource>();
        foreach (var transactionId in transactionIds)
        {
            var candidates = rows
                .Where(row => row.TransactionId == transactionId)
                .Select(row => new AchReturnCenitOriginalSource(
                    NormalizeCenitSec(row.StandardEntryClassCode),
                    NormalizeDigits(row.ImmediateDestination, 10),
                    NormalizeDigits(row.ImmediateOrigin, 10),
                    (row.ImmediateDestinationName ?? string.Empty).Trim(),
                    (row.ImmediateOriginName ?? string.Empty).Trim()))
                .Distinct()
                .ToList();
            if (candidates.Count != 1)
            {
                throw new InvalidOperationException($"CENIT_RETURN_ORIGINAL_RAW_EVIDENCE_REQUIRED: la transacción {transactionId} no tiene un único registro 1/5/6 original trazable.");
            }

            result[transactionId] = candidates[0];
        }

        return result;
    }

    private async Task<AchReturnCenitCycleEvidence> ResolveCenitCycleEvidenceAsync(
        AchCycle originalCycle,
        AchCycle returnCycle,
        CancellationToken ct)
    {
        if (!ExternalFileNameSupport.TryExtractPositiveCycleNumber(originalCycle.CycleName, out var originalCycleNumber)
            || !ExternalFileNameSupport.TryExtractPositiveCycleNumber(returnCycle.CycleName, out var returnCycleNumber))
        {
            throw new InvalidOperationException("CENIT_RETURN_CYCLE_EVIDENCE_REQUIRED: los ciclos CENIT no tienen numeración normalizada.");
        }

        var cycleNames = await context.AchCycles
            .AsNoTracking()
            .Where(item => item.ClearingHouseId == returnCycle.ClearingHouseId
                && item.ProcessingDate.Date == returnCycle.ProcessingDate.Date)
            .Select(item => item.CycleName)
            .ToListAsync(ct);
        var lastReturnCycleNumber = cycleNames
            .Select(name => ExternalFileNameSupport.TryExtractPositiveCycleNumber(name, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        if (lastReturnCycleNumber <= 0)
        {
            throw new InvalidOperationException("CENIT_RETURN_CYCLE_EVIDENCE_REQUIRED: no existe último ciclo CENIT normalizado para la fecha operacional.");
        }

        return new AchReturnCenitCycleEvidence(originalCycleNumber, returnCycleNumber, lastReturnCycleNumber);
    }

    private static bool IsAchColombia(string? clearingHouseCode)
        => string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
           || string.Equals(clearingHouseCode, "ACHCOL", StringComparison.OrdinalIgnoreCase)
           || string.Equals(clearingHouseCode, "ACHCOLOMBIA", StringComparison.OrdinalIgnoreCase);

    private static bool IsCenit(string? clearingHouseCode)
        => string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCenitSec(string? value)
    {
        var sec = (value ?? string.Empty).Trim().ToUpperInvariant();
        return sec switch
        {
            "PPD" or "CCD" => sec,
            "CTX" => throw new InvalidOperationException(CenitReturnIn2026Layout.CtxScopeStatus),
            _ => throw new InvalidOperationException($"CENIT_RETURN_SEC_NOT_SUPPORTED: SEC {sec} no está soportado por Return Out CENIT.")
        };
    }

    private static string NormalizeDigits(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }
}
