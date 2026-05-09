namespace Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;

public sealed record NotificarRespuestaAchCommand(long NotificationAttemptId, string? CorrelationId);
