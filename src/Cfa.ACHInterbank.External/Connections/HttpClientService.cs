using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class HttpClientService : IHttpClientService
{
    private readonly ILoggerManager _logger;
    private readonly AppSettings _appSettings = AppSettings.Settings;

    public HttpClientService(ILoggerManager logger)
    {
        _logger = logger;
    }

    public async Task<T> SendPostRequestAsync<T>(string url, object content, HttpMethod method, TypeBody typeBody, string token = "")
    {
        try
        {
            T dataobject = default!;

            using var client = new HttpClient();
            using var request = new HttpRequestMessage(method, $"{url}");

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            HttpContent? httpContent = null;

            // Diferenciamos el tipo de contenido
            if (content != null)
            {
                switch (typeBody)
                {
                    case TypeBody.Query:
                        var fromData = ParseFormUrlEncodedContent(JsonSerializer.Serialize(content));
                        httpContent = new FormUrlEncodedContent(fromData);
                        break;
                    default:
                        var json = JsonSerializer.Serialize(content);
                        httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                        break;
                }
            }
            request.Content = httpContent;
            //client.DefaultRequestHeaders.Add("x-api-key", _appSettings.TokenManager!.x_api_key);
            //client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var curlCommand = GenerateCurlCommand(request);
            _logger.LogInfo($"Curl de la petición {curlCommand}");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (response.Content != null)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseContent))
                {
                    _logger.LogInfo($"Este es la respuesta del servicio: {responseContent}");
                    if (response.IsSuccessStatusCode)
                        dataobject = JsonSerializer.Deserialize<T>(responseContent)!;
                    else
                        throw new Exception(responseContent);
                }

            }

            return dataobject;
        }
        catch
        {
            throw;
        }

    }

    private HttpClient GetHttpClientWithMTLS(string serialNumberCertificate)
    {
        //Extraigo el certificado del almacen de llaves del sistema operativo
        X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        X509Certificate2? cert = store.Certificates
            .OfType<X509Certificate2>()
            .FirstOrDefault(c => c.SerialNumber.Contains(serialNumberCertificate));

        HttpClientHandler handler = new()
        {
            SslProtocols = SslProtocols.Tls13,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        handler.ClientCertificates.Add(cert!); //Agrega certificado con llave privada al objeto httpclient
        return new HttpClient(handler);
    }

    // Método para convertir el contenido de FormUrlEncodedContent
    private IEnumerable<KeyValuePair<string, string>> ParseFormUrlEncodedContent(string content)
    {
        //Deserializar JSON a un Diccionario y Convertir el Diccionario a un array de KeyValuePair<string, string>[]
        return JsonSerializer.Deserialize<Dictionary<string, string>>(content)!.ToArray(); ;
    }

    private static string GenerateCurlCommand(HttpRequestMessage request)
    {
        var command = new StringBuilder("curl");

        if (request.Method != HttpMethod.Get)
        {
            command.Append($" -X {request.Method.Method}");
        }

        if (request.Content != null)
        {

            foreach (var header in request.Headers)
            {
                command.Append($" -H '{header.Key}: {string.Join("; ", header.Value)}'");
            }

            var content = request.Content.ReadAsStringAsync().Result;
            command.Append($" -d '{content}'");
            foreach (var header in request.Content.Headers)
            {
                command.Append($" -H '{header.Key}: {string.Join("; ", header.Value)}'");
            }
        }
        command.Append($" '{request.RequestUri}'");

        return command.ToString();
    }
}
