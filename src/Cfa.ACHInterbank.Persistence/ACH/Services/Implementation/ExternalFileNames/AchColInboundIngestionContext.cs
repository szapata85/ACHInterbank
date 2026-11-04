using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

/// <summary>
/// F2.3: persists the *validated* ACH Colombia physical-file context on its ingestion.
/// Does not create, infer or modify a domain AchCycle and does not authorize SOAP dispatch.
/// </summary>
internal static class AchColInboundIngestionContext
{
    internal static void ApplyValidated(
        IncomingNachaFileIngestion ingestion,
        AchColInboundFileNameParser.AchColInboundFileName validated)
    {
        ArgumentNullException.ThrowIfNull(ingestion);
        ArgumentNullException.ThrowIfNull(validated);
        if (!string.Equals(ingestion.FileName, validated.OriginalPhysicalFileName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "El nombre físico validado no corresponde a la ingesta persistida.", nameof(validated));
        }

        ingestion.CanonicalPlainFileName = validated.CanonicalPlainFileName;
        ingestion.FileIdModifier = validated.FileIdModifier.ToString();
        ingestion.FileSequence = validated.FileSequence;
        ingestion.ExternalCycleNumber = validated.ExternalCycleNumber;
        ingestion.FileProtectionKind = validated.ProtectionKind.ToString();
        ingestion.FileNameDate = DateTime.SpecifyKind(
            validated.FileDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        // Intentionally do not set DetectedCycleNumber, ResolvedAchCycleId, or
        // CycleResolutionStatus: only admission/resolver owns those values.
    }
}
