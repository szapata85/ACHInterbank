using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Services;

public sealed class AchResponseIdempotencyHashService : IAchResponseIdempotencyHashService
{
    public string BuildHash(ProcesarRespuestaAchCommand command)
    {
        var key = string.Join("|", new[]
        {
            command.TipoRespuesta.ToString(),
            Normalize(command.CodigoCamaraCompensacion),
            Normalize(command.IdTransaccion),
            Normalize(command.CodigoEstadoExterno),
            NormalizeNullable(command.CodigoCausalExterna) ?? string.Empty,
            command.IdTransaccionServicioExterno.ToString(),
            NormalizeNullable(command.CodigoEntidadOrigen) ?? string.Empty,
            NormalizeNullable(command.CodigoEntidadDestino) ?? string.Empty
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash);
    }

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
