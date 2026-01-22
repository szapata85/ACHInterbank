namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IWscfaachSoapClient
{
    Task<string> PLValidarUsuarioBVAsync(string requestXml, CancellationToken ct = default);
    Task<string> ProcContrapartidasAsync(string requestXml, CancellationToken ct = default);
    Task<string> ProcTransaccionesAsync(string requestXml, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ProcTransaccionesParallelAsync(
        IEnumerable<string> requestXmls,
        int degreeOfParallelism = 4,
        CancellationToken ct = default);
}
