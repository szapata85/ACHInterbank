namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Fuente declarada para una solicitud de ingestión masiva.
/// </summary>
public enum BulkIngestionSourceType
{
    /// <summary>Transacciones incluidas directamente en el request; es la fuente admitida actualmente por el modo síncrono.</summary>
    InlineTransactions = 1,

    /// <summary>Fuente declarada como archivo JSON; el flujo síncrono actual no la procesa.</summary>
    JsonFile = 2,

    /// <summary>Fuente declarada como archivo CSV; el flujo síncrono actual no la procesa.</summary>
    CsvFile = 3,

    /// <summary>Fuente declarada como archivo Excel; el flujo síncrono actual no la procesa.</summary>
    ExcelFile = 4
}

/// <summary>
/// Modalidad solicitada para la ingestión masiva.
/// </summary>
public enum BulkIngestionProcessingMode
{
    /// <summary>Procesa las transacciones inline durante la misma solicitud.</summary>
    Synchronous = 1,

    /// <summary>Modalidad declarada como procesamiento asíncrono; la implementación actual responde con estado NOT_IMPLEMENTED y no crea un job.</summary>
    AsynchronousJob = 2
}

/// <summary>
/// Solicitud de ingestión masiva que selecciona fuente y modalidad de procesamiento.
/// </summary>
public sealed class BulkIngestionRequest
{
    /// <summary>Referencia de lote trasladada al registro masivo cuando se ejecuta el flujo síncrono inline.</summary>
    public string BatchReference { get; set; } = string.Empty;

    /// <summary>Fuente declarada para los datos de la solicitud.</summary>
    public BulkIngestionSourceType SourceType { get; set; } = BulkIngestionSourceType.InlineTransactions;

    /// <summary>Modalidad de procesamiento solicitada.</summary>
    public BulkIngestionProcessingMode ProcessingMode { get; set; } = BulkIngestionProcessingMode.Synchronous;

    /// <summary>Tamaño de bloque opcional trasladado al servicio de registro masivo.</summary>
    public int? ChunkSize { get; set; }

    /// <summary>Transacciones inline procesadas cuando SourceType es InlineTransactions y la modalidad es síncrona.</summary>
    public List<BulkAchTransactionItemRequest>? Transactions { get; set; }

    /// <summary>Nombre de archivo reservado en el contrato para fuentes basadas en archivo; el flujo actual de este endpoint no lo procesa.</summary>
    public string? FileName { get; set; }

    /// <summary>Content type reservado en el contrato para fuentes basadas en archivo; el flujo actual de este endpoint no lo procesa.</summary>
    public string? ContentType { get; set; }

    /// <summary>Contenido Base64 reservado en el contrato para fuentes basadas en archivo; el flujo actual de este endpoint no lo procesa.</summary>
    public string? ContentBase64 { get; set; }

    /// <summary>Identificador de solicitud presente en el contrato; el flujo actual no lo utiliza.</summary>
    public string? ClientRequestId { get; set; }

    /// <summary>Conteo de reintentos presente en el contrato; el flujo actual no lo utiliza.</summary>
    public int? RetryCount { get; set; }
}

/// <summary>
/// Resultado de una solicitud de ingestión masiva según la modalidad ejecutada.
/// </summary>
public sealed class BulkIngestionResponse
{
    /// <summary>Modalidad que determinó el resultado devuelto.</summary>
    public BulkIngestionProcessingMode ProcessingMode { get; set; } = BulkIngestionProcessingMode.Synchronous;

    /// <summary>Identificador de job cuando exista uno; la modalidad asíncrona actual no crea un job y devuelve este valor nulo.</summary>
    public string? JobId { get; set; }

    /// <summary>Estado textual producido por el flujo actual, por ejemplo COMPLETED o NOT_IMPLEMENTED.</summary>
    public string? Status { get; set; }

    /// <summary>Resultado inmediato del registro masivo cuando la solicitud síncrona procesa transacciones inline.</summary>
    public BulkAchTransactionResponse? ImmediateResult { get; set; }
}
