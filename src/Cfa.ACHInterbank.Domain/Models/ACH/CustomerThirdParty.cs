using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CustomerThirdParty : AuditableEntity
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int DestinationInstitutionId { get; set; }
    public FinancialInstitution DestinationInstitution { get; set; } = null!;

    public string DestinationAccountNumber { get; set; } = null!;
    public string RecipientIdNumber { get; set; } = null!;

    public CustomerThirdPartyStatusEnum Status { get; set; } = CustomerThirdPartyStatusEnum.Pending;

    public int? PrenotificationTransactionId { get; set; }
    public AchTransaction? PrenotificationTransaction { get; set; }

    public string? ValidationCycleId { get; set; }
    public DateTime? ValidationReceivedAt { get; set; }
    public string? ValidationMessage { get; set; }
}
