using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNameReservationService
{
    Task<ExternalFileNameReservationResult> ReserveAsync(
        ExternalFileNameContext context,
        string requestFingerprint,
        CancellationToken ct = default);

    Task CompleteAsync(
        long reservationId,
        string externalFileName,
        char? fileIdModifier,
        CancellationToken ct = default);
}
