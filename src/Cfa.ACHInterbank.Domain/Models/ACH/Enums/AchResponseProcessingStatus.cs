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
    Duplicada = 8
}
