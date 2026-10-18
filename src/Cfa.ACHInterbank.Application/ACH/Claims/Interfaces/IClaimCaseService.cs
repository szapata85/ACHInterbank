using Cfa.ACHInterbank.Application.ACH.Claims.Models;

namespace Cfa.ACHInterbank.Application.ACH.Claims.Interfaces;

public interface IClaimCaseService
{
    Task<ClaimCaseDetailDto> CreateDev14Async(
        CreateDev14ClaimCaseRequest request,
        string? idempotencyKey,
        CancellationToken ct = default);

    Task<ClaimCaseDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ClaimCaseQueryResult> QueryAsync(
        ClaimCaseQuery query,
        CancellationToken ct = default);

    Task<IReadOnlyList<ClaimCaseListItemDto>> GetByTransactionAsync(
        int transactionId,
        CancellationToken ct = default);

    Task<ClaimCaseDetailDto> RecordProgressAsync(
        int id,
        RecordClaimProgressRequest request,
        string actor,
        string? idempotencyKey,
        CancellationToken ct = default);

    Task<ClaimCaseDetailDto> ResolveAsync(
        int id,
        ResolveClaimCaseRequest request,
        string actor,
        string? idempotencyKey,
        CancellationToken ct = default);

    Task<ClaimCaseDetailDto> ExpireAsync(
        int id,
        ExpireClaimCaseRequest request,
        string actor,
        string? idempotencyKey,
        CancellationToken ct = default);
}
