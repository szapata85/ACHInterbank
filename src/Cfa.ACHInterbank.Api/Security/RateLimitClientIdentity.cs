using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Api.Security;

/// <summary>
/// Identidad del limitador global. Fuera de Development, X-Forwarded-For solamente
/// se acepta si el socket de origen pertenece a la lista explícita de proxies confiables.
/// </summary>
public static class RateLimitClientIdentity
{
    public static string GetClientKey(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        var peerIp = remote is null ? "unknown" : Normalize(remote).ToString() ?? "unknown";

        var environment = context.RequestServices.GetService<IHostEnvironment>();
        var configuration = context.RequestServices.GetService<IConfiguration>();
        var dev = environment?.IsDevelopment() == true;
        var trusted = dev || IsExplicitlyTrusted(remote, configuration);

        if (trusted && context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            // Only first hop; nginx SEC-I05 overwrites the client-supplied header.
            var firstHop = forwarded.ToString().Split(',', 2)[0].Trim();
            if (IPAddress.TryParse(firstHop, out var parsed))
            {
                return Normalize(parsed).ToString();
            }
        }

        return peerIp;
    }

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    private static bool IsExplicitlyTrusted(IPAddress? peer, IConfiguration? config)
    {
        if (peer is null || config is null) return false;
        var normalized = Normalize(peer);
        var proxies = config.GetSection("Security:TrustedReverseProxyIps").Get<string[]>() ?? [];
        foreach (var configured in proxies)
        {
            if (IPAddress.TryParse(configured, out var allowed)
                && normalized.Equals(Normalize(allowed))) return true;
        }
        return false;
    }
}
