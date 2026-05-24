using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class BatchResolver : IBatchResolver
{
    private readonly AchDbContext _context;
    private readonly IAchBatchRepository _batchRepository;
    private readonly IRoutingStrategyService _routing;
    private readonly TimeProvider _timeProvider;

    public BatchResolver(
        AchDbContext context,
        IAchBatchRepository batchRepository,
        IRoutingStrategyService routing,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _batchRepository = batchRepository;
        _routing = routing;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<TransactionBatchContext> ResolveAsync(AchTransactionRequestData request, CancellationToken ct = default)
    {
        var source = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(fi => fi.IsDefaultSource && fi.Status == FinancialInstitutionStatus.Active)
            .Select(fi => new
            {
                fi.Id,
                fi.Name,
                fi.RoutingNumber,
                fi.TransitCode,
                fi.CheckDigit
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No existe institución de origen por defecto y activa.");

        string sourceRouting = source.RoutingNumber?.Trim() ?? string.Empty;
        string sourceTransit = source.TransitCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sourceRouting) || string.IsNullOrWhiteSpace(sourceTransit))
        {
            throw new InvalidOperationException("La institución de origen no tiene configurado el código de ruteo/transito.");
        }

        string originBase = $"{sourceRouting}{sourceTransit}";
        if (originBase.Length != 8)
        {
            throw new InvalidOperationException($"La institución de origen tiene una longitud inválida para el ruteo: {originBase}.");
        }

        var dest = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(fi => fi.Id == request.DestinationInstitutionId && fi.Status == FinancialInstitutionStatus.Active)
            .Select(fi => new
            {
                fi.Id,
                fi.RoutingNumber,
                fi.TransitCode,
                fi.CheckDigit
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Institución destino no encontrada o inactiva.");

        string destRouting = dest.RoutingNumber?.Trim() ?? string.Empty;
        string destTransit = dest.TransitCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(destRouting) || string.IsNullOrWhiteSpace(destTransit))
        {
            throw new InvalidOperationException("La institución destino no tiene configurado el código de ruteo/transito.");
        }

        string destinationBase = $"{destRouting}{destTransit}";
        if (destinationBase.Length != 8)
        {
            throw new InvalidOperationException($"La institución destino tiene una longitud inválida para el ruteo: {destinationBase}.");
        }

        var now = _timeProvider.GetLocalNow().LocalDateTime;
        string achCycleId = await _routing.ResolveClearingHouseForTransactionAsync(request.DestinationInstitutionId, now, ct);
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .FirstOrDefaultAsync(c => c.Id == achCycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo ACH para la transacción.");
        DateTime effectiveEntryDate = cycle.ProcessingDate.Date;

        var (windowStart, windowEnd) = BuildCycleWindow(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime);
        bool isOutsideWindow = now < windowStart || now > windowEnd;
        var isCenit = string.Equals(cycle.ClearingHouse?.Code, "CENIT", StringComparison.OrdinalIgnoreCase);

        if (isOutsideWindow && !isCenit)
        {
            EnsureCycleIsOpenForTransactions(cycle, now);
        }

        if (request.Type == TransactionTypeEnum.Debit && !request.IsPrenotification && IsCycleFive(cycle.CycleName))
        {
            throw new InvalidOperationException("No se aceptan transacciones débito en el Ciclo 5.");
        }

        string companyName = request.CompanyName.Trim();
        string companyIdentification = request.CompanyIdentification.Trim().ToUpperInvariant();

        var companyEntryDescriptionCatalog = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(item => item.Id == request.CompanyEntryDescriptionId && item.IsActive)
            .Select(item => new { item.Id, item.Term })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("El concepto de lote seleccionado no existe o está inactivo.");

        string companyEntryDescription = companyEntryDescriptionCatalog.Term.Trim().ToUpperInvariant();

        var batch = await _batchRepository.FindForTransactionAsync(
            achCycleId,
            companyName,
            companyIdentification,
            companyEntryDescription,
            effectiveEntryDate,
            ct);

        if (batch is null)
        {
            batch = new AchBatch
            {
                AchCycleId = achCycleId,
                CompanyName = companyName,
                CompanyIdentification = companyIdentification,
                CompanyEntryDescription = companyEntryDescription,
                CompanyEntryDescriptionId = companyEntryDescriptionCatalog.Id,
                EffectiveEntryDate = effectiveEntryDate,
                OriginOrOdfi = originBase
            };
            await _batchRepository.AddAsync(batch, ct);
        }

        return new TransactionBatchContext
        {
            Batch = batch,
            AchCycleId = achCycleId,
            EffectiveEntryDate = effectiveEntryDate,
            OriginatingDfi = originBase,
            ReceivingDfi = destinationBase,
            CompanyName = companyName,
            CompanyIdentification = companyIdentification,
            CompanyEntryDescription = companyEntryDescription,
            CompanyEntryDescriptionId = companyEntryDescriptionCatalog.Id,
            ReturnSlaDeadlineAtUtc = await CalculateReturnSlaDeadlineAtUtcAsync(cycle, ct),
            ServiceClassCode = "200",
            SourceInstitutionId = source.Id,
            DestinationInstitutionId = dest.Id,
            ClearingHouseId = cycle.ClearingHouseId,
            MustQueueForTargetCycle = isCenit && isOutsideWindow,
            QueueReason = isCenit && isOutsideWindow
                ? "OutsideOperatingWindowRoutedToNextCycle"
                : string.Empty
        };
    }


    private static void EnsureCycleIsOpenForTransactions(AchCycle cycle, DateTime nowLocal)
    {
        var (windowStart, windowEnd) = BuildCycleWindow(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime);

        if (nowLocal < windowStart)
        {
            throw new InvalidOperationException($"El ciclo {cycle.CycleName} aún no está abierto. Ventana operativa: {windowStart:yyyy-MM-dd HH:mm} - {windowEnd:yyyy-MM-dd HH:mm}.");
        }

        if (nowLocal > windowEnd)
        {
            throw new InvalidOperationException($"El ciclo {cycle.CycleName} está cerrado para recepción de transacciones. Ventana operativa: {windowStart:yyyy-MM-dd HH:mm} - {windowEnd:yyyy-MM-dd HH:mm}.");
        }
    }

    private static (DateTime Start, DateTime End) BuildCycleWindow(DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            return (processingDate.Date + startTime, processingDate.Date + endTime);
        }

        return (processingDate.Date.AddDays(-1) + startTime, processingDate.Date + endTime);
    }

    private static bool IsCycleFive(string cycleName)
    {
        if (string.IsNullOrWhiteSpace(cycleName))
        {
            return false;
        }

        var digits = new string(cycleName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var cycleNumber) && cycleNumber == 5;
    }

    private async Task<DateTime?> CalculateReturnSlaDeadlineAtUtcAsync(AchCycle currentCycle, CancellationToken ct)
    {
        var orderedCycles = await _batchRepository.GetUpcomingCyclesAsync(
            currentCycle.ClearingHouseId,
            currentCycle.ProcessingDate,
            currentCycle.CutoffTime,
            5,
            ct);

        if (orderedCycles.Count < 5)
        {
            return null;
        }

        var deadlineCycle = orderedCycles[4];
        var deadlineLocal = deadlineCycle.ProcessingDate.Date + deadlineCycle.CutoffTime;
        return DateTime.SpecifyKind(deadlineLocal, DateTimeKind.Local).ToUniversalTime();
    }


    private static string BuildTransitCodeWithCheckDigit(string routingNumber, string transitCode, string? checkDigit)
    {
        string baseCode = $"{routingNumber}{transitCode}";
        string resolvedCheckDigit = string.IsNullOrWhiteSpace(checkDigit)
            ? DigitoChequeoHelper.CalcularDigitoChequeo(baseCode)
            : checkDigit.Trim();

        if (resolvedCheckDigit.Length != 1)
        {
            throw new InvalidOperationException($"Dígito de chequeo inválido para código de tránsito {baseCode}.");
        }

        return $"{baseCode}{resolvedCheckDigit}";
    }
}
