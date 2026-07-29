using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouseTransactionRule : AuditableEntity
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public TransactionNature TransactionNature { get; set; }
    public TransactionTypeEnum TransactionType { get; set; }
    public bool RequiresPrenotification { get; set; }
    public PrenotificationRequirementMode PrenotificationMode { get; set; }
    public int? PrenotificationLeadBusinessDays { get; set; }
    public bool RequiresReceiverIdentificationValidation { get; set; }
    public ValidationRequirementMode ReceiverIdentificationValidationMode { get; set; }
    public bool AppliesToNachaExport { get; set; } = true;
    public bool AppliesToMonetaryTransactions { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string NormativeSource { get; set; } = string.Empty;
    public string NormativeReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool IsEffective(DateTime date)
        => IsActive
           && EffectiveFrom.Date <= date.Date
           && (!EffectiveTo.HasValue || EffectiveTo.Value.Date >= date.Date);
}
