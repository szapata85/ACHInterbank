using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchOutboundReturnDispatchService(
    AchDbContext context,
    IAchReturnsService returnsService,
    IAchOutboundReturnArtifactService artifactService,
    INachaExportDigitalEnvelopeService digitalEnvelopeService,
    IAchFileExportAuditService fileExportAuditService,
    IAchOutboundReturnTransportAdapter transportAdapter,
    IAchFileTransmissionEvidenceRecorder evidenceRecorder) : IAchOutboundReturnDispatchService
{
    public async Task<AchOutboundReturnDispatchResult> GenerateAndDispatchAsync(
        AchOutboundReturnGenerateDispatchRequest request,
        CancellationToken ct = default)
    {
        ValidateRequest(request.IdempotencyKey, request.Actor);
        var generated = await returnsService.GenerateReturnsFileAsync(request.Generation, ct);
        var artifact = new AchOutboundReturnArtifact(
            generated.FileName,
            generated.Content,
            generated.TotalRecords,
            generated.TotalReturns,
            request.Generation.CycleId,
            await ResolveClearingHouseIdAsync(request.Generation.CycleId, ct),
            request.Generation.Items.Select(x => x.TransactionId).Distinct().OrderBy(x => x).ToArray(),
            Convert.ToHexString(SHA256.HashData(generated.Content)));
        return await DispatchArtifactAsync(artifact, request.IdempotencyKey, request.Actor, ct);
    }

    public async Task<AchOutboundReturnDispatchResult> DispatchAsync(
        AchOutboundReturnDispatchRequest request,
        CancellationToken ct = default)
    {
        ValidateRequest(request.IdempotencyKey, request.Actor);
        var generatedFileName = request.FileName.EndsWith(".ENV", StringComparison.OrdinalIgnoreCase)
            ? request.FileName[..^4]
            : request.FileName;
        var artifact = await artifactService.BuildAsync(generatedFileName, ct);
        return await DispatchArtifactAsync(artifact, request.IdempotencyKey, request.Actor, ct);
    }

    private async Task<AchOutboundReturnDispatchResult> DispatchArtifactAsync(
        AchOutboundReturnArtifact artifact,
        string idempotencyKey,
        string actor,
        CancellationToken ct,
        bool allowStageRaceRecovery = true)
    {
        var encryptedFileName = artifact.FileName + ".ENV";
        AchFileExport encryptedExport;
        AchFileTransmissionAttempt attempt;
        byte[] protectedContent;
        string protectedHash;
        await using var stageTransaction = context.Database.IsRelational()
                                           && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct)
            : null;
        try
        {
            await fileExportAuditService.RecordGeneratedFileAsync(
                artifact.CycleId,
                artifact.ClearingHouseId,
                "RETURN",
                artifact.FileName,
                artifact.RecordCount,
                artifact.ReturnCount,
                false,
                artifact.TransactionIds,
                artifact.ContentSha256,
                ct);

            var existingExport = await context.AchFileExports
                .Include(x => x.TransmissionAttempts)
                .SingleOrDefaultAsync(x => x.AchCycleId == artifact.CycleId
                                           && x.ExportKind == "RETURN"
                                           && x.FileName == encryptedFileName
                                           && x.IsEncrypted, ct);
            if (existingExport is null)
            {
                var envelope = await digitalEnvelopeService.EncryptAsync(
                    artifact.ClearingHouseId,
                    artifact.FileName,
                    artifact.Content,
                    actor,
                    ct);
                encryptedFileName = envelope.FileName;
                protectedContent = envelope.Content;
                protectedHash = Convert.ToHexString(SHA256.HashData(protectedContent));

                await fileExportAuditService.RecordGeneratedFileAsync(
                    artifact.CycleId,
                    artifact.ClearingHouseId,
                    "RETURN",
                    encryptedFileName,
                    artifact.RecordCount,
                    artifact.ReturnCount,
                    true,
                    artifact.TransactionIds,
                    protectedHash,
                    ct);
                existingExport = await context.AchFileExports
                    .Include(x => x.TransmissionAttempts)
                    .SingleAsync(x => x.AchCycleId == artifact.CycleId
                                      && x.ExportKind == "RETURN"
                                      && x.FileName == encryptedFileName
                                      && x.IsEncrypted, ct);
            }
            else
            {
                var persistedPayload = existingExport.TransmissionAttempts
                    .Where(x => x.ProtectedContent.Length > 0)
                    .OrderByDescending(x => x.AttemptNumber)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("El artefacto cifrado persistido no contiene payload reintentable; requiere revisión manual.");
                protectedContent = persistedPayload.ProtectedContent;
                protectedHash = persistedPayload.ContentSha256;
            }

            encryptedExport = existingExport;
            var duplicateAttempt = encryptedExport.TransmissionAttempts
                .SingleOrDefault(x => x.IdempotencyKey == idempotencyKey);
            if (duplicateAttempt is not null)
            {
                if (stageTransaction is not null)
                {
                    await stageTransaction.CommitAsync(ct);
                }
                return MapAttempt(encryptedExport, duplicateAttempt, true);
            }

            if (encryptedExport.LifecycleStatus is AchFileExportLifecycleStatus.Transmitted
                or AchFileExportLifecycleStatus.Acknowledged
                or AchFileExportLifecycleStatus.Accepted
                or AchFileExportLifecycleStatus.Rejected)
            {
                var lastSuccessful = encryptedExport.TransmissionAttempts
                    .OrderByDescending(x => x.AttemptNumber)
                    .First(x => x.Status == AchFileTransmissionAttemptStatus.Succeeded);
                if (stageTransaction is not null)
                {
                    await stageTransaction.CommitAsync(ct);
                }
                return MapAttempt(encryptedExport, lastSuccessful, true);
            }

            attempt = new AchFileTransmissionAttempt
            {
                AchFileExportId = encryptedExport.Id,
                AttemptNumber = encryptedExport.TransmissionAttempts.Select(x => x.AttemptNumber).DefaultIfEmpty().Max() + 1,
                IdempotencyKey = idempotencyKey,
                Status = AchFileTransmissionAttemptStatus.Started,
                StartedAtUtc = DateTime.UtcNow,
                ResultCode = "STARTED",
                ResultSummary = "Handoff Return Out iniciado.",
                ContentSha256 = protectedHash,
                ProtectedContent = protectedContent
            };
            context.AchFileTransmissionAttempts.Add(attempt);
            await context.SaveChangesAsync(ct);
            if (stageTransaction is not null)
            {
                await stageTransaction.CommitAsync(ct);
            }
        }
        catch (DbUpdateException) when (allowStageRaceRecovery)
        {
            if (stageTransaction is not null)
            {
                await stageTransaction.RollbackAsync(CancellationToken.None);
            }
            context.ChangeTracker.Clear();
            return await DispatchArtifactAsync(artifact, idempotencyKey, actor, ct, false);
        }
        catch
        {
            if (stageTransaction is not null)
            {
                await stageTransaction.RollbackAsync(CancellationToken.None);
            }
            throw;
        }

        var transportResult = await transportAdapter.TransmitAsync(new AchOutboundReturnTransportRequest(
            encryptedExport.Id,
            artifact.ClearingHouseId,
            encryptedFileName,
            protectedContent,
            protectedHash,
            idempotencyKey), ct);
        attempt.CompletedAtUtc = transportResult.OccurredAtUtc;
        attempt.Retryable = transportResult.Retryable;
        attempt.ResultCode = Trim(transportResult.ResultCode, 60);
        attempt.ResultSummary = Trim(transportResult.ResultSummary, 500);
        attempt.ExternalReference = TrimNullable(transportResult.ExternalReference, 120);
        attempt.Status = transportResult.Succeeded
            ? AchFileTransmissionAttemptStatus.Succeeded
            : transportResult.Retryable
                ? AchFileTransmissionAttemptStatus.FailedRetryable
                : AchFileTransmissionAttemptStatus.FailedFinal;
        await context.SaveChangesAsync(ct);

        if (transportResult.Succeeded)
        {
            await evidenceRecorder.RecordAsync(new AchFileTransmissionEvidence(
                encryptedExport.Id,
                AchFileExportLifecycleStatus.Transmitted,
                transportResult.ExternalReference!,
                transportResult.OccurredAtUtc), ct);
            await context.Entry(encryptedExport).ReloadAsync(ct);
        }

        return MapAttempt(encryptedExport, attempt, false);
    }

    private async Task<int> ResolveClearingHouseIdAsync(string cycleId, CancellationToken ct)
        => await context.AchCycles
            .Where(x => x.Id == cycleId)
            .Select(x => x.ClearingHouseId)
            .SingleAsync(ct);

    private static AchOutboundReturnDispatchResult MapAttempt(
        AchFileExport export,
        AchFileTransmissionAttempt attempt,
        bool wasDuplicate)
        => new(
            export.Id,
            export.FileName,
            export.LifecycleStatus,
            attempt.Status == AchFileTransmissionAttemptStatus.Succeeded,
            attempt.Retryable,
            wasDuplicate,
            attempt.ResultCode,
            attempt.ResultSummary,
            attempt.ExternalReference,
            attempt.AttemptNumber);

    private static void ValidateRequest(string idempotencyKey, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (idempotencyKey.Length > 128)
        {
            throw new ArgumentException("La clave de idempotencia excede 128 caracteres.", nameof(idempotencyKey));
        }
    }

    private static string Trim(string value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    private static string? TrimNullable(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Trim(value, maxLength);
}
