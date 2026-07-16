using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// Estados funcionales cuyo movimiento puede formar parte de una salida NACHA-M.
/// Los rechazos del operador o de la EPR no deben reaparecer en una exportación.
/// </summary>
public static class NachaExportEligibility
{
    public static readonly AchTransferStateEnum[] ExportableStates =
    [
        AchTransferStateEnum.Pending,
        AchTransferStateEnum.AppliedTacitly,
        AchTransferStateEnum.Certified
    ];
}
