using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// Estado de validación operacional de una cuenta canónica dentro de un contexto de cámara.
/// No forma parte de la identidad del maestro ThirdPartyAccount.
/// </summary>
public class ThirdPartyAccountValidation : AuditableEntity
{
    private string _serviceType = string.Empty;

    public long Id { get; set; }
    public long ThirdPartyAccountId { get; set; }
    public ThirdPartyAccount ThirdPartyAccount { get; set; } = null!;

    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;

    public TransactionNature TransactionNature { get; set; }

    /// <summary>Standard Entry Class / service discriminator when the effective rule needs it; empty otherwise.</summary>
    public string ServiceType
    {
        get => _serviceType;
        set => _serviceType = NormalizeServiceType(value);
    }

    public ThirdPartyAccountValidationType ValidationType { get; set; } = ThirdPartyAccountValidationType.Prenotification;
    public ThirdPartyAccountValidationStatus Status { get; set; } = ThirdPartyAccountValidationStatus.NotStarted;
    public long? CurrentAttemptId { get; set; }

    public int? PolicyRuleId { get; set; }
    public ClearingHouseTransactionRule? PolicyRule { get; set; }
    public DateTime? PolicyEffectiveFrom { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? LastMonetaryUseAtUtc { get; set; }

    public ICollection<PrenotificationValidationAttempt> Attempts { get; set; } = new List<PrenotificationValidationAttempt>();

    public static string NormalizeServiceType(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
}
