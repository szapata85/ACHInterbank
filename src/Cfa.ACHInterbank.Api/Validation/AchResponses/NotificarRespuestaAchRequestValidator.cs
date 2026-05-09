using Cfa.ACHInterbank.Api.Contracts.AchResponses;

namespace Cfa.ACHInterbank.Api.Validation.AchResponses;

public sealed class NotificarRespuestaAchRequestValidator
{
    public IReadOnlyList<string> Validate(NotificarRespuestaAchRequest request)
    {
        var errors = new List<string>();
        if (request.NotificationAttemptId <= 0) errors.Add("NotificationAttemptId debe ser mayor a cero.");
        return errors;
    }
}
