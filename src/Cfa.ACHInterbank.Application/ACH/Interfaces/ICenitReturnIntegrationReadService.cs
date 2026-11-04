using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// I06-CENIT: inspección/conciliación diagnóstica; no publica, transmite, confirma
/// ni muta una devolución. No permite convertir un acuse ajeno en confirmación I05.
/// </summary>
public interface ICenitReturnIntegrationReadService
{
    Task<CenitReturnIntegrationDiagnostic?> GetAsync(Guid preparationId, CancellationToken ct = default);
    Task<CenitReturnIntegrationPage> ListAsync(int maxItems = 100, CancellationToken ct = default);
}
