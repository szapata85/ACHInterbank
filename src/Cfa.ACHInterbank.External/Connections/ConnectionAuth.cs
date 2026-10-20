using System.Diagnostics;
using System.ServiceModel;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using wsAutorizadorOM;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class ConnectionAuth : IConnectionAuthScoped
{
    private readonly ILoggerManager _logger;
    private readonly ILoadBalancer _loadBalancer;
    private readonly WcfAuthorizationOptions _wcfOptions;

    // These operations can reach authorization backends with side effects. Transport retry is intentionally disabled.
    private readonly Dictionary<string, Func<IinicoClient, string, string>> _requestHandlers;

    public ConnectionAuth(ILoggerManager logger, ILoadBalancer loadBalancer, IOptions<WcfAuthorizationOptions> wcfOptions)
    {
        _logger = logger;
        _loadBalancer = loadBalancer;
        _wcfOptions = wcfOptions?.Value ?? throw new ArgumentNullException(nameof(wcfOptions));
        _requestHandlers = new Dictionary<string, Func<IinicoClient, string, string>>
        {
            { "WCFBM", (client, input) => ExecuteWCF(client, input) },
            { "WCFPSE", (client, input) => ExecuteWCFPSE(client, input) },
            { "WCFTransfiya", (client, input) => ExecuteWCFTransfiya(client, input) },
            { "WCFRedeban", (client, input) => ExecuteWCFRedeban(client, input) }
        };
    }

    public async Task<string> StartWCF(string data, string requestType = "WCFBM")
    {
        if (!_requestHandlers.TryGetValue(requestType, out var handler))
        {
            _logger.LogError(
                $"WCF authorization failure; operation={requestType}; classification=definitive-invalid-operation; " +
                "retry=disabled; payload=redacted.");
            return string.Empty;
        }

        IinicoClient? client = null;
        var started = Stopwatch.GetTimestamp();

        try
        {
            var timeoutAutorizador = TimeSpan.FromMinutes(ParseTimeoutMinutes());
            var endpoint = await _loadBalancer.GetNextServer("servicesWCF").ConfigureAwait(false);
            var address = new EndpointAddress(endpoint.Url);
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = 2147483647,
                MaxBufferSize = 2147483647,
                CloseTimeout = timeoutAutorizador,
                SendTimeout = timeoutAutorizador,
                ReceiveTimeout = timeoutAutorizador,
                OpenTimeout = timeoutAutorizador
            };

            _logger.LogInfo(
                $"WCF authorization request; operation={requestType}; endpoint={ExternalLogSanitizer.Endpoint(endpoint.Url ?? string.Empty)}; " +
                $"timeoutSeconds={timeoutAutorizador.TotalSeconds:F0}; retry=disabled; payload=redacted.");

            client = new IinicoClient(binding, address);
            var result = await Task.Run(() => handler(client, data)).ConfigureAwait(false);
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            _logger.LogInfo(
                $"WCF authorization response; operation={requestType}; elapsedMs={elapsedMs:F0}; status=completed; " +
                "retry=disabled; response=redacted.");
            return result;
        }
        catch (TimeoutException ex)
        {
            LogFailure(requestType, started, ex, "transient-outcome-uncertain");
            return "timeout";
        }
        catch (CommunicationException ex)
        {
            LogFailure(requestType, started, ex, ClassifyCommunicationFailure(ex));
            return string.Empty;
        }
        catch (Exception ex)
        {
            LogFailure(requestType, started, ex, "definitive-or-unknown");
            return string.Empty;
        }
        finally
        {
            CloseClient(client);
        }
    }

    private void LogFailure(string requestType, long started, Exception exception, string classification)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _logger.LogError(
            $"WCF authorization failure; operation={requestType}; elapsedMs={elapsedMs:F0}; " +
            $"classification={classification}; exception={exception.GetType().Name}; retry=disabled; payload=redacted.");
    }

    private static string ClassifyCommunicationFailure(CommunicationException exception)
        => exception is EndpointNotFoundException or ServerTooBusyException
            ? "transient-outcome-uncertain"
            : exception is FaultException
                ? "definitive-remote-fault"
                : "communication-outcome-uncertain";

    private static void CloseClient(IinicoClient? client)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            if (client.State == CommunicationState.Faulted)
            {
                client.Abort();
            }
            else
            {
                client.Close();
            }
        }
        catch
        {
            client.Abort();
        }
    }

    private int ParseTimeoutMinutes()
    {
        if (!int.TryParse(_wcfOptions.TimeoutMinutes, out var minutes) || minutes <= 0)
        {
            throw new InvalidOperationException("appSettings:prueba debe configurar un timeout WCF positivo en minutos.");
        }

        return minutes;
    }

    private static string ExecuteWCF(IinicoClient client, string input)
    {
        CompositeType parametros = new() { JsonInput = input };
        return client.FnPrincipalBancaMovil(parametros);
    }

    private static string ExecuteWCFPSE(IinicoClient client, string input)
    {
        ParamPSE parametrosPSE = new() { InputPSE = input };
        return client.FnPSE(parametrosPSE);
    }

    private static string ExecuteWCFTransfiya(IinicoClient client, string input)
    {
        ParamTransfiya parametrosTransfiya = new() { InputTransfiya = input };
        return client.FnTransfiya(parametrosTransfiya);
    }

    private static string ExecuteWCFRedeban(IinicoClient client, string input)
    {
        ParamRedeban parametrosRedeban = new() { InputRedeban = input };
        return client.FnRedeban(parametrosRedeban);
    }
}
