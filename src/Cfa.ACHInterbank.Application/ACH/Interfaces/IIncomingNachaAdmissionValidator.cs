using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaAdmissionValidator
{
    Task<IncomingNachaAdmissionResult> ValidateAsync(
        IncomingNachaAdmissionRequest request,
        CancellationToken ct = default);
}
