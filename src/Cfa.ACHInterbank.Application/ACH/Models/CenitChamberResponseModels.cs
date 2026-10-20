using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using System.Text.Json.Serialization;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Datos recibidos por la operación que importa una respuesta de cámara CENIT.</summary>
/// <param name="SourceResponseId">Identificador de origen obligatorio; el servicio elimina espacios exteriores y conserva como máximo 128 caracteres.</param>
/// <param name="SourceFileName">Nombre simple obligatorio del archivo de respuesta; no admite componentes de ruta y el servicio conserva como máximo 180 caracteres.</param>
/// <param name="MessageType">Tipo de mensaje obligatorio. El servicio reconoce XML, Reconciliation/Reconciliacion y NoActivity/SinActividad después de normalizar mayúsculas, guiones y guiones bajos.</param>
/// <param name="Content">Contenido recibido. Un valor nulo se procesa como cadena vacía; su interpretación depende de MessageType.</param>
/// <param name="ReceivedAtUtc">Fecha y hora de recepción; no puede tener el valor DateTime predeterminado y el servicio la normaliza a UTC.</param>
/// <param name="RelatedOutboundFileName">Nombre simple opcional del archivo saliente relacionado; cuando se informa se usa para correlación y se conserva como máximo 180 caracteres.</param>
/// <param name="RelatedReference">Referencia opcional usada para correlación cuando no se resuelve por nombre de archivo; se conserva como máximo 120 caracteres.</param>
/// <param name="TransactionTraceNumber">Número de traza opcional usado para correlacionar una transacción dentro del archivo; se conserva como máximo 20 caracteres.</param>
/// <param name="AchCycleId">Identificador opcional de ciclo usado para correlación; se conserva como máximo 40 caracteres y es requerido por el servicio para respuestas de sesión.</param>
public sealed record CenitChamberResponseImportCommand(
    string SourceResponseId,
    string SourceFileName,
    string MessageType,
    string? Content,
    DateTime ReceivedAtUtc,
    string? RelatedOutboundFileName = null,
    string? RelatedReference = null,
    string? TransactionTraceNumber = null,
    string? AchCycleId = null);

/// <summary>Resultado persistido o recuperado al procesar una respuesta de cámara CENIT.</summary>
/// <param name="Id">Identificador de la respuesta persistida.</param>
/// <param name="IsDuplicate">Indica que la operación recuperó una respuesta existente en lugar de crear un nuevo registro.</param>
/// <param name="SourceResponseId">Identificador de origen normalizado de la respuesta.</param>
/// <param name="SourceFileName">Nombre normalizado del archivo de respuesta.</param>
/// <param name="RawTechnicalReference">Referencia técnica persistida formada a partir del archivo, identificador de origen y secuencia del ítem.</param>
/// <param name="ResponseType">Clasificación de tipo de respuesta determinada por el servicio.</param>
/// <param name="State">Estado resultante persistido para la respuesta.</param>
/// <param name="CorrelationOutcome">Resultado de la correlación con ciclo, archivo o transacción.</param>
/// <param name="RelatedFileId">Identificador del archivo exportado relacionado cuando la correlación lo determina.</param>
/// <param name="RelatedFileName">Nombre del archivo relacionado, derivado del archivo correlacionado o del nombre informado en la solicitud.</param>
/// <param name="AchCycleId">Identificador del ciclo relacionado cuando fue informado o resuelto por correlación.</param>
/// <param name="XmlNamespace">Namespace del documento XML cuando fue extraído por el parser.</param>
/// <param name="MessageGroupId">Identificador de grupo extraído del encabezado XML cuando existe.</param>
/// <param name="MessageStatus">Estado textual extraído del mensaje XML cuando existe.</param>
/// <param name="MessageCreatedAtUtc">Fecha de creación extraída del mensaje XML y convertida a UTC cuando puede interpretarse.</param>
/// <param name="OriginatingSender">Emisor extraído de las referencias adicionales del XML cuando existe.</param>
/// <param name="RelatedReference">Referencia relacionada informada por la solicitud o extraída del mensaje.</param>
/// <param name="RelatedTransactionId">Identificador interno de la transacción relacionada cuando la correlación por traza es única.</param>
/// <param name="TransactionTraceNumber">Número de traza informado por la solicitud o extraído del mensaje cuando existe.</param>
/// <param name="ReasonCode">Código de razón clasificado desde el mensaje cuando existe.</param>
/// <param name="Description">Descripción clasificada desde el mensaje o generada para resultados de sesión pendientes.</param>
/// <param name="ReceivedAtUtc">Fecha y hora de recepción normalizada a UTC.</param>
/// <param name="ProcessedAtUtc">Fecha y hora en que el resultado fue aplicado o procesado cuando el flujo la registra.</param>
/// <param name="IsApplied">Indica si el resultado fue aplicado al archivo o a la salida de sesión correlacionada.</param>
/// <param name="ProblemCode">Código de resultado operativo controlado cuando el procesamiento no concluye sin incidencias.</param>
/// <param name="ItemSequence">Posición de este resultado dentro de los ítems producidos a partir del mensaje recibido.</param>
/// <param name="ItemCount">Cantidad total de ítems producidos a partir del mensaje recibido.</param>
public sealed record CenitChamberResponseResult(
    Guid Id,
    bool IsDuplicate,
    string SourceResponseId,
    string SourceFileName,
    string RawTechnicalReference,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CenitChamberResponseType ResponseType,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CenitChamberResponseState State,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CenitChamberCorrelationOutcome CorrelationOutcome,
    int? RelatedFileId,
    string? RelatedFileName,
    string? AchCycleId,
    string? XmlNamespace,
    string? MessageGroupId,
    string? MessageStatus,
    DateTime? MessageCreatedAtUtc,
    string? OriginatingSender,
    string? RelatedReference,
    int? RelatedTransactionId,
    string? TransactionTraceNumber,
    string? ReasonCode,
    string? Description,
    DateTime ReceivedAtUtc,
    DateTime? ProcessedAtUtc,
    bool IsApplied,
    string? ProblemCode,
    int ItemSequence,
    int ItemCount);

/// <summary>Página de respuestas de cámara CENIT expuesta por la consulta operativa.</summary>
/// <param name="Items">Resultados de la página actual, incluidas respuestas persistidas y representaciones de archivos pendientes cuando corresponda.</param>
/// <param name="Total">Total combinado de respuestas persistidas y archivos pendientes contabilizados por el servicio.</param>
/// <param name="Page">Número de página normalizado.</param>
/// <param name="PageSize">Tamaño de página normalizado al intervalo de 1 a 200.</param>
public sealed record CenitChamberResponsePage(
    IReadOnlyList<CenitChamberResponseResult> Items,
    int Total,
    int Page,
    int PageSize);
