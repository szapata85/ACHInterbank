using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class BulkFileUploadRequest
{
    public string? BatchReference { get; set; }
    public string? ClientRequestId { get; set; }
    public string? RequestedBy { get; set; }
}

/// <summary>
/// Resultado de la carga y validación estructural inicial de un archivo de ingestión masiva.
/// </summary>
public sealed class BulkFileUploadResponse
{
    /// <summary>Identificador del lote creado o del lote existente devuelto por coincidencia de ClientRequestId y hash.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Identificador del último job despachado para el lote cuando existe; puede ser nulo si el lote no fue encolado.</summary>
    public string? JobId { get; set; }

    /// <summary>Referencia efectiva del lote, suministrada por el cliente o generada por el servicio cuando no se informó una.</summary>
    public string BatchReference { get; set; } = string.Empty;

    /// <summary>Estado actual del lote al momento de responder la carga.</summary>
    public BulkIngestionBatchStatusEnum Status { get; set; } = BulkIngestionBatchStatusEnum.Uploaded;

    /// <summary>Tipo de archivo resuelto por el servicio para seleccionar el parser.</summary>
    public BulkIngestionFileTypeEnum FileType { get; set; } = BulkIngestionFileTypeEnum.Unknown;

    /// <summary>Cantidad total de registros detectados por el parser del archivo.</summary>
    public int TotalRecordsDetected { get; set; }

    /// <summary>Cantidad de registros clasificados como StructuralError durante la validación estructural inicial.</summary>
    public int TotalStructuralErrors { get; set; }

    /// <summary>Cantidad de registros estructuralmente válidos que quedaron listos para procesamiento.</summary>
    public int TotalReadyForProcessing { get; set; }

    /// <summary>Resumen persistido de errores estructurales detectados en la carga; la carga inicial conserva hasta 20 mensajes distintos.</summary>
    public IReadOnlyList<string> ErrorSummary { get; set; } = [];
}

public sealed class ParsedRawItem
{
    public int Index { get; set; }
    public IReadOnlyDictionary<string, string?> Fields { get; set; } = new Dictionary<string, string?>();
}

public sealed class ParsedFileResult
{
    public BulkIngestionFileTypeEnum FileType { get; set; } = BulkIngestionFileTypeEnum.Unknown;
    public List<ParsedRawItem> Items { get; set; } = [];
}

public sealed class StructuralValidationOutcome
{
    public int Index { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public BulkAchTransactionItemRequest? NormalizedItem { get; set; }
    public IReadOnlyDictionary<string, string?> Fields { get; set; } = new Dictionary<string, string?>();
}
