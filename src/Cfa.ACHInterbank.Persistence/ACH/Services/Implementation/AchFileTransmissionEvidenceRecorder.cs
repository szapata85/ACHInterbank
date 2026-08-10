using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchFileTransmissionEvidenceRecorder(AchDbContext context) : IAchFileTransmissionEvidenceRecorder
{
    public async Task RecordAsync(AchFileTransmissionEvidence evidence, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidence.ExternalReference);
        if (evidence.OccurredAtUtc == default)
        {
            throw new ArgumentException("La evidencia de transporte requiere fecha verificable.", nameof(evidence));
        }

        var export = await context.AchFileExports
            .SingleOrDefaultAsync(x => x.Id == evidence.AchFileExportId, ct)
            ?? throw new InvalidOperationException("No existe el archivo de salida asociado a la evidencia de transporte.");

        ValidateReference(export.TransmissionReference, evidence.ExternalReference);

        if (IsSameEvidence(export, evidence))
        {
            return;
        }

        ValidateTransition(export.LifecycleStatus, evidence.Status);
        export.TransmissionReference ??= evidence.ExternalReference.Trim();

        if (evidence.Status >= AchFileExportLifecycleStatus.Transmitted)
        {
            export.TransmittedAtUtc ??= evidence.OccurredAtUtc;
        }

        if (evidence.Status >= AchFileExportLifecycleStatus.Acknowledged)
        {
            if (string.IsNullOrWhiteSpace(evidence.AcknowledgementCode))
            {
                throw new InvalidOperationException("El resultado requiere un código de acuse verificable.");
            }

            export.AcknowledgedAtUtc ??= evidence.OccurredAtUtc;
            export.AcknowledgementCode = evidence.AcknowledgementCode.Trim();
        }

        export.LifecycleStatus = evidence.Status;
        await context.SaveChangesAsync(ct);
    }

    private static bool IsSameEvidence(
        AchFileExport export,
        AchFileTransmissionEvidence evidence)
        => export.LifecycleStatus == evidence.Status
           && string.Equals(export.TransmissionReference, evidence.ExternalReference.Trim(), StringComparison.Ordinal)
           && (evidence.Status < AchFileExportLifecycleStatus.Acknowledged
               || string.Equals(export.AcknowledgementCode, evidence.AcknowledgementCode?.Trim(), StringComparison.Ordinal));

    private static void ValidateReference(string? current, string supplied)
    {
        if (!string.IsNullOrWhiteSpace(current)
            && !string.Equals(current, supplied.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La referencia de transporte no coincide con la evidencia ya persistida.");
        }
    }

    private static void ValidateTransition(
        AchFileExportLifecycleStatus current,
        AchFileExportLifecycleStatus target)
    {
        if (target is not (AchFileExportLifecycleStatus.Transmitted
            or AchFileExportLifecycleStatus.Acknowledged
            or AchFileExportLifecycleStatus.Accepted
            or AchFileExportLifecycleStatus.Rejected))
        {
            throw new InvalidOperationException("El estado solicitado no representa evidencia de transporte o resultado.");
        }

        if (current is AchFileExportLifecycleStatus.Accepted or AchFileExportLifecycleStatus.Rejected)
        {
            throw new InvalidOperationException("Un resultado funcional definitivo no puede reemplazarse por otro estado.");
        }

        if (target < current)
        {
            throw new InvalidOperationException("El lifecycle de transporte no admite regresiones.");
        }
    }
}
