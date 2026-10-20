using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnArtifactGenerationComponent(
    AchDbContext context,
    INachaFileBuilder? nachaFileBuilder,
    IExternalFileNamePolicy? externalFileNamePolicy,
    ILogger<AchReturnsService> logger)
{
    public void EnsureAvailable()
    {
        if (nachaFileBuilder is null)
        {
            throw new InvalidOperationException("RETURN_OUT_ACH_OPTION_C_REQUIRED: INachaFileBuilder es requerido; no existe fallback hardcoded.");
        }
        if (externalFileNamePolicy is null)
        {
            throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
        }
    }

    public async Task<AchReturnArtifactResult> GenerateAsync(
        AchReturnGenerationScope scope,
        GenerateReturnsFileRequest request,
        AchReturnAssemblyResult assembly,
        DateTime now,
        CancellationToken ct)
    {
        EnsureAvailable();
        var cycle = scope.ReturnCycle;
        var provisionalFileName = $"RET_{cycle.Id}_{now:yyyyMMddHHmmss}.RET";
        var optionCRequest = new NachaReturnOutBuildRequest(
            now,
            "A",
            assembly.Header.ImmediateDestination,
            assembly.Header.ImmediateOrigin,
            assembly.Header.ImmediateDestinationName,
            assembly.Header.ImmediateOriginName,
            "RETURN",
            assembly.Batches,
            PersistAudit: false,
            ClearingHouseCode: scope.IsCenit ? "CENIT" : "ACH",
            ClearingHouseName: cycle.ClearingHouse?.Name ?? (scope.IsCenit ? "CENIT" : "ACH Colombia"),
            NormativeVersion: scope.IsCenit ? CenitReturnOut2026Layout.NormativeVersion : "V35");
        var provisionalArtifact = await nachaFileBuilder!.BuildReturnOutAsync(optionCRequest, ct);
        var namingResult = await ResolveReturnExternalFileNameAsync(cycle, provisionalFileName, provisionalArtifact.Content, ct);
        var fileIdModifier = namingResult.Components.FileIdModifier?.ToString()
            ?? throw new InvalidOperationException("RETURN_FILENAME_FILE_ID_REQUIRED: la política no asignó identificador ZZZ.");
        var finalArtifact = await nachaFileBuilder.BuildReturnOutAsync(optionCRequest with
        {
            FileIdModifier = fileIdModifier,
            PersistAudit = true
        }, ct);

        return new AchReturnArtifactResult(
            namingResult.ExternalFileName,
            finalArtifact.Content,
            finalArtifact.RecordCount);
    }

    private async Task<ExternalFileNamePolicyResult> ResolveReturnExternalFileNameAsync(
        AchCycle cycle,
        string provisionalFileName,
        string nachaContent,
        CancellationToken ct)
    {
        if (cycle.ClearingHouse is null)
        {
            throw new InvalidOperationException("No se pudo resolver la cÃ¡mara para el archivo de devoluciÃ³n.");
        }

        var context = new ExternalFileNameContext
        {
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = cycle.ClearingHouse.Code,
            ClearingHouseOriginCode = cycle.ClearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            ProcessingDate = cycle.ProcessingDate,
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            InternalFileName = provisionalFileName,
            NachaContent = nachaContent,
            RequestedBy = "system"
        };

        if (externalFileNamePolicy is null)
        {
            throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
        }

        var policyResult = await externalFileNamePolicy.GenerateExternalNameAsync(context, ct);
        if (policyResult.Validation.IsHardBlocked)
        {
            var details = string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}"));
            throw new InvalidOperationException($"Error Fatal ID: External filename validation failed. {details}");
        }

        if (policyResult.Validation.Issues.Count > 0)
        {
            logger.LogWarning(
                "RETURN_FILENAME_POLICY_WARNING|CycleId={CycleId}|FileName={FileName}|Issues={Issues}",
                cycle.Id,
                policyResult.ExternalFileName,
                string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}")));
        }

        if (!string.IsNullOrWhiteSpace(policyResult.ExternalFileName))
        {
            return policyResult;
        }

        throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
    }
    // Preserved from the pre-R7 owner for source/compatibility continuity. The active
    // ReturnOut path still requires IExternalFileNamePolicy and does not call this fallback.
    private async Task<string> ResolveReturnExternalFileNameWithoutPolicyAsync(AchCycle cycle, CancellationToken ct)
    {
        var sameDayFiles = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Include(x => x.ReturnCycle)
            .Where(x => x.ReturnCycle.ClearingHouseId == cycle.ClearingHouseId && x.GeneratedAtUtc.Date == cycle.ProcessingDate.Date)
            .Select(x => x.FileName)
            .Where(x => x != null && x != string.Empty)
            .Distinct()
            .ToListAsync(ct);

        var parsedSequences = sameDayFiles
            .Select(fileName =>
            {
                var parsed = ExternalFileNameSupport.Parse(new ExternalFileNameContext
                {
                    ClearingHouseId = cycle.ClearingHouseId,
                    ClearingHouseCode = cycle.ClearingHouse?.Code ?? string.Empty,
                    ClearingHouseOriginCode = cycle.ClearingHouse?.OriginCode,
                    ProcessingDate = cycle.ProcessingDate,
                    ExternalFileType = ExternalFileType.ReturnOut,
                    Flow = ExternalFileFlow.Originacion,
                    Direction = ExternalFileDirection.Outbound
                }, fileName);

                return parsed.ExternalSequence;
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        var nextSequence = parsedSequences.Count == 0 ? 1 : parsedSequences.Max() + 1;
        if (nextSequence > 36)
        {
            throw new InvalidOperationException("Regla RET HARD BLOCK: mÃ¡ximo 36 archivos diarios por participante.");
        }

        logger.LogWarning(
            "RETURN_FILENAME_POLICY_FALLBACK|CycleId={CycleId}|ClearingHouseId={ClearingHouseId}|Sequence={Sequence}",
            cycle.Id,
            cycle.ClearingHouseId,
            nextSequence);

        var originCode = NormalizeDigits(cycle.ClearingHouse?.OriginCode, 7);
        return ExternalFileNameSupport.BuildReturnName(originCode, nextSequence);
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
