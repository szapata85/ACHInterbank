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

    public CustomerThirdPartyStatusEnum Status { get; private set; } = CustomerThirdPartyStatusEnum.Pending;

    public int? PrenotificationTransactionId { get; set; }
    public AchTransaction? PrenotificationTransaction { get; set; }

    public string? ValidationCycleId { get; private set; }
    public DateTime? ValidationReceivedAt { get; private set; }
    public string? ValidationMessage { get; private set; }

    public void LinkPendingPrenotification(AchTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (Status != CustomerThirdPartyStatusEnum.Pending)
        {
            throw new InvalidOperationException(
                "Una prenotificación resuelta no puede volver a estado pendiente.");
        }

        PrenotificationTransaction = transaction;
        PrenotificationTransactionId = transaction.Id > 0 ? transaction.Id : null;
        ValidationCycleId = null;
        ValidationReceivedAt = null;
        ValidationMessage = null;
    }

    public bool ApplyAutomaticNachaResult(
        CustomerThirdPartyStatusEnum result,
        int? prenotificationTransactionId,
        string? validationCycleId,
        DateTime receivedAtUtc,
        string validationMessage,
        string evidenceReference)
    {
        if (result is not (CustomerThirdPartyStatusEnum.Active or CustomerThirdPartyStatusEnum.Rejected))
        {
            throw new ArgumentOutOfRangeException(nameof(result),
                "El procesamiento NACHA-M solo puede producir un resultado final.");
        }

        if (string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new ArgumentException("El resultado automático requiere una descripción.", nameof(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException("El resultado automático requiere evidencia de procesamiento.", nameof(evidenceReference));
        }

        if (PrenotificationTransactionId.HasValue
            && prenotificationTransactionId.HasValue
            && PrenotificationTransactionId.Value != prenotificationTransactionId.Value)
        {
            throw new InvalidOperationException(
                "La respuesta no corresponde a la prenotificación vinculada al tercero.");
        }

        if (Status != CustomerThirdPartyStatusEnum.Pending)
        {
            return false;
        }

        Status = result;
        if (prenotificationTransactionId.HasValue)
        {
            PrenotificationTransactionId = prenotificationTransactionId;
        }

        ValidationCycleId = string.IsNullOrWhiteSpace(validationCycleId)
            ? null
            : validationCycleId.Trim();
        ValidationReceivedAt = receivedAtUtc.ToUniversalTime();
        ValidationMessage = validationMessage.Trim();
        return true;
    }
}
