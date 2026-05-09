using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;

public interface IRegistrarRespuestaAchCommandMapper
{
    RegistrarRespuestaAchCommand Map(AchResponse response, AchResponseNotificationAttempt attempt);
}
