using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class FinancialInstitution : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsDefaultSource { get; set; } = false;

    // Cámara principal “actual” (puede usarse para compatibilidad)
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;

    // 🔹 Preferencias dinámicas para múltiples cámaras
    public ICollection<InstitutionClearingHousePreference> ClearingHousePreferences { get; set; }
        = new List<InstitutionClearingHousePreference>();

    // 🔹 Desglose de ruta y dígito de chequeo
    public string RoutingNumber { get; set; } = null!;
    public string TransitCode { get; set; } = null!;
    public string CheckDigit { get; private set; } = null!;

    // 🔹 Estado de la entidad (activa, inactiva, retirada)
    public FinancialInstitutionStatus Status { get; set; } = FinancialInstitutionStatus.Active;

    public ICollection<AchTransaction> SourceTransactions { get; set; } = new List<AchTransaction>();
    public ICollection<AchTransaction> DestinationTransactions { get; set; } = new List<AchTransaction>();

    // ✅ Cálculo de dígito de chequeo
    public void CalculateCheckDigit()
    {
        CheckDigit = Helpers.DigitoChequeoHelper
            .CalcularDigitoChequeo($"{RoutingNumber}{TransitCode}");
    }
}
