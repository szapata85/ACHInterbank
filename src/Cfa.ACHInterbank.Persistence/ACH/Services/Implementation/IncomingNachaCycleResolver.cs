using System.Globalization;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaCycleResolver : IIncomingNachaCycleResolver
{
    private readonly AchDbContext _context;
    private readonly IPaymentRailContextService? _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService;
    private readonly ILogger<IncomingNachaCycleResolver> _logger;

    public IncomingNachaCycleResolver(
        AchDbContext context,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<IncomingNachaCycleResolver>? logger = null)
    {
        _context = context;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<IncomingNachaCycleResolver>.Instance;
    }

    public async Task<IncomingNachaCycleResolutionResult> ResolveAsync(IncomingNachaCycleResolutionRequest request, CancellationToken ct = default)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var header = request.Records.FirstOrDefault(x => x.Length >= 96 && x[0] == '1');
        if (string.IsNullOrWhiteSpace(header))
        {
            return Build(false, false, null, null, null, 0, IncomingNachaCycleResolutionStatus.NoResuelto, "SinHeader", warnings, ["No se encontró registro tipo 1 en el archivo NACHA-M."], new { request.FileName });
        }

        var immediateOrigin = header.Substring(13, 10).Trim();
        var headerDateRaw = header.Substring(23, 8).Trim();
        var fileCycleNumber = CenitOfficialFileNameParser.ExtractCycleNumberFromFileName(request.FileName);
        var fileNameParsed = CenitOfficialFileNameParser.TryParseCenitFileName(request.FileName, out var parsedFileName)
            ? parsedFileName
            : null;

        var clearingHouse = await _context.ClearingHouses.AsNoTracking()
            .Include(x => x.ClearingHouseConfig)
            .FirstOrDefaultAsync(x => x.OriginCode == immediateOrigin, ct);
        if (clearingHouse is null)
        {
            var inferred = await InferClearingHouseFromFileNameAsync(request.FileName, ct);
            if (inferred is not null)
            {
                clearingHouse = inferred;
                warnings.Add("La cámara se infirió por nombre de archivo porque no coincidió OriginCode.");
            }
        }

        var operationalDate = ParseDate(headerDateRaw);
        if (!operationalDate.HasValue)
        {
            errors.Add($"Fecha operativa inválida en header: '{headerDateRaw}'.");
        }

        if (clearingHouse is not null
            && string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
            && fileNameParsed is not null
            && operationalDate.HasValue
            && fileNameParsed.FileDate != DateOnly.FromDateTime(operationalDate.Value))
        {
            errors.Add("CENIT_FILENAME_HEADER_DATE_MISMATCH");
            return Build(false, false, clearingHouse.Id, operationalDate, null, 0.15m, IncomingNachaCycleResolutionStatus.NoResuelto, "FechaNombreNoCoincide", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber,
                parsedFileDate = fileNameParsed.FileDate,
                parsedSuffix = fileNameParsed.Suffix
            });
        }

        if (clearingHouse is null || !operationalDate.HasValue)
        {
            var shadowResult = CompareCycleShadow(
                clearingHouseId: clearingHouse?.Id,
                clearingHouseCode: clearingHouse?.Code,
                paymentRailCode: clearingHouse?.ClearingHouseConfig?.PaymentRailCode,
                operationalDate: operationalDate,
                legacyDecisionCode: "LEGACY_CYCLE_UNRESOLVED",
                legacyResolved: false);
            return Build(false, false, clearingHouse?.Id, operationalDate, null, 0.2m, IncomingNachaCycleResolutionStatus.NoResuelto, "NoResuelto", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber,
                parsedFileDate = fileNameParsed?.FileDate,
                parsedSuffix = fileNameParsed?.Suffix,
                shadowCompare = shadowResult
            });
        }

        var candidatesQuery = _context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouse.Id && x.ProcessingDate.Date == operationalDate.Value.Date);

        var candidates = await candidatesQuery
            .OrderBy(x => x.CutoffTime)
            .ToListAsync(ct);

        IReadOnlyList<AchCycle> effectiveCandidates = candidates;
        if (fileCycleNumber.HasValue)
        {
            effectiveCandidates = candidates
                .Where(x => ExternalFileNameSupport.TryExtractPositiveCycleNumber(x.CycleName, out var parsedCycleNumber) && parsedCycleNumber == fileCycleNumber.Value)
                .ToList();

            if (effectiveCandidates.Count == 0)
            {
                errors.Add($"El nombre oficial indica el ciclo {fileCycleNumber.Value}, pero no existe un ciclo operativo coincidente para la misma cámara y fecha.");
                var shadowResult = CompareCycleShadow(
                    clearingHouse.Id,
                    clearingHouse.Code,
                    clearingHouse.ClearingHouseConfig?.PaymentRailCode,
                    operationalDate,
                    "LEGACY_CYCLE_NO_MATCH_BY_NAME",
                    legacyResolved: false);
                return Build(false, false, clearingHouse.Id, operationalDate, null, 0.25m, IncomingNachaCycleResolutionStatus.NoResuelto, "NombreSinCandidato", warnings, errors, new
                {
                    request.FileName,
                    immediateOrigin,
                    headerDateRaw,
                    fileCycleNumber,
                    parsedFileDate = fileNameParsed?.FileDate,
                    parsedSuffix = fileNameParsed?.Suffix,
                    candidateIds = candidates.Select(x => x.Id).ToList(),
                    shadowCompare = shadowResult
                });
            }
        }

        var inferredStatus = string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
            ? IncomingNachaCycleResolutionStatus.ResueltoConfirmado
            : IncomingNachaCycleResolutionStatus.ResueltoInferido;

        if (effectiveCandidates.Count == 1)
        {
            var cycle = effectiveCandidates[0];
            if (fileCycleNumber.HasValue && (!ExternalFileNameSupport.TryExtractPositiveCycleNumber(cycle.CycleName, out var selectedCycleNumber) || selectedCycleNumber != fileCycleNumber.Value))
            {
                warnings.Add("El número de ciclo del nombre no coincide con el ciclo operativo seleccionado por fecha/cámara.");
            }

            var shadowResult = CompareCycleShadow(
                clearingHouse.Id,
                clearingHouse.Code,
                clearingHouse.ClearingHouseConfig?.PaymentRailCode,
                operationalDate,
                cycle.Id,
                legacyResolved: true);

            return Build(true, false, clearingHouse.Id, operationalDate, cycle.Id, 0.95m, inferredStatus, fileCycleNumber.HasValue ? "Header+Nombre" : "Header", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber,
                parsedFileDate = fileNameParsed?.FileDate,
                parsedSuffix = fileNameParsed?.Suffix,
                candidateCount = candidates.Count,
                selectedCycleId = cycle.Id,
                shadowCompare = shadowResult
            });
        }

        if (effectiveCandidates.Count > 1)
        {
            errors.Add("Se detectaron múltiples ciclos candidatos para la misma cámara y fecha operativa.");
            var shadowResult = CompareCycleShadow(
                clearingHouse.Id,
                clearingHouse.Code,
                clearingHouse.ClearingHouseConfig?.PaymentRailCode,
                operationalDate,
                "LEGACY_CYCLE_AMBIGUOUS",
                legacyResolved: false);
            return Build(false, true, clearingHouse.Id, operationalDate, null, 0.45m, IncomingNachaCycleResolutionStatus.Ambiguo, "Ambiguo", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber,
                parsedFileDate = fileNameParsed?.FileDate,
                parsedSuffix = fileNameParsed?.Suffix,
                candidateIds = effectiveCandidates.Select(x => x.Id).ToList(),
                shadowCompare = shadowResult
            });
        }

        errors.Add("No se encontró ciclo operativo candidato para cámara y fecha operativa.");
        var unresolvedShadow = CompareCycleShadow(
            clearingHouse.Id,
            clearingHouse.Code,
            clearingHouse.ClearingHouseConfig?.PaymentRailCode,
            operationalDate,
            "LEGACY_CYCLE_NO_CANDIDATE",
            legacyResolved: false);
        return Build(false, false, clearingHouse.Id, operationalDate, null, 0.35m, IncomingNachaCycleResolutionStatus.NoResuelto, "SinCandidatos", warnings, errors, new
        {
            request.FileName,
            immediateOrigin,
            headerDateRaw,
            fileCycleNumber,
            parsedFileDate = fileNameParsed?.FileDate,
            parsedSuffix = fileNameParsed?.Suffix,
            shadowCompare = unresolvedShadow
        });
    }

    private PaymentRailShadowCompareResult? CompareCycleShadow(
        int? clearingHouseId,
        string? clearingHouseCode,
        string? paymentRailCode,
        DateTime? operationalDate,
        string legacyDecisionCode,
        bool legacyResolved)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return null;
        }

        var context = _paymentRailContextService.ResolveContext(
            clearingHouseId,
            clearingHouseCode,
            achCycleId: legacyResolved ? legacyDecisionCode : null,
            operationalDate: operationalDate,
            requestedRailCode: paymentRailCode);
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(clearingHouseId, clearingHouseCode, paymentRailCode));
        var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
            context.OperationalContext,
            PaymentRailCapabilityKind.Cycle,
            legacyDecisionCode));
        var shadowResult = _shadowCompareService.CompareCycleResolution(
            context,
            wrapperResult,
            legacyDecisionCode,
            legacyResolved);

        _logger.LogInformation(
            "PAYMENT_RAIL_SHADOW_COMPARE_CYCLE|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
            shadowResult.RailCode,
            shadowResult.LegacyDecisionCode,
            shadowResult.WrapperDecisionCode,
            shadowResult.IsEquivalent,
            shadowResult.ComparisonCode);

        return shadowResult;
    }

    private static IncomingNachaCycleResolutionResult Build(
        bool resolved,
        bool ambiguous,
        int? clearingHouseId,
        DateTime? opDate,
        string? cycleId,
        decimal confidence,
        IncomingNachaCycleResolutionStatus status,
        string mode,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors,
        object evidence)
    {
        return new IncomingNachaCycleResolutionResult
        {
            IsResolved = resolved,
            IsAmbiguous = ambiguous,
            ClearingHouseId = clearingHouseId,
            DetectedClearingHouseId = clearingHouseId,
            OperationalDate = opDate,
            AchCycleId = cycleId,
            Confidence = confidence,
            Status = status,
            ResolutionMode = mode,
            Warnings = warnings,
            Errors = errors,
            EvidenceJson = JsonSerializer.Serialize(evidence)
        };
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyyyMMdd))
        {
            return yyyyMMdd.Date;
        }

        if (DateTime.TryParseExact(value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyMMdd))
        {
            return yyMMdd.Date;
        }

        return null;
    }

    private async Task<ClearingHouse?> InferClearingHouseFromFileNameAsync(string fileName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var name = fileName.ToLowerInvariant();
        var clearingHouses = await _context.ClearingHouses.AsNoTracking()
            .Include(x => x.ClearingHouseConfig)
            .ToListAsync(ct);

        var cenitByCatalog = clearingHouses.FirstOrDefault(ch =>
            (ch.Code ?? string.Empty).Contains("cenit", StringComparison.OrdinalIgnoreCase)
            || (ch.Name ?? string.Empty).Contains("cenit", StringComparison.OrdinalIgnoreCase));
        if (name.Contains("cenit") && cenitByCatalog is not null)
        {
            return cenitByCatalog;
        }

        var achByCatalog = clearingHouses.FirstOrDefault(ch =>
            (ch.Code ?? string.Empty).Contains("ach", StringComparison.OrdinalIgnoreCase)
            || (ch.Name ?? string.Empty).Contains("ach", StringComparison.OrdinalIgnoreCase));
        if (name.Contains("ach") && achByCatalog is not null)
        {
            return achByCatalog;
        }

        return null;
    }
}
