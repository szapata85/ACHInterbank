using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// Cuenta canónica perteneciente a un tercero. La cámara, naturaleza, servicio,
/// prenotificación, ciclos y SLA no forman parte de esta entidad ni de su identidad.
/// </summary>
public class ThirdPartyAccount : AuditableEntity
{
    private string _accountNumberNormalized = null!;

    public long Id { get; set; }

    public long ThirdPartyId { get; set; }
    public ThirdParty ThirdParty { get; set; } = null!;

    public int FinancialInstitutionId { get; set; }
    public FinancialInstitution FinancialInstitution { get; set; } = null!;

    public AccountTypeEnum AccountType { get; set; }

    /// <summary>
    /// Número de cuenta normalizado para identidad/matching.
    /// Se conserva únicamente Trim para no inventar transformaciones distintas del matching legacy actual.
    /// </summary>
    public string AccountNumberNormalized
    {
        get => _accountNumberNormalized;
        set => _accountNumberNormalized = NormalizeAccountNumber(value);
    }

    /// <summary>
    /// Estado exclusivamente administrativo del maestro. No expresa validación ACH/CENIT.
    /// </summary>
    public ThirdPartyAdministrativeStatus Status { get; set; } = ThirdPartyAdministrativeStatus.Active;

    /// <summary>
    /// Metadato opcional de procedencia. No participa en identidad ni elegibilidad.
    /// </summary>
    public ThirdPartyAccountOriginSource? OriginSource { get; set; }

    public ICollection<ThirdPartyAccountValidation> Validations { get; set; } = new List<ThirdPartyAccountValidation>();

    public static string NormalizeAccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El número de cuenta del tercero es obligatorio.", nameof(value));
        }

        return value.Trim();
    }
}
