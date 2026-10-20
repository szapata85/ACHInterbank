using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaPostParseProcessor
{
    Task ProcessAsync(Guid ingestionId, string executedBy, CancellationToken ct = default);
    Task<IncomingNachaLinkedReturnApplicationResult> ApplyLinkedReturnAsync(
        Guid incomingNachaTransactionLinkId,
        string executedBy,
        CancellationToken ct = default);
}
