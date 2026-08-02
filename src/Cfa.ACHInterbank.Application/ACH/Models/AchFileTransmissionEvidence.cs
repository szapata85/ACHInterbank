using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchFileTransmissionEvidence(
    int AchFileExportId,
    AchFileExportLifecycleStatus Status,
    string ExternalReference,
    DateTime OccurredAtUtc,
    string? AcknowledgementCode = null);
