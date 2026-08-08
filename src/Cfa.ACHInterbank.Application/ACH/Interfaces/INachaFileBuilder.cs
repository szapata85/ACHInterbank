namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Servicio responsable de la construcción de archivos NACHA-M
/// a partir de los ciclos y transacciones registrados.
/// </summary>
public interface INachaFileBuilder
{
    /// <summary>
    /// Genera ReturnOut ACH Colombia mediante el perfil Opción C DEVOLUCION/SALIDA.
    /// La ausencia o invalidez del perfil produce error; esta ruta no tiene fallback legacy.
    /// </summary>
    Task<Cfa.ACHInterbank.Application.ACH.Models.NachaReturnOutBuildResult> BuildReturnOutAsync(
        Cfa.ACHInterbank.Application.ACH.Models.NachaReturnOutBuildRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Genera un archivo NACHA-M a partir de un conjunto de lotes específicos.
    /// </summary>
    /// <param name="batchIds">Lista de identificadores de lotes a exportar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Contenido completo del archivo NACHA-M en formato texto plano.</returns>
    Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct = default);

    /// <summary>
    /// Genera un archivo NACHA-M basado en un ciclo de procesamiento (ACHCycle).
    /// </summary>
    /// <param name="cycleId">Identificador del ciclo ACH.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Archivo NACHA-M en formato plano concatenando registros 1-9.</returns>
    Task<string> BuildNachaFileByCycleAsync(string cycleId, CancellationToken ct = default);

    /// <summary>
    /// Genera el archivo y devuelve la membresía exacta utilizada en esa versión.
    /// </summary>
    Task<Cfa.ACHInterbank.Application.ACH.Models.NachaFileBuildArtifact> BuildNachaFileArtifactByCycleAsync(string cycleId, CancellationToken ct = default);

    /// <summary>
    /// Construye un registro NACHA-M individual de acuerdo con su layout configurado.
    /// </summary>
    /// <typeparam name="T">Entidad del dominio correspondiente al tipo de registro.</typeparam>
    /// <param name="recordType">Código de registro (1, 5, 6, 7, 8, 9).</param>
    /// <param name="entity">Entidad a mapear en el layout.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cadena formateada del registro conforme a NACHA-M.</returns>
    Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct = default);
}
