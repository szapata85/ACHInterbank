using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchTraceabilityService
{
    Task<AchTransaction> CertifySol02Async(
        int transactionId,
        string? certificationReference,
        string? notes,
        CancellationToken ct = default);

    Task<AchTraceabilityDetailDto?> GetTransactionTraceabilityAsync(int transactionId, CancellationToken ct = default);

    Task<IReadOnlyList<AchTraceabilityReportRowDto>> GetTraceabilityReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        AchTransferStateEnum? state,
        string? achCycleId,
        CancellationToken ct = default);
}
