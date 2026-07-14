using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Prepara, exclusivamente en el entorno LIVE local autorizado, la contraparte y
/// transacción interna mínima para enlazar una entrada NACHA-M entrante.
/// </summary>
public interface IIncomingNachaLocalLivePreparationService
{
    Task EnsureAsync(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        IncomingNachaFunctionalClass functionalClass,
        CancellationToken ct = default);
}
