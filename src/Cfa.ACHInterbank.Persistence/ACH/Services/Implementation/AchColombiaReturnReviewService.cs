using System.Text.Json;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I04-I08 ACH: revisión, generación controlada y verificación de artefacto bajo doble habilitación.
/// Ninguno de estos métodos transmite archivos ni acredita aceptación de cámara.
/// </summary>
public sealed class AchColombiaReturnReviewService(AchDbContext context, TimeProvider clock,
    IAchReturnsService returnsService, IConfiguration? configuration = null,
    IAchOutboundReturnArtifactService? artifactService = null) : IAchColombiaReturnReviewService
{
    internal const string Approved = "APROBADA_SIN_ENVIO";
    internal const string Discarded = "DESCARTADA";
    internal const string FileGenerated = "ARCHIVO_GENERADO_SIN_ENVIO";

    public async Task<IReadOnlyList<AchColombiaReturnReviewItem>> ListAsync(int limit, CancellationToken ct = default)
    {
        var candidates = await context.AchColombiaReturnPreparations.AsNoTracking()
            .Include(p => p.OriginalTransaction).ThenInclude(t => t.AchCycle).ThenInclude(c => c.ClearingHouse)
            .Where(p => p.OriginalTransaction.AchCycle.ClearingHouse != null
                && p.OriginalTransaction.AchCycle.ClearingHouse.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode)
            .OrderByDescending(p => p.PreparedAtUtc).ThenByDescending(p => p.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);
        var ids = candidates.Select(p => p.OriginalTransactionId).ToArray();
        var generated = await context.AchReturnsGenerated.AsNoTracking()
            .Where(g => ids.Contains(g.OriginalTransactionId))
            .Select(g => g.OriginalTransactionId).ToListAsync(ct);
        var generatedSet = generated.ToHashSet();
        // Una única consulta de ciclos para las 100 candidatas como máximo.
        // La decisión real vuelve a comprobar la ventana bajo la petición de aprobación.
        var achHouseId = await context.ClearingHouses.AsNoTracking()
            .Where(h => h.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode)
            .Select(h => h.Id).FirstOrDefaultAsync(ct);
        var enabled = achHouseId > 0 && await context.ClearingHouseConfigs.AsNoTracking()
            .AnyAsync(x => x.ClearingHouseId == achHouseId
                && (x.AutomaticReturnMode == AchColombiaAutomaticReturnSettingsService.WithApproval
                    || x.AutomaticReturnMode == AchColombiaAutomaticReturnSettingsService.Automatic), ct);
        var bogotaNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId)).DateTime;
        var firstDate = candidates.Count == 0 ? bogotaNow.Date
            : candidates.Min(x => x.OriginalTransaction.AchCycle.ProcessingDate.Date);
        List<AchCycle> cycleRows = candidates.Count == 0 ? new List<AchCycle>() : await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouseCycleConfig)
            .Where(x => x.ClearingHouse.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode
                && x.ProcessingDate >= firstDate && x.ProcessingDate <= bogotaNow.Date.AddDays(1))
            .ToListAsync(ct);
        return candidates.Select(p =>
        {
            var manuallyGenerated = generatedSet.Contains(p.OriginalTransactionId);
            var decision = AchColombiaReturnPreparationWindow.Evaluate(
                p.OriginalTransaction, cycleRows, bogotaNow);
            var inWindow = enabled && !manuallyGenerated && p.ReasonCode != "R10"
                && decision is { LateCreditReview: false }
                && decision.Value.CycleId == p.SuggestedReturnCycleId;
            var available = inWindow && p.Status == AchColombiaReturnPreparationService.Prepared;
            var canGenerateFile = inWindow && p.Status == Approved;
            return new AchColombiaReturnReviewItem(
                p.Id, p.OriginalTransactionId, p.OriginalTransaction.Reference,
                p.OriginalTransaction.Amount, p.OriginalTransaction.Type.ToString(),
                p.OriginalCycleId, p.SuggestedReturnCycleId, p.ReasonCode, p.Status,
                p.PreparedAtUtc, available,
                p.Status == FileGenerated ? "Archivo generado, sin envío. La descarga verificará su integridad."
                : manuallyGenerated ? "Ya se generó una devolución para la transacción original."
                : p.Status == AchColombiaReturnPreparationService.LateCreditReview
                    ? "Crédito fuera de la ventana ordinaria. Requiere gestión especial."
                : p.Status == Discarded ? "Candidata descartada."
                : !enabled ? "La preparación automática de ACH Colombia está desactivada."
                : p.ReasonCode == "R10" ? "R10 requiere evidencia adicional antes de su aprobación."
                : !inWindow ? "La ventana operativa ha cambiado o no está disponible."
                : p.Status == Approved ? "Puedes generar el archivo; no se enviará a la cámara."
                : "La aprobación volverá a validar la ventana y la causal.",
                canGenerateFile);
        }).ToArray();
    }

    public async Task<AchColombiaReturnReviewResult> DecideAsync(long candidateId,
        AchColombiaReturnReviewDecision decision, CancellationToken ct = default)
    {
        if (candidateId <= 0 || decision is null || decision.Action is not ("APROBAR" or "DESCARTAR"))
            throw new ArgumentException("La decisión no es válida.", nameof(decision));

        var candidate = await context.AchColombiaReturnPreparations
            .Include(p => p.SourceEvent)
            .Include(p => p.OriginalTransaction).ThenInclude(t => t.AchCycle).ThenInclude(c => c.ClearingHouse)
            .SingleOrDefaultAsync(p => p.Id == candidateId, ct);
        if (candidate?.OriginalTransaction?.AchCycle?.ClearingHouse?.Code
            != RegulatoryCycleScheduleCatalog.AchColombiaCode)
            throw new AchColombiaReturnReviewConflictException("La candidata no existe en ACH Colombia.");

        // Resolver solo una vez, incluso entre dos operadores: Status es token de concurrencia EF.
        if (candidate.Status is not (AchColombiaReturnPreparationService.Prepared
            or AchColombiaReturnPreparationService.LateCreditReview))
            throw new AchColombiaReturnReviewConflictException("La candidata ya fue resuelta. Actualiza la consulta.");

        if (decision.Action == "APROBAR")
            await ValidateApprovalAsync(candidate, approvedForFile: false, ct: ct);

        candidate.Status = decision.Action == "APROBAR" ? Approved : Discarded;
        try
        {
            // SaveChangesAsync genera AuditLogs con usuario/fecha/campos modificados,
            // a diferencia de ExecuteUpdateAsync, y comprueba concurrencia por Status.
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AchColombiaReturnReviewConflictException(
                "Otra persona modificó esta candidata. Actualiza la consulta antes de continuar.");
        }

        return new AchColombiaReturnReviewResult(candidate.Id, candidate.Status);
    }

    /// <summary>
    /// I05-ACH: genera SOLO bajo petición explícita, desde una candidata aprobada.
    /// La transacción SQL envolvente mantiene el estado de la candidata y el archivo
    /// generado por el servicio existente en la misma confirmación. No transmite el .RET.
    /// </summary>
    public Task<GenerateReturnsFileResponse> GenerateApprovedFileAsync(long candidateId,
        CancellationToken ct = default) => GenerateFileAsync(candidateId, automatic: false, ct);

    /// <summary>
    /// I06-ACH: únicamente proceso interno; requiere modo AUTOMATICO y puerta técnica
    /// de despliegue, ambos verificados antes de generar. No transmite.
    /// </summary>
    public Task<GenerateReturnsFileResponse> GenerateAutomaticallyAsync(long candidateId,
        CancellationToken ct = default) => GenerateFileAsync(candidateId, automatic: true, ct);

    private Task<GenerateReturnsFileResponse> GenerateFileAsync(
        long candidateId, bool automatic, CancellationToken ct)
    {
        if (candidateId <= 0)
            throw new ArgumentException("La candidata no es válida.", nameof(candidateId));
        if (automatic && configuration?.GetValue<bool>("AchCol:EnableAutomaticReturnFileGeneration") != true)
            throw new AchColombiaReturnReviewConflictException(
                "La generación automática todavía no está habilitada para ACH Colombia.");
        // I08-ACH: nunca activar generación autónoma sin poder verificar el mismo
        // contenido NACHA-M que quedó persistido. La revisión manual no se altera.
        if (automatic && artifactService is null)
            throw new AchColombiaReturnReviewConflictException(
                "No está disponible la validación de integridad de ACH Colombia.");

        return context.Database.IsRelational()
            ? context.Database.CreateExecutionStrategy().ExecuteAsync(
                () => GenerateApprovedFileAttemptAsync(candidateId, automatic, ct))
            : GenerateApprovedFileAttemptAsync(candidateId, automatic, ct);
    }

    /// <summary>Reconstruye el archivo ya generado, sin consumir nuevas secuencias.</summary>
    public async Task<GenerateReturnsFileResponse> RecoverGeneratedFileAsync(
        long candidateId, CancellationToken ct = default)
    {
        if (candidateId <= 0 || artifactService is null)
            throw new AchColombiaReturnReviewConflictException("El archivo no está disponible.");
        var candidate = await context.AchColombiaReturnPreparations.AsNoTracking()
            .Include(p => p.OriginalTransaction).ThenInclude(t => t.AchCycle).ThenInclude(c => c.ClearingHouse)
            .SingleOrDefaultAsync(p => p.Id == candidateId, ct);
        if (candidate?.Status != FileGenerated || candidate.OriginalTransaction.AchCycle.ClearingHouse?.Code
            != RegulatoryCycleScheduleCatalog.AchColombiaCode)
            throw new AchColombiaReturnReviewConflictException(
                "Solo se pueden recuperar archivos generados de ACH Colombia.");
        var generated = await context.AchReturnsGenerated.AsNoTracking()
            .SingleOrDefaultAsync(g => g.OriginalTransactionId == candidate.OriginalTransactionId, ct);
        if (generated is null || string.IsNullOrWhiteSpace(generated.FileName))
            throw new AchColombiaReturnReviewConflictException("No existe un archivo generado verificable.");

        AchOutboundReturnArtifact artifact;
        try
        {
            artifact = await artifactService.BuildAsync(generated.FileName, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
        {
            throw new AchColombiaReturnReviewConflictException(
                "El archivo registrado no pudo reconstruirse. Requiere revisión operativa.");
        }
        if (artifact.ClearingHouseId != candidate.OriginalTransaction.AchCycle.ClearingHouseId
            || artifact.CycleId != generated.ReturnCycleId
            || artifact.FileName != generated.FileName
            || artifact.ReturnCount != 1
            || artifact.TransactionIds.Count != 1
            || artifact.TransactionIds[0] != candidate.OriginalTransactionId
            || artifact.Content.Length == 0)
            throw new AchColombiaReturnReviewConflictException(
                "El archivo reconstruido no corresponde a esta candidata. Requiere revisión.");

        // I07-ACH: el artefacto reconstruido DEBE ser idéntico al generado.
        // El generador ya conserva la huella original en el evento de estado;
        // no introducir una segunda tabla ni asumir equivalencia por nombre.
        var originalPayload = await context.AchTransactionStateEvents.AsNoTracking()
            .Where(e => e.AchTransactionId == candidate.OriginalTransactionId
                && e.IdempotencyKey == $"outbound-return-v1:{candidate.OriginalTransactionId}"
                && e.ToState == AchTransferStateEnum.ReturnedByEpr
                && e.ClearingHouseId == candidate.OriginalTransaction.AchCycle.ClearingHouseId)
            .Select(e => e.PayloadJson)
            .SingleOrDefaultAsync(ct);
        if (!MatchesOriginalArtifact(originalPayload, generated, artifact,
                candidate.OriginalTransaction.AchCycle.ClearingHouseId))
            throw new AchColombiaReturnReviewConflictException(
                "No se pudo comprobar que el archivo coincida con el original generado. " +
                "No se entregó ninguna descarga; requiere revisión operativa.");

        return new GenerateReturnsFileResponse(artifact.FileName, "text/plain",
            artifact.Content, artifact.RecordCount, artifact.ReturnCount);
    }

    private async Task<GenerateReturnsFileResponse> GenerateApprovedFileAttemptAsync(
        long candidateId, bool automatic, CancellationToken ct)
    {
        var candidate = await context.AchColombiaReturnPreparations
            .Include(p => p.SourceEvent)
            .Include(p => p.OriginalTransaction).ThenInclude(t => t.AchCycle).ThenInclude(c => c.ClearingHouse)
            .SingleOrDefaultAsync(p => p.Id == candidateId, ct);
        if (candidate?.OriginalTransaction?.AchCycle?.ClearingHouse?.Code
            != RegulatoryCycleScheduleCatalog.AchColombiaCode)
            throw new AchColombiaReturnReviewConflictException(
                "La candidata no existe en ACH Colombia.");
        if (automatic
            ? candidate.Status is not (AchColombiaReturnPreparationService.Prepared or Approved)
            : candidate.Status != Approved)
            throw new AchColombiaReturnReviewConflictException(
                "La candidata no está disponible para generar el archivo. Actualiza la consulta.");

        // La aprobación pudo quedar obsoleta: repetir controles de cámara, configuración,
        // causal, evidencia, ciclo, vencimiento y archivo manual antes de generar.
        await ValidateApprovalAsync(candidate, approvedForFile: candidate.Status == Approved,
            ct: ct, requireAutomaticMode: automatic);

        // Una sola operación para el registro generado, los eventos de estado y la
        // candidata. El generador detecta colisiones manuales con lock y clave única.
        await using var unitOfWork = context.Database.IsRelational()
            && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct)
            : null;
        try
        {
            var response = await returnsService.GenerateReturnsFileAsync(
                new GenerateReturnsFileRequest(candidate.OriginalCycleId,
                    [new ReturnSelectionItemDto(candidate.OriginalTransactionId, candidate.ReasonCode)],
                    candidate.SuggestedReturnCycleId), ct);

            // No dar por generado un archivo solo por recibir bytes del generador.
            // Corroborar el registro único que usa también la generación manual.
            var generated = await context.AchReturnsGenerated.AsNoTracking()
                .SingleOrDefaultAsync(x => x.OriginalTransactionId == candidate.OriginalTransactionId, ct);
            if (generated is null
                || generated.ReturnCycleId != candidate.SuggestedReturnCycleId
                || generated.ReturnReasonCode != candidate.ReasonCode
                || !string.Equals(generated.FileName, response.FileName, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(response.FileName)
                || response.Content.Length == 0 || response.TotalReturns != 1)
                throw new InvalidOperationException(
                    "El generador no confirmó el archivo de ACH Colombia. No se completó la operación.");

            // I08-ACH: comprobación de ida y vuelta ANTES del commit de la generación
            // autónoma. No se acepta un archivo que no pueda reproducirse byte a byte
            // con la evidencia persistida. La transacción envolvente revierte el
            // generado y el estado si la comprobación falla (solo BD relacional).
            if (automatic)
                await VerifyAutomaticArtifactAsync(candidate, generated, response, ct);

            candidate.Status = FileGenerated;
            await context.SaveChangesAsync(ct); // auditoría y concurrencia de Status.
            if (unitOfWork is not null)
                await unitOfWork.CommitAsync(ct);
            return response;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (unitOfWork is not null)
                await unitOfWork.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw new AchColombiaReturnReviewConflictException(
                "Otra persona modificó la candidata. Consulta su estado antes de reintentar.");
        }
        catch
        {
            if (unitOfWork is not null)
                await unitOfWork.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task VerifyAutomaticArtifactAsync(AchColombiaReturnPreparation candidate,
        AchReturnGenerated generated, GenerateReturnsFileResponse original, CancellationToken ct)
    {
        if (artifactService is null)
            throw new AchColombiaReturnReviewConflictException(
                "No está disponible la validación de integridad de ACH Colombia.");
        AchOutboundReturnArtifact rebuilt;
        try
        {
            rebuilt = await artifactService.BuildAsync(generated.FileName, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
        {
            throw new AchColombiaReturnReviewConflictException(
                "El archivo de ACH Colombia no pudo verificarse antes de confirmarlo.");
        }

        if (rebuilt.ClearingHouseId != candidate.OriginalTransaction.AchCycle.ClearingHouseId
            || rebuilt.CycleId != candidate.SuggestedReturnCycleId
            || rebuilt.FileName != generated.FileName
            || rebuilt.ReturnCount != 1 || rebuilt.TransactionIds.Count != 1
            || rebuilt.TransactionIds[0] != candidate.OriginalTransactionId
            || rebuilt.RecordCount != original.TotalRecords
            || rebuilt.Content.Length == 0
            || !CryptographicOperations.FixedTimeEquals(rebuilt.Content, original.Content))
            throw new AchColombiaReturnReviewConflictException(
                "El archivo generado no coincide con su reconstrucción. " +
                "Se detuvo la generación automática; requiere revisión operativa.");
    }

    private async Task ValidateApprovalAsync(AchColombiaReturnPreparation candidate,
        bool approvedForFile, CancellationToken ct, bool requireAutomaticMode = false)
    {
        if (candidate.Status != (approvedForFile ? Approved : AchColombiaReturnPreparationService.Prepared)
            || string.IsNullOrWhiteSpace(candidate.SuggestedReturnCycleId)
            || candidate.OriginalCycleId != candidate.OriginalTransaction.AchCycleId)
            throw new AchColombiaReturnReviewConflictException(
                "La candidata no tiene un estado o un ciclo válido para esta operación.");

        if (candidate.ReasonCode == "R10")
            throw new AchColombiaReturnReviewConflictException(
                "La causal R10 requiere documentar la base verificable antes de aprobar.");

        var tx = candidate.OriginalTransaction;
        var houseId = tx.AchCycle.ClearingHouseId;
        var mode = await context.ClearingHouseConfigs.AsNoTracking()
            .Where(c => c.ClearingHouseId == houseId)
            .Select(c => c.AutomaticReturnMode).SingleOrDefaultAsync(ct);
        if ((requireAutomaticMode && mode != AchColombiaAutomaticReturnSettingsService.Automatic)
            || mode is not (AchColombiaAutomaticReturnSettingsService.WithApproval
                or AchColombiaAutomaticReturnSettingsService.Automatic))
            throw new AchColombiaReturnReviewConflictException(
                "La preparación automática de ACH Colombia está desactivada.");

        if (await context.AchReturnsGenerated.AsNoTracking()
                .AnyAsync(g => g.OriginalTransactionId == candidate.OriginalTransactionId, ct))
            throw new AchColombiaReturnReviewConflictException(
                "Ya existe una devolución generada manualmente para la transacción.");

        if (candidate.SourceEvent.AchTransactionId != tx.Id
            || candidate.SourceEvent.EventType != AchColombiaApplicationFailureDetector.DetectedEvent
            || candidate.SourceEvent.RaisedBy != "achcol.application-failure.detector"
            || !EvidenceStillMatches(candidate.SourceEvent.EvidenceJson, candidate.ReasonCode, tx.Id))
            throw new AchColombiaReturnReviewConflictException(
                "La evidencia funcional de la candidata ya no puede verificarse.");

        var activeCode = await context.Set<AchReturnCode>().AsNoTracking().AnyAsync(c =>
            c.ClearingHouseId == houseId && c.Code == candidate.ReasonCode && c.IsActive &&
            (tx.Type == TransactionTypeEnum.Credit && c.AppliesToCredit
              || tx.Type == TransactionTypeEnum.Debit && c.AppliesToDebit
              || tx.Type == TransactionTypeEnum.Prenotification && c.AppliesToPrenotification), ct);
        if (!activeCode || !await context.ReturnReasons.AsNoTracking()
                .AnyAsync(r => r.Code == candidate.ReasonCode && r.IsForReturn, ct))
            throw new AchColombiaReturnReviewConflictException(
                "La causal no está habilitada para esta transacción y esta cámara.");

        var bogotaNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId)).DateTime;
        var cycles = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouseCycleConfig)
            .Where(x => x.ClearingHouseId == houseId
                && x.ProcessingDate >= tx.AchCycle.ProcessingDate.Date
                && x.ProcessingDate <= bogotaNow.Date.AddDays(1))
            .ToListAsync(ct);
        var current = AchColombiaReturnPreparationWindow.Evaluate(tx, cycles, bogotaNow);
        if (current is null || current.Value.LateCreditReview
            || current.Value.CycleId != candidate.SuggestedReturnCycleId)
            throw new AchColombiaReturnReviewConflictException(
                "El ciclo propuesto ya no está vigente. No se aprobó la candidata; revisa el vencimiento.");
    }

    // Solo los metadatos de la generación original autorizan la recuperación.
    // Fail-closed también para eventos históricos incompletos o archivos alterados.
    private static bool MatchesOriginalArtifact(string? payloadJson, AchReturnGenerated generated,
        AchOutboundReturnArtifact artifact, int clearingHouseId)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return false;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            var originalHash = root.GetProperty("contentSha256").GetString();
            if (string.IsNullOrWhiteSpace(originalHash)) return false;
            var expectedDigest = Convert.FromHexString(originalHash);
            if (expectedDigest.Length != 32) return false;

            return root.GetProperty("eventType").GetString() == "ReturnFileGenerated"
                && root.GetProperty("originalTransactionId").GetInt32() == generated.OriginalTransactionId
                && root.GetProperty("clearingHouseId").GetInt32() == clearingHouseId
                && root.GetProperty("fileName").GetString() == generated.FileName
                && root.GetProperty("returnCycleId").GetString() == generated.ReturnCycleId
                && root.GetProperty("returnReasonCode").GetString() == generated.ReturnReasonCode
                && root.GetProperty("recordCount").GetInt32() == artifact.RecordCount
                && root.GetProperty("returnCount").GetInt32() == artifact.ReturnCount
                && root.GetProperty("transmissionStatus").GetString() == "GeneratedNotTransmitted"
                && CryptographicOperations.FixedTimeEquals(expectedDigest, SHA256.HashData(artifact.Content));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException
            or InvalidOperationException or FormatException or OverflowException)
        {
            return false;
        }
    }

    private static bool EvidenceStillMatches(string evidence, string reasonCode, int transactionId)
    {
        try
        {
            using var json = JsonDocument.Parse(evidence);
            var root = json.RootElement;
            return root.GetProperty("camera").GetString() == RegulatoryCycleScheduleCatalog.AchColombiaCode
                && root.GetProperty("origin").GetString() == "CORE_FUNCIONAL"
                && root.GetProperty("transactionId").GetInt32() == transactionId
                && root.GetProperty("causeVerified").GetBoolean()
                && !root.GetProperty("returnGenerated").GetBoolean()
                && !root.GetProperty("returnSent").GetBoolean()
                && root.GetProperty("suggestedReturnReason").GetString() == reasonCode
                && root.GetProperty("responseCode").GetString() == reasonCode;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return false;
        }
    }
}
