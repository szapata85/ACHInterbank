using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;

public interface IDifferentialPrenotificationResponseProcessor
{
    Task<DifferentialPrenotificationResponseProcessResult> ProcessAsync(
        ProcesarRespuestaAchCommand command,
        AchResponse response,
        HomologarRespuestaAchResult homologation,
        CancellationToken cancellationToken = default);
}
