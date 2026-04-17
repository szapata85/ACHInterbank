using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaTransactionLinker
{
    Task<IncomingNachaLinkingResult> LinkAsync(EntryDetail entry, AddendaRecord? addenda, CancellationToken ct = default);
}
