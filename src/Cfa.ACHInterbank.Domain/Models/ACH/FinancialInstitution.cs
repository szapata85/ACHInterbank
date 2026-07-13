using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class FinancialInstitution : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDefaultSource { get; set; } = false;

    // 🔑 Identificación oficial
    public string RoutingNumber { get; set; } = null!;
    public string TransitCode { get; set; } = null!;
    public string CheckDigit { get; private set; } = null!;

    public FinancialInstitutionStatus Status { get; set; } = FinancialInstitutionStatus.Active;

    // Relación dinámica con las cámaras
    public ICollection<InstitutionClearingHousePreference> ClearingHousePreferences { get; set; }
        = new List<InstitutionClearingHousePreference>();

    public ICollection<AchTransaction> SourceTransactions { get; set; } = new List<AchTransaction>();
    public ICollection<AchTransaction> DestinationTransactions { get; set; } = new List<AchTransaction>();

    public void CalculateCheckDigit()
    {
        CheckDigit = Helpers.DigitoChequeoHelper
            .CalcularDigitoChequeo($"{RoutingNumber}{TransitCode}");
    }
}
