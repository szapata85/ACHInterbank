using Cfa.ACHInterbank.Application.ACH.Interfaces;
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
    private readonly IRoutingStrategyService _routing;

    public BatchResolver(AchDbContext context, IRoutingStrategyService routing)
    {
        _context = context;
        _routing = routing;
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

        string originWithCheckDigit = BuildTransitCodeWithCheckDigit(sourceRouting, sourceTransit, source.CheckDigit);

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

        string destinationWithCheckDigit = BuildTransitCodeWithCheckDigit(destRouting, destTransit, dest.CheckDigit);

        var now = DateTime.Now;
        string achCycleId = await _routing.ResolveClearingHouseForTransactionAsync(request.DestinationInstitutionId, now, ct);
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == achCycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo ACH para la transacción.");
        DateTime effectiveEntryDate = cycle.ProcessingDate.Date;

        string companyName = request.CompanyName.Trim();
        string companyIdentification = request.CompanyIdentification.Trim().ToUpperInvariant();

        var companyEntryDescriptionCatalog = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(item => item.Id == request.CompanyEntryDescriptionId && item.IsActive)
            .Select(item => new { item.Id, item.Term })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("El concepto de lote seleccionado no existe o está inactivo.");

        string companyEntryDescription = companyEntryDescriptionCatalog.Term.Trim().ToUpperInvariant();

        var batch = await _context.AchBatches
            .FirstOrDefaultAsync(b =>
                b.AchCycleId == achCycleId &&
                b.CompanyName == companyName &&
                b.CompanyIdentification == companyIdentification &&
                b.CompanyEntryDescription == companyEntryDescription &&
                b.EffectiveEntryDate == effectiveEntryDate, ct);

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
            _context.AchBatches.Add(batch);
            await _context.SaveChangesAsync(ct);
        }

        return new TransactionBatchContext
        {
            Batch = batch,
            AchCycleId = achCycleId,
            EffectiveEntryDate = effectiveEntryDate,
            OriginatingDfi = originWithCheckDigit,
            ReceivingDfi = destinationWithCheckDigit,
            CompanyName = companyName,
            CompanyIdentification = companyIdentification,
            CompanyEntryDescription = companyEntryDescription,
            CompanyEntryDescriptionId = companyEntryDescriptionCatalog.Id,
            ServiceClassCode = "200",
            SourceInstitutionId = source.Id,
            DestinationInstitutionId = dest.Id
        };
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
