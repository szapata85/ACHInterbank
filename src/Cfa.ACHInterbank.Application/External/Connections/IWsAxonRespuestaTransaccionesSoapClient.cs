namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IWsAxonRespuestaTransaccionesSoapClient
{
    Task<string> RegistrarRespuestaTransaccionAsync(string requestXml, CancellationToken ct = default);
    Task<string> RegistrarRespuestaTransaccionAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> RegistrarRespuestaTransaccionParallelAsync(
        IEnumerable<string> requestXmls,
        int degreeOfParallelism = 4,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> RegistrarRespuestaTransaccionParallelAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> parameterSets,
        int degreeOfParallelism = 4,
        CancellationToken ct = default);
}
