using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

public sealed record AchResponseCorrelationResult(
    AchResponseCorrelationStatus Status,
    int? AchTransactionId,
    string Criterion,
    IReadOnlyList<int> CandidateTransactionIds);
