namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFileBuilder
{
    Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct);
    Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct);

    /// <summary>
    /// Genera un archivo NACHA-M (106 caracteres por registro) para un ciclo específico.
    /// Incluye encabezados, lotes, transacciones y addendas.
    /// </summary>
    Task<string> BuildNachaFileByCycleAsync(int cycleId, CancellationToken ct = default);
}
