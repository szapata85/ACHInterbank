using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IPrenotificationHandler
{
    Task HandleAsync(AchTransactionRequestData request, AchTransaction transaction, CancellationToken ct = default);
}
