namespace Cfa.ACHInterbank.Domain.Models.ACH.Enums;

/// <summary>
/// Estado administrativo del maestro canónico de terceros/cuentas.
/// No representa validación ni elegibilidad interbancaria.
/// </summary>
public enum ThirdPartyAdministrativeStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3
}
