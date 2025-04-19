using System.ServiceModel;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using wsAutorizadorOM;

namespace Cfa.ACHInterbank.External.Connections;

public class ConnectionAuthScoped : IConnectionAuthScoped
{
    private readonly ILoggerManagerTransient _logger;
    private readonly ILoadBalancerSingleton _loadBalancer;
    private readonly AppSettings _appSettings = AppSettings.Settings;

    // Diccionario que asocia el tipo de solicitud con su respectiva función
    private readonly Dictionary<string, Func<IinicoClient, string, string>> _requestHandlers;

    public ConnectionAuthScoped(ILoggerManagerTransient logger, ILoadBalancerSingleton loadBalancer)
    {
        _logger = logger;
        _loadBalancer = loadBalancer;
        // Inicializamos el diccionario con los tipos de solicitudes (Diccionario de delegados)
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
        string ReturnStartClientSocket = string.Empty;
        try
        {
            _logger.LogInfo($"datos enviados al autorizador: {data}");

            TimeSpan timeoutAutorizador = TimeSpan.FromMinutes(int.Parse(_appSettings.prueba!));
            var end = await _loadBalancer.GetNextServer("servicesWCF");
            EndpointAddress address = new EndpointAddress(end.Url);
            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = 2147483647,
                MaxBufferSize = 2147483647,
                CloseTimeout = timeoutAutorizador,
                SendTimeout = timeoutAutorizador,
                ReceiveTimeout = timeoutAutorizador,
                OpenTimeout = timeoutAutorizador
            };

            IinicoClient obj_IinicoClient = new(binding, address);
            ReturnStartClientSocket = await Task.Run(() => _requestHandlers[requestType](obj_IinicoClient, data));

            obj_IinicoClient.Close();

            _logger.LogInfo($"Respuesta del autorizador {ReturnStartClientSocket}");

        }
        catch (TimeoutException toex)
        {
            _logger.LogError($"{toex.Message} StartWCF");
            ReturnStartClientSocket = "timeout";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + " StartWCF");
        }
        return ReturnStartClientSocket;
    }

    private string ExecuteWCF(IinicoClient client, string input)
    {
        CompositeType parametros = new() { JsonInput = input };
        return client.FnPrincipalBancaMovil(parametros);
    }

    private string ExecuteWCFPSE(IinicoClient client, string input)
    {
        ParamPSE parametrosPSE = new() { InputPSE = input };
        return client.FnPSE(parametrosPSE);
    }

    private string ExecuteWCFTransfiya(IinicoClient client, string input)
    {
        ParamTransfiya parametrosTransfiya = new() { InputTransfiya = input };
        return client.FnTransfiya(parametrosTransfiya);
    }

    private string ExecuteWCFRedeban(IinicoClient client, string input)
    {
        ParamRedeban parametrosRedeban = new() { InputRedeban = input };
        return client.FnRedeban(parametrosRedeban);
    }

}
