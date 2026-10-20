using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// Maestro canónico de un tercero perteneciente a un cliente.
/// Su identidad es agnóstica de cámara y contexto transaccional.
/// </summary>
public class ThirdParty : AuditableEntity
{
    private string _personType = null!;
    private string _documentType = null!;
    private string _documentNumberNormalized = null!;
    private string _displayName = null!;

    public long Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string PersonType
    {
        get => _personType;
        set => _personType = NormalizeCatalogCode(value, nameof(PersonType));
    }
    public PersonTypeCatalog PersonTypeCatalog { get; set; } = null!;

    public string DocumentType
    {
        get => _documentType;
        set => _documentType = NormalizeCatalogCode(value, nameof(DocumentType));
    }
    public DocumentTypeCatalog DocumentTypeCatalog { get; set; } = null!;

    /// <summary>
    /// Número de documento normalizado para identidad/matching.
    /// Normalización objetivo: Trim + ToUpperInvariant, consistente con matching legacy actual.
    /// </summary>
    public string DocumentNumberNormalized
    {
        get => _documentNumberNormalized;
        set => _documentNumberNormalized = NormalizeDocumentNumber(value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => _displayName = NormalizeDisplayName(value);
    }

    /// <summary>
    /// Estado exclusivamente administrativo del maestro. No expresa validación ACH/CENIT.
    /// </summary>
    public ThirdPartyAdministrativeStatus Status { get; set; } = ThirdPartyAdministrativeStatus.Active;

    public ICollection<ThirdPartyAccount> Accounts { get; set; } = new List<ThirdPartyAccount>();

    public static string NormalizeDocumentNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El número de documento del tercero es obligatorio.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }

    public static string NormalizeCatalogCode(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El código de catálogo es obligatorio.", parameterName);
        }

        return value.Trim().ToUpperInvariant();
    }

    public static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El nombre visible del tercero es obligatorio.", nameof(value));
        }

        return value.Trim();
    }
}
