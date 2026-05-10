using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

public sealed record ProcesarRespuestaAchResult(
    Guid? AchResponseId,
    bool Procesada,
    bool Duplicada,
    bool ExisteHomologacion,
    bool PermiteNotificacion,
    bool IntentoPendienteCreado,
    AchResponseProcessingStatus EstadoProcesamiento,
    string? Motivo,
    string? HashIdempotencia);
