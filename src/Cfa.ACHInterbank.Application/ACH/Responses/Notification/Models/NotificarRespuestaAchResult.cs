using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;

public sealed record NotificarRespuestaAchResult(
    bool Procesada,
    bool Encontrada,
    bool YaProcesada,
    bool ExisteError,
    bool ErrorTecnico,
    AchResponseNotificationStatus? EstadoNotificacion,
    AchResponseProcessingStatus? EstadoProcesamiento,
    string? CodigoError,
    string? DescripcionError,
    string? ErrorTecnicoDetalle,
    string? Motivo);
