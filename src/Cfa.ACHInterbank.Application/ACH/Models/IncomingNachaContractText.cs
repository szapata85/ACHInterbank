using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public static class IncomingNachaContractText
{
    public static string IngestionStatus(IncomingNachaIngestionStatus value) => value switch
    {
        IncomingNachaIngestionStatus.Recibido => "Recibido",
        IncomingNachaIngestionStatus.Duplicado => "Archivo duplicado",
        IncomingNachaIngestionStatus.EnValidacion => "Validando archivo",
        IncomingNachaIngestionStatus.PendienteResolucion => "Pendiente de resolución",
        IncomingNachaIngestionStatus.ListoParaParseo => "Listo para interpretar",
        IncomingNachaIngestionStatus.Parseado => "Contenido interpretado",
        IncomingNachaIngestionStatus.Bloqueado => "Requiere atención",
        IncomingNachaIngestionStatus.Fallido => "Error técnico",
        IncomingNachaIngestionStatus.Completado => "Carga completada",
        _ => "Estado no disponible"
    };

    public static string Stage(IncomingNachaIngestionStage value) => value switch
    {
        IncomingNachaIngestionStage.Received => "Archivo recibido",
        IncomingNachaIngestionStage.PreValidating => "Validando información inicial",
        IncomingNachaIngestionStage.Decrypting => "Descifrando archivo",
        IncomingNachaIngestionStage.HeaderParsing => "Leyendo encabezado",
        IncomingNachaIngestionStage.ValidatingHeader => "Validando encabezado",
        IncomingNachaIngestionStage.ValidatingCycle => "Validando ciclo",
        IncomingNachaIngestionStage.Parsing => "Interpretando contenido",
        IncomingNachaIngestionStage.ValidatingContent => "Validando transacciones",
        IncomingNachaIngestionStage.Persisting => "Guardando información",
        IncomingNachaIngestionStage.Persisted => "Carga completada",
        IncomingNachaIngestionStage.Rejected => "Rechazado",
        IncomingNachaIngestionStage.Failed => "Error técnico",
        _ => "Etapa no disponible"
    };

    public static string QueueStatus(IncomingNachaDispatchQueueStatus? value) => value switch
    {
        IncomingNachaDispatchQueueStatus.Queued => "Pendiente de programación",
        IncomingNachaDispatchQueueStatus.Dispatching => "Enviando al servicio",
        IncomingNachaDispatchQueueStatus.Dispatched => "Enviado al servicio",
        IncomingNachaDispatchQueueStatus.Confirmed => "Procesado",
        IncomingNachaDispatchQueueStatus.RetryPending => "Pendiente de reintento",
        IncomingNachaDispatchQueueStatus.FailedFinal => "Falló el procesamiento",
        IncomingNachaDispatchQueueStatus.Blocked => "Requiere atención",
        IncomingNachaDispatchQueueStatus.WaitingWindow => "Esperando ventana operativa",
        _ => "Pendiente de programación"
    };

    public static string ProcessingStatus(IncomingNachaIndividualProcessingStatus? value) => value switch
    {
        IncomingNachaIndividualProcessingStatus.Scheduled => "Programado",
        IncomingNachaIndividualProcessingStatus.Processing => "Procesando",
        IncomingNachaIndividualProcessingStatus.Completed => "Procesado",
        IncomingNachaIndividualProcessingStatus.RetryPending => "Pendiente de reintento",
        IncomingNachaIndividualProcessingStatus.TechnicalFailed => "Error técnico",
        _ => "Pendiente"
    };

    public static string BusinessOutcome(IncomingNachaBusinessOutcome? value) => value switch
    {
        IncomingNachaBusinessOutcome.Successful => "Exitoso",
        IncomingNachaBusinessOutcome.Rejected => "Rechazado",
        IncomingNachaBusinessOutcome.Returned => "Devuelto",
        IncomingNachaBusinessOutcome.NotProcessed => "No procesado",
        _ => "Pendiente de respuesta"
    };

    public static string TransportStatus(IntegrationTransportStatus value) => value switch
    {
        IntegrationTransportStatus.Succeeded => "Respuesta recibida",
        IntegrationTransportStatus.TimedOut => "Tiempo de espera agotado",
        IntegrationTransportStatus.Failed => "Error de comunicación",
        _ => "No ejecutado"
    };
}
