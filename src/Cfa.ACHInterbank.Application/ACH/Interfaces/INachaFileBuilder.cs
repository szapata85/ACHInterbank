using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFileBuilder
{
    /// <summary>
    /// Genera una línea de registro NACHA a partir de la entidad indicada,
    /// usando la configuración en la BD para el tipo de registro.
    /// </summary>
    /// <typeparam name="T">Entidad transaccional o DTO</typeparam>
    /// <param name="recordType">Tipo de registro (HEADER, ENTRY_DETAIL, etc.)</param>
    /// <param name="entity">Instancia con los datos</param>
    /// <param name="ct">Token de cancelación</param>
    Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct);

    /// <summary>
    /// Genera el archivo completo concatenando registros, sin saltos de línea,
    /// a partir de las transacciones y su información relacionada.
    /// </summary>
    Task<string> BuildNachaFileAsync(IEnumerable<AchTransaction> transactions, CancellationToken ct);
}

