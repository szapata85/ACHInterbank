using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;

public interface IAchResponseIdempotencyHashService
{
    string BuildHash(ProcesarRespuestaAchCommand command);
}
