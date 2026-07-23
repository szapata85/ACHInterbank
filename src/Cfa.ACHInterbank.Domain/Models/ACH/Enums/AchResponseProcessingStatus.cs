namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum AchResponseProcessingStatus
{
    Recibida = 1,
    Homologada = 2,
    Notificada = 3,
    ErrorFuncional = 4,
    PendienteReintento = 5,
    RequiereRevisionManual = 6,
    NoHomologada = 7,
    Duplicada = 8,
    PendienteCorrelacion = 9,
    Huerfana = 10,
    EnRevision = 11,
    Resuelta = 12,
    Rechazada = 13,
    ErrorTecnico = 14,
    PendienteReproceso = 15,
    Reprocesando = 16,
    Reprocesada = 17,
    Cerrada = 18
}
