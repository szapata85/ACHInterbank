using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaDispatchEligibilityPolicy
{
    Task<IncomingNachaDispatchEligibilityResult> EvaluateAsync(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        IncomingNachaTransactionLink link,
        AchTransaction transaction,
        DateTime nowLocal,
        CancellationToken ct = default);
}
