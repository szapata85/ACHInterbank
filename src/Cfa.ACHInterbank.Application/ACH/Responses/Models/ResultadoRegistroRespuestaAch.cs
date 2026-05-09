namespace Cfa.ACHInterbank.Application.ACH.Responses.Models;

public sealed record ResultadoRegistroRespuestaAch(
    bool ExisteError,
    string? CodigoError,
    string? DescripcionError)
{
    public bool Exitoso => !ExisteError;
}
