using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.External.Connections;

internal static class ControlledLocalSoapTransportResolver
{
    private static readonly HashSet<string> ControlledLocalHosts =
        new(["localhost", "127.0.0.1", "host.docker.internal"], StringComparer.OrdinalIgnoreCase);

    public static ControlledLocalSoapTransportResolution Resolve(
        Uri logicalEndpoint,
        ControlledLocalSoapTransportOptions? options,
        string? legacyHostHeader = null)
    {
        ArgumentNullException.ThrowIfNull(logicalEndpoint);

        var transportHost = options?.TransportHost?.Trim();
        var hostHeader = string.IsNullOrWhiteSpace(options?.HostHeader)
            ? legacyHostHeader?.Trim()
            : options.HostHeader.Trim();

        if (string.IsNullOrWhiteSpace(transportHost))
        {
            ValidateHostHeader(logicalEndpoint, hostHeader);
            return new ControlledLocalSoapTransportResolution(logicalEndpoint, hostHeader);
        }

        if (!ControlledLocalHosts.Contains(logicalEndpoint.IdnHost)
            || !ControlledLocalHosts.Contains(transportHost)
            || Uri.CheckHostName(transportHost) == UriHostNameType.Unknown)
        {
            throw new InvalidOperationException(
                "SOAP ControlledLocal transport alias must map between authorized local hosts.");
        }

        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            throw new InvalidOperationException(
                "SOAP ControlledLocal transport alias requires an explicit logical Host header.");
        }

        ValidateHostHeader(logicalEndpoint, hostHeader);

        var transportEndpoint = new UriBuilder(logicalEndpoint)
        {
            Host = transportHost
        }.Uri;

        return new ControlledLocalSoapTransportResolution(transportEndpoint, hostHeader);
    }

    private static void ValidateHostHeader(Uri logicalEndpoint, string? hostHeader)
    {
        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            return;
        }

        if (hostHeader.IndexOfAny(['/', '\\', '@', '?', '#', '\r', '\n']) >= 0
            || !Uri.TryCreate($"{logicalEndpoint.Scheme}://{hostHeader}", UriKind.Absolute, out var hostUri)
            || !ControlledLocalHosts.Contains(logicalEndpoint.IdnHost)
            || !ControlledLocalHosts.Contains(hostUri.IdnHost)
            || hostUri.Port != logicalEndpoint.Port)
        {
            throw new InvalidOperationException(
                "SOAP Host header override is outside the ControlledLocal transport policy.");
        }
    }
}

internal sealed record ControlledLocalSoapTransportResolution(
    Uri TransportEndpoint,
    string? HostHeader);
