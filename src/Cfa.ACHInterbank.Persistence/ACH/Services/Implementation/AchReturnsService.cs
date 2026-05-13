using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchReturnsService(
    AchDbContext context,
    TimeProvider? timeProvider = null,
    IAchRegulatoryCatalogService? regulatoryCatalogService = null,
    IPaymentRailContextService? paymentRailContextService = null,
    IPaymentRailOperationalStrategyResolver? strategyResolver = null,
    IPaymentRailShadowCompareService? shadowCompareService = null,
    ILogger<AchReturnsService>? logger = null) : IAchReturnsService
{
    private readonly IAchRegulatoryCatalogService _regulatoryCatalogService = regulatoryCatalogService
                                                                           ?? throw new InvalidOperationException("IAchRegulatoryCatalogService es requerido para gobernanza regulatoria de devoluciones.");
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly IPaymentRailContextService? _paymentRailContextService = paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver = strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService = shadowCompareService;
    private readonly ILogger<AchReturnsService> _logger = logger ?? NullLogger<AchReturnsService>.Instance;
    private const int MaxCyclesForReturn = 4;
    private const string ImmediateDestinationAchColombia = "000101006";
    private const string ReturnOriginatorId = "BANCORET";
    private const string ReturnBatchNumber = "0000001";
    private static readonly HashSet<string> CreditTransactionCodes = new(StringComparer.Ordinal)
    {
        "21", "22", "23", "31", "32", "33", "42", "51", "52", "53"
    };
    private static readonly HashSet<string> DebitTransactionCodes = new(StringComparer.Ordinal)
    {
        "26", "27", "28", "36", "37", "38", "55", "56", "57"
    };
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

        var duplicateSelections = request.Items
            .GroupBy(item => item.TransactionId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSelections is not null)
        {
            throw new InvalidOperationException($"La transacción {duplicateSelections.Key} fue seleccionada más de una vez en la misma generación.");
        }

        var cycle = await context.AchCycles
            .Include(c => c.ClearingHouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo de operación.");

        var selectedIds = request.Items.Select(i => i.TransactionId).Distinct().ToList();
        var transactions = await context.AchTransactions
            .Include(t => t.AchCycle)
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

        var cycleOrder = await GetCycleOrderAsync(cycle.ClearingHouseId, ct);
        cycleOrder.TryGetValue(request.CycleId, out var selectedCycleOrder);

        var alreadyReturnedTransactions = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Select(r => r.OriginalTransactionId)
            .ToHashSetAsync(ct);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var generatedRows = new List<AchReturnGenerated>();
        var entryLines = new List<string>();
        var addendaLines = new List<string>();
        var returnEntries = new List<ReturnEntrySnapshot>();

        foreach (var item in request.Items)
        {
            var tx = transactions.First(t => t.Id == item.TransactionId);

            if (alreadyReturnedTransactions.Contains(tx.Id))
            {
                throw new InvalidOperationException($"La transacción {tx.Id} ya cuenta con una devolución registrada.");
            }

            if (tx.Type is Domain.Entities.Transactions.Enums.TransactionTypeEnum.Return or Domain.Entities.Transactions.Enums.TransactionTypeEnum.Reversal)
            {
                throw new InvalidOperationException($"La transacción {tx.Id} no es elegible para devolución porque ya corresponde a un retorno o reverso.");
            }

            if (!cycleOrder.TryGetValue(tx.AchCycleId, out var txCycleOrder) || (selectedCycleOrder - txCycleOrder) > MaxCyclesForReturn)
            {
                throw new InvalidOperationException($"La transacción {tx.Id} excede la ventana máxima de 4 ciclos para devolución.");
            }

            var reasonCode = item.ReturnReasonCode.Trim().ToUpperInvariant();
            await ValidateReturnPolicyAsync(tx, reasonCode, now, ct);

            var amount = tx.IsPrenotification ? 0m : tx.Amount;
            var newSequence = await GenerateNewReturnSequenceAsync(tx.ReceivingDFI, now.Date, ct);
            var originalSequence = NormalizeDigits(tx.TraceNumber, 15);
            var receiverEntity = NormalizeDigits(tx.OriginatingDFI, 8);
            var originEntity = NormalizeDigits(tx.ReceivingDFI, 8);

            entryLines.Add(BuildType6ReturnLine(tx, receiverEntity, amount, newSequence));
            addendaLines.Add(BuildType7ReturnAddendaLine(reasonCode, originalSequence, newSequence, newSequence[^7..]));
            returnEntries.Add(new ReturnEntrySnapshot(tx.TransactionCode, amount, receiverEntity));

            generatedRows.Add(new AchReturnGenerated
            {
                OriginalTransactionId = tx.Id,
                ReturnCycleId = request.CycleId,
                ReturnReasonCode = reasonCode,
                Amount = amount,
                NewSequenceNumber = newSequence,
                OriginalSequenceNumber = originalSequence,
                ReceiverEntityCode = receiverEntity,
                OriginatorEntityCode = originEntity,
                FileName = string.Empty,
                GeneratedAtUtc = now
            });

            CompareReturnShadow(
                cycle.ClearingHouseId,
                cycle.ClearingHouse?.Code,
                request.CycleId,
                tx.EffectiveEntryDate.Date,
                $"RETURN_GENERATED:{reasonCode}",
                legacyOperationSucceeded: true);
        }

        var serviceClassCode = ResolveServiceClassCode(returnEntries);
        var totalDebit = returnEntries
            .Where(entry => DebitTransactionCodes.Contains(entry.TransactionCode))
            .Sum(entry => entry.Amount);
        var totalCredit = returnEntries
            .Where(entry => CreditTransactionCodes.Contains(entry.TransactionCode))
            .Sum(entry => entry.Amount);
        var hash = ComputeEntryHash(returnEntries.Select(entry => entry.ReceiverEntityCode));
        var fileName = $"RET_{request.CycleId}_{now:yyyyMMddHHmmss}.RET";
        var originatingDfi = NormalizeDigits(cycle.ClearingHouse?.OriginCode, 8);

        var lines = new List<string>
        {
            BuildType1HeaderLine(cycle, now),
            BuildType5HeaderLine(now, serviceClassCode, ReturnOriginatorId, ReturnBatchNumber, originatingDfi),
        };

        for (int i = 0; i < entryLines.Count; i++)
        {
            lines.Add(entryLines[i]);
            lines.Add(addendaLines[i]);
        }

        var batchControlLine = BuildType8ControlLine(
            entryLines.Count,
            addendaLines.Count,
            hash,
            totalDebit,
            totalCredit,
            serviceClassCode,
            ReturnOriginatorId,
            originatingDfi,
            ReturnBatchNumber);

        ValidateGeneratedBatchControl(
            lines[1],
            batchControlLine,
            entryLines.Count,
            addendaLines.Count,
            hash,
            totalDebit,
            totalCredit,
            serviceClassCode,
            ReturnOriginatorId,
            originatingDfi,
            ReturnBatchNumber);

        lines.Add(batchControlLine);

        var totalRecordsWithFileControl = lines.Count + 1;
        var blockCount = (int)Math.Ceiling(totalRecordsWithFileControl / 10m);
        var paddingNeeded = (blockCount * 10) - totalRecordsWithFileControl;

        var fileControlLine = BuildType9ControlLine(1, totalRecordsWithFileControl, entryLines.Count + addendaLines.Count, hash, totalDebit, totalCredit);
        ValidateGeneratedFileControl(
            fileControlLine,
            batchControlLine,
            1,
            totalRecordsWithFileControl + paddingNeeded,
            entryLines.Count + addendaLines.Count,
            hash,
            totalDebit,
            totalCredit);

        lines.Add(fileControlLine);

        if (paddingNeeded > 0)
        {
            var paddingRecord = new string('9', 106);
            for (int i = 0; i < paddingNeeded; i++)
            {
                lines.Add(paddingRecord);
            }
        }

        foreach (var row in generatedRows)
        {
            row.FileName = fileName;
        }

        context.Set<AchReturnGenerated>().AddRange(generatedRows);
        await context.SaveChangesAsync(ct);

        var fileContent = string.Concat(lines);
        return new GenerateReturnsFileResponse(fileName, "text/plain", Encoding.UTF8.GetBytes(fileContent), lines.Count, generatedRows.Count);
    }

    private void CompareReturnShadow(
        int? clearingHouseId,
        string? clearingHouseCode,
        string? achCycleId,
        DateTime? operationalDate,
        string legacyDecisionCode,
        bool legacyOperationSucceeded)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        try
        {
            var context = _paymentRailContextService.ResolveContext(clearingHouseId, clearingHouseCode, achCycleId, operationalDate);
            var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(clearingHouseId, clearingHouseCode, achCycleId));
            var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
                context.OperationalContext,
                PaymentRailCapabilityKind.Return,
                legacyDecisionCode));
            var shadowResult = _shadowCompareService.CompareReturnOperation(
                context,
                wrapperResult,
                legacyDecisionCode,
                legacyOperationSucceeded);

            _logger.LogInformation(
                "PAYMENT_RAIL_SHADOW_COMPARE_RETURN|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
                shadowResult.RailCode,
                shadowResult.LegacyDecisionCode,
                shadowResult.WrapperDecisionCode,
                shadowResult.IsEquivalent,
                shadowResult.ComparisonCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PAYMENT_RAIL_SHADOW_COMPARE_RETURN_FAILED");
        }
    }


    private async Task ValidateReturnPolicyAsync(AchTransaction transaction, string reasonCode, DateTime nowUtc, CancellationToken ct)
    {
        var returnCodeValidation = await _regulatoryCatalogService.ValidateReturnCodeAsync(
            transaction.AchCycle.ClearingHouseId,
            reasonCode,
            transaction.Type,
            transaction.EffectiveEntryDate.Date,
            nowUtc.Date,
            ct);
        if (!returnCodeValidation.IsAllowed)
        {
            throw new InvalidOperationException(returnCodeValidation.Reason
                                                ?? $"La causal {reasonCode} no está permitida para la transacción {transaction.Id}.");
        }

        var returnPolicyValidation = await _regulatoryCatalogService.ValidateReturnPolicyAsync(
            transaction.AchCycle.ClearingHouseId,
            transaction.Type,
            reasonCode,
            transaction.EffectiveEntryDate.Date,
            nowUtc.Date,
            hasAddenda: true,
            transaction.State.ToString(),
            ct);
        if (!returnPolicyValidation.IsAllowed)
        {
            throw new InvalidOperationException(returnPolicyValidation.Reason
                                                ?? $"La política regulatoria no permite devolver la transacción {transaction.Id}.");
        }
    }

    private async Task<Dictionary<string, int>> GetCycleOrderAsync(int clearingHouseId, CancellationToken ct)
    {
        var buffered = await context.AchCycles
            .AsNoTracking()
            .Where(c => c.ClearingHouseId == clearingHouseId)
            .OrderBy(c => c.ProcessingDate)
            .ToListAsync(ct);

        var cycles = buffered
            .OrderBy(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .Select(c => c.Id)
            .ToList();

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

    private static string BuildType5HeaderLine(
        DateTime nowUtc,
        string serviceClassCode,
        string companyIdentification,
        string batchNumber,
        string originatingDfi)
    {
        var text = new StringBuilder(106);
        text.Append('5');
        text.Append(PadNum(serviceClassCode, 3));
        text.Append(PadAlpha("DEVOLUCIONES", 16));
        text.Append(PadAlpha(string.Empty, 20));
        text.Append(PadAlpha(companyIdentification, 10));
        text.Append("PPD");
        text.Append(PadAlpha("RETORNO", 10));
        text.Append(nowUtc.ToString("yyyyMMdd"));
        text.Append(nowUtc.ToString("yyyyMMdd"));
        text.Append("000");
        text.Append('1');
        text.Append(PadNum(originatingDfi, 8));
        text.Append(PadNum(batchNumber, 7));
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

    private static string BuildType7ReturnAddendaLine(string reasonCode, string originalTraceNumber, string newTraceNumber, string entryDetailSequenceNumber)
    {
        var buffer = new char[106];
        Array.Fill(buffer, ' ');
        buffer[0] = '7';

        WriteValue(buffer, 2, "99");
        WriteValue(buffer, 4, PadAlpha(reasonCode, 5));
        WriteValue(buffer, 9, PadNum(originalTraceNumber, 15));
        WriteValue(buffer, 82, PadNum(newTraceNumber, 15));
        WriteValue(buffer, 100, PadNum(entryDetailSequenceNumber, 7));
        return new string(buffer);
    }

    private static string BuildType8ControlLine(
        int entryCount,
        int addendaCount,
        long hash,
        decimal totalDebit,
        decimal totalCredit,
        string serviceClassCode,
        string companyIdentification,
        string originatingDfi,
        string batchNumber)
    {
        var text = new StringBuilder(106);
        text.Append('8');
        text.Append(PadNum(serviceClassCode, 3));
        text.Append(PadNum((entryCount + addendaCount).ToString(), 6));
        text.Append(PadNum((hash % 10_000_000_000L).ToString(), 10));
        text.Append(PadNum(((long)(totalDebit * 100)).ToString(), 18));
        text.Append(PadNum(((long)(totalCredit * 100)).ToString(), 18));
        text.Append(PadAlpha(companyIdentification, 10));
        text.Append(PadAlpha(string.Empty, 19));
        text.Append(PadAlpha(string.Empty, 6));
        text.Append(PadNum(originatingDfi, 8));
        text.Append(PadNum(batchNumber, 7));
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

    private static string ResolveServiceClassCode(IEnumerable<ReturnEntrySnapshot> entries)
    {
        var hasCredits = false;
        var hasDebits = false;

        foreach (var entry in entries)
        {
            if (CreditTransactionCodes.Contains(entry.TransactionCode))
            {
                hasCredits = true;
            }
            else if (DebitTransactionCodes.Contains(entry.TransactionCode))
            {
                hasDebits = true;
            }
            else
            {
                throw new InvalidOperationException($"Error Fatal 8: código de transacción no soportado para construir el control de lote RET ({entry.TransactionCode}).");
            }
        }

        if (hasCredits && hasDebits)
        {
            return "200";
        }

        return hasCredits ? "220" : "225";
    }

    private static long ComputeEntryHash(IEnumerable<string> receiverEntityCodes)
    {
        const long maxHash = 10_000_000_000L;
        long hash = 0;

        foreach (var receiverEntityCode in receiverEntityCodes)
        {
            var digits = NormalizeDigits(receiverEntityCode, 8);
            if (long.TryParse(digits, out var value))
            {
                hash = (hash + value) % maxHash;
            }
        }

        return hash;
    }

    private static void ValidateGeneratedBatchControl(
        string batchHeaderLine,
        string batchControlLine,
        int entryCount,
        int addendaCount,
        long entryHash,
        decimal totalDebit,
        decimal totalCredit,
        string serviceClassCode,
        string companyIdentification,
        string originatingDfi,
        string batchNumber)
    {
        if (batchControlLine.Length != 106)
        {
            throw new InvalidOperationException($"Error Fatal 8: el Registro Tipo 8 debe tener exactamente 106 caracteres y se generaron {batchControlLine.Length}.");
        }

        if (batchControlLine.Contains('\r') || batchControlLine.Contains('\n'))
        {
            throw new InvalidOperationException("Error Fatal 8: el Registro Tipo 8 no puede contener caracteres CR/LF.");
        }

        if (!string.Equals(batchControlLine.Substring(1, 3), batchHeaderLine.Substring(1, 3), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: el Código Clase de Transacciones del Registro Tipo 8 no coincide con el Registro Tipo 5.");
        }

        var physicalCount = entryCount + addendaCount;
        if (batchControlLine.Substring(4, 6) != physicalCount.ToString("000000"))
        {
            throw new InvalidOperationException("Error Fatal 51/60: el conteo físico de registros tipo 6 y 7 no coincide con el control del lote.");
        }

        if (batchControlLine.Substring(10, 10) != (entryHash % 10_000_000_000L).ToString("0000000000"))
        {
            throw new InvalidOperationException("Error Fatal 52/61: el Hash Total del lote no coincide con la sumatoria de los registros tipo 6.");
        }

        if (batchControlLine.Substring(20, 18) != ((long)(totalDebit * 100)).ToString("000000000000000000"))
        {
            throw new InvalidOperationException("Error Fatal 63: el total de débitos del lote no coincide con el control de lote.");
        }

        if (batchControlLine.Substring(38, 18) != ((long)(totalCredit * 100)).ToString("000000000000000000"))
        {
            throw new InvalidOperationException("Error Fatal 63: el total de créditos del lote no coincide con el control de lote.");
        }

        if (batchControlLine.Substring(56, 10) != PadAlpha(companyIdentification, 10)
            || batchControlLine.Substring(56, 10) != batchHeaderLine.Substring(40, 10))
        {
            throw new InvalidOperationException("Error Fatal 8: la Identificación del Usuario Originador del Registro Tipo 8 no coincide con el Registro Tipo 5.");
        }

        if (batchControlLine.Substring(85, 6) != "      ")
        {
            throw new InvalidOperationException("Error Fatal 87: el campo reservado del Registro Tipo 8 debe contener únicamente espacios en blanco.");
        }

        if (batchControlLine.Substring(91, 8) != PadNum(originatingDfi, 8)
            || batchControlLine.Substring(91, 8) != batchHeaderLine.Substring(83, 8))
        {
            throw new InvalidOperationException("Error Fatal 8: la Entidad Participante Originadora del Registro Tipo 8 no coincide con el Registro Tipo 5.");
        }

        if (batchControlLine.Substring(99, 7) != PadNum(batchNumber, 7)
            || batchControlLine.Substring(99, 7) != batchHeaderLine.Substring(91, 7))
        {
            throw new InvalidOperationException("Error Fatal D18: el Número de Lote del Registro Tipo 8 no coincide con el Registro Tipo 5.");
        }

        if (batchControlLine.Substring(1, 3) != PadNum(serviceClassCode, 3))
        {
            throw new InvalidOperationException("Error Fatal 8: la clase de servicio generada para el Registro Tipo 8 es inconsistente.");
        }
    }

    private static void ValidateGeneratedFileControl(
        string fileControlLine,
        string batchControlLine,
        int batchCount,
        int totalRecordsIncludingPadding,
        int entryAddendaCount,
        long entryHash,
        decimal totalDebit,
        decimal totalCredit)
    {
        if (fileControlLine.Length != 106)
        {
            throw new InvalidOperationException($"Error Fatal 9: el Registro Tipo 9 debe tener exactamente 106 caracteres y se generaron {fileControlLine.Length}.");
        }

        if (fileControlLine.Contains('\r') || fileControlLine.Contains('\n'))
        {
            throw new InvalidOperationException("Error Fatal 9: el Registro Tipo 9 no puede contener caracteres CR/LF.");
        }

        var expectedBlockCount = totalRecordsIncludingPadding / 10;
        if (fileControlLine.Substring(1, 6) != batchCount.ToString("000000"))
        {
            throw new InvalidOperationException("Error Fatal 58/59: la cantidad total de lotes del Registro Tipo 9 no coincide con el archivo.");
        }

        if (fileControlLine.Substring(7, 6) != expectedBlockCount.ToString("000000"))
        {
            throw new InvalidOperationException("Error Fatal 58/59: el número de bloques físicos del Registro Tipo 9 no coincide con el archivo.");
        }

        if (fileControlLine.Substring(13, 8) != entryAddendaCount.ToString("00000000"))
        {
            throw new InvalidOperationException("Error Fatal 60: el conteo total de registros tipo 6 y 7 no coincide con el Registro Tipo 9.");
        }

        if (fileControlLine.Substring(21, 10) != (entryHash % 10_000_000_000L).ToString("0000000000"))
        {
            throw new InvalidOperationException("Error Fatal 61: el Hash Total del archivo no coincide con el Registro Tipo 9.");
        }

        if (fileControlLine.Substring(31, 18) != ((long)(totalDebit * 100)).ToString("000000000000000000"))
        {
            throw new InvalidOperationException("Error Fatal 62: el total de débitos del archivo no coincide con el Registro Tipo 9.");
        }

        if (fileControlLine.Substring(49, 18) != ((long)(totalCredit * 100)).ToString("000000000000000000"))
        {
            throw new InvalidOperationException("Error Fatal 63: el total de créditos del archivo no coincide con el Registro Tipo 9.");
        }

        if (fileControlLine.Substring(67, 39) != new string(' ', 39))
        {
            throw new InvalidOperationException("Error Fatal 9: el campo reservado del Registro Tipo 9 debe contener espacios en blanco.");
        }

        if (batchControlLine[0] != '8')
        {
            throw new InvalidOperationException("Error Fatal 9: se requiere al menos un Registro Tipo 8 válido antes del Registro Tipo 9.");
        }
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

    private static void WriteValue(char[] buffer, int startPosition, string value)
    {
        value.CopyTo(0, buffer, startPosition - 1, value.Length);
    }

    private sealed record ReturnEntrySnapshot(string TransactionCode, decimal Amount, string ReceiverEntityCode);
}
