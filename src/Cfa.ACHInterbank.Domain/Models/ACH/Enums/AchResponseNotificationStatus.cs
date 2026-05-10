namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

public enum AchResponseNotificationStatus
{
    Pendiente = 1,
    Exitosa = 2,
    ErrorFuncional = 3,
    ErrorTecnico = 4,
    PendienteReintento = 5,
    RequiereRevisionManual = 6
}
