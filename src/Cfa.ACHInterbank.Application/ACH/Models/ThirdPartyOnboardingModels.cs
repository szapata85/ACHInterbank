using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;


/// <summary>
/// Entrada de master data para asegurar únicamente la identidad canónica de un tercero.
/// Deliberadamente no contiene cuenta, cámara ni contexto operacional.
/// </summary>
public sealed record ThirdPartyMasterRequest(
    int CustomerId,
    string DocumentType,
    string DocumentNumber,
    string DisplayName,
    string PersonType);

/// <summary>Referencia estable al tercero canónico asegurado.</summary>
public sealed record ThirdPartyRef(
    long ThirdPartyId,
    bool ThirdPartyCreated,
    bool RecoveredFromUniqueConflict);

/// <summary>
/// Entrada de master data para asegurar un tercero y su cuenta canónica.
/// Deliberadamente no contiene cámara, naturaleza, servicio, prenotificación, ciclo ni SLA.
/// </summary>
public sealed record ThirdPartyOnboardingRequest(
    int CustomerId,
    string DocumentType,
    string DocumentNumber,
    string DisplayName,
    string PersonType,
    int FinancialInstitutionId,
    AccountTypeEnum AccountType,
    string AccountNumber,
    ThirdPartyAccountOriginSource? SourceMetadata = null);

/// <summary>
/// Referencia estable al maestro canónico asegurado por el onboarding.
/// </summary>
public sealed record ThirdPartyAccountRef(
    long ThirdPartyId,
    long ThirdPartyAccountId,
    bool ThirdPartyCreated,
    bool ThirdPartyAccountCreated,
    bool RecoveredFromUniqueConflict);
