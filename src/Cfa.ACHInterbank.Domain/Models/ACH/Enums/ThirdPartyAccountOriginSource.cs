namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

/// <summary>
/// Fuente de creación del maestro canónico. Es metadato de trazabilidad y no participa en la identidad.
/// </summary>
public enum ThirdPartyAccountOriginSource
{
    Manual = 1,
    SingleTransaction = 2,
    BulkTransaction = 3,
    Backfill = 4
}
