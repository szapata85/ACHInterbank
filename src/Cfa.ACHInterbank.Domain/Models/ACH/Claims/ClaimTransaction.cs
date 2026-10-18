using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Domain.Models.ACH.Claims;

/// <summary>
/// Asociación persistente entre un caso Claims y una transacción ACH.
/// La entidad explícita permite ampliar posteriormente datos por transacción sin forzar 1:1.
/// </summary>
public sealed class ClaimTransaction : AuditableEntity
{
    private ClaimTransaction()
    {
    }

    public int Id { get; private set; }

    public int ClaimCaseId { get; private set; }
    public ClaimCase ClaimCase { get; private set; } = null!;

    public int AchTransactionId { get; private set; }
    public AchTransaction AchTransaction { get; private set; } = null!;

    internal static ClaimTransaction Create(ClaimCase claimCase, AchTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(claimCase);
        ArgumentNullException.ThrowIfNull(transaction);

        return new ClaimTransaction
        {
            ClaimCase = claimCase,
            ClaimCaseId = claimCase.Id,
            AchTransaction = transaction,
            AchTransactionId = transaction.Id
        };
    }
}
