using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingNachaAchResultRequest(
    int ClearingHouseId,
    string Code,
    string FlowType,
    bool IsDebit,
    bool IsCredit,
    bool IsPrenotification,
    bool IsReturn,
    DateTime ProcessedAtUtc);

public sealed record IncomingNachaAchResultResolution(
    bool IsResolved,
    int? AchReturnCodeId,
    string ResultCode,
    string ResultDescription,
    IncomingNachaBusinessOutcome BusinessOutcome,
    string ResolutionCode);
