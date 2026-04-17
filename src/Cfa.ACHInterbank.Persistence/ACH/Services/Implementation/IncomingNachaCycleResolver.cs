using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaCycleResolver : IIncomingNachaCycleResolver
{
    private readonly AchDbContext _context;

    public IncomingNachaCycleResolver(AchDbContext context)
    {
        _context = context;
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
        var fileCycleNumber = ExtractCycleNumberFromFileName(request.FileName);

        var clearingHouse = await _context.ClearingHouses.AsNoTracking().FirstOrDefaultAsync(x => x.OriginCode == immediateOrigin, ct);
        if (clearingHouse is null)
        {
            var inferred = InferClearingHouseFromFileName(request.FileName);
            if (inferred.HasValue)
            {
                clearingHouse = await _context.ClearingHouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == inferred.Value, ct);
                warnings.Add("La cámara se infirió por nombre de archivo porque no coincidió OriginCode.");
            }
        }

        var operationalDate = ParseDate(headerDateRaw);
        if (!operationalDate.HasValue)
        {
            errors.Add($"Fecha operativa inválida en header: '{headerDateRaw}'.");
        }

        if (clearingHouse is null || !operationalDate.HasValue)
        {
            return Build(false, false, clearingHouse?.Id, operationalDate, null, 0.2m, IncomingNachaCycleResolutionStatus.NoResuelto, "NoResuelto", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber
            });
        }

        var candidatesQuery = _context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouse.Id && x.ProcessingDate.Date == operationalDate.Value.Date);

        var candidates = await candidatesQuery
            .OrderBy(x => x.CutoffTime)
            .ToListAsync(ct);

        var filteredByName = fileCycleNumber.HasValue
            ? candidates.Where(x => x.CycleName.Contains(fileCycleNumber.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList()
            : candidates;

        var effectiveCandidates = filteredByName.Count > 0 ? filteredByName : candidates;
        var inferredStatus = string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
            ? IncomingNachaCycleResolutionStatus.ResueltoConfirmado
            : IncomingNachaCycleResolutionStatus.ResueltoInferido;

        if (effectiveCandidates.Count == 1)
        {
            var cycle = effectiveCandidates[0];
            if (fileCycleNumber.HasValue && !cycle.CycleName.Contains(fileCycleNumber.Value.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("El número de ciclo del nombre no coincide con el ciclo operativo seleccionado por fecha/cámara.");
            }

            return Build(true, false, clearingHouse.Id, operationalDate, cycle.Id, 0.95m, inferredStatus, fileCycleNumber.HasValue ? "Header+Nombre" : "Header", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber,
                candidateCount = candidates.Count,
                selectedCycleId = cycle.Id
            });
        }

        if (effectiveCandidates.Count > 1)
        {
            errors.Add("Se detectaron múltiples ciclos candidatos para la misma cámara y fecha operativa.");
            return Build(false, true, clearingHouse.Id, operationalDate, null, 0.45m, IncomingNachaCycleResolutionStatus.Ambiguo, "Ambiguo", warnings, errors, new
            {
                request.FileName,
                immediateOrigin,
                headerDateRaw,
                fileCycleNumber,
                candidateIds = effectiveCandidates.Select(x => x.Id).ToList()
            });
        }

        errors.Add("No se encontró ciclo operativo candidato para cámara y fecha operativa.");
        return Build(false, false, clearingHouse.Id, operationalDate, null, 0.35m, IncomingNachaCycleResolutionStatus.NoResuelto, "SinCandidatos", warnings, errors, new
        {
            request.FileName,
            immediateOrigin,
            headerDateRaw,
            fileCycleNumber
        });
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

    private static int? ExtractCycleNumberFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name)) return null;
        var match = Regex.Match(name, "(?:^|[._-])(\\d{1,2})(?:$|[._-])");
        if (!match.Success) return null;
        return int.TryParse(match.Groups[1].Value, out var cycleNumber) && cycleNumber > 0 ? cycleNumber : null;
    }

    private static int? InferClearingHouseFromFileName(string fileName)
    {
        var name = fileName.ToLowerInvariant();
        if (name.Contains("cenit")) return 2;
        if (name.Contains("ach")) return 1;
        return null;
    }
}
