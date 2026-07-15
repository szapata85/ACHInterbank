using System;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class RequestResponseLoggingMiddleware
{
    private const string Redacted = "[REDACTED]";
    private static readonly string[] SensitiveNames = ["password", "pass", "token", "authorization", "cookie", "secret", "privatekey", "rawdata", "pfx"];
    private readonly RequestDelegate _requestDelegate;
    private readonly ILoggerManager _loggerManager;

    public RequestResponseLoggingMiddleware(RequestDelegate requestDelegate, ILoggerManager loggerManager)
    {
        _requestDelegate = requestDelegate;
        _loggerManager = loggerManager;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestData = await ReadSanitizedRequestAsync(context.Request);
        var requestHeaders = SanitizeHeaders(context.Request.Headers);

        // Copiar el stream del Response para capturarlo
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Continuar con el pipeline
        await _requestDelegate(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await ReadStreamAsync(context.Response.Body);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var sanitizedResponseBody = SanitizePayload(responseBody, context.Response.ContentType);

        var LogTransactional = string.Empty;
        LogTransactional = $"Solicitud \n Method -> {context.Request.Method}";
        LogTransactional += $"\n Request Uri -> {context.Request.Path}";
        LogTransactional += $"\n Datos -> {requestData}";
        LogTransactional += $"\n Cabeceras -> {JsonConvert.SerializeObject(requestHeaders)}";
        _loggerManager.LogInfo(LogTransactional);

        LogTransactional = $"Respuesta \n Method -> {context.Request.Method}";
        LogTransactional += $"\n Request Uri -> {context.Request.Path}";
        LogTransactional += $"\n Datos -> {sanitizedResponseBody}";
        _loggerManager.LogInfo(LogTransactional);

        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    }

    private async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        stream.Seek(0, SeekOrigin.Begin);
        return content;
    }

    private async Task<string> ReadSanitizedRequestAsync(HttpRequest request)
    {
        if (request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "[MULTIPART CONTENT OMITTED]";
        }

        if (request.ContentLength is > 0)
        {
            var body = await ReadStreamAsync(request.Body);
            return SanitizePayload(body, request.ContentType);
        }

        var query = request.Query.ToDictionary(
            item => item.Key,
            item => IsSensitiveName(item.Key) ? Redacted : item.Value.ToString());
        return JsonConvert.SerializeObject(query);
    }

    private static Dictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
    {
        return headers.ToDictionary(
            item => item.Key,
            item => IsSensitiveName(item.Key) ? Redacted : item.Value.ToString());
    }

    private static string SanitizePayload(string payload, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
        if (contentType?.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "[BINARY CONTENT OMITTED]";
        }

        try
        {
            var token = JToken.Parse(payload);
            RedactToken(token);
            return token.ToString(Formatting.None);
        }
        catch (JsonReaderException)
        {
            return payload.Length <= 2048 ? payload : $"{payload[..2048]}[TRUNCATED]";
        }
    }

    private static void RedactToken(JToken token)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties().ToList())
            {
                if (IsSensitiveName(property.Name))
                {
                    property.Value = Redacted;
                }
                else
                {
                    RedactToken(property.Value);
                }
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array) RedactToken(item);
        }
    }

    private static bool IsSensitiveName(string name)
        => SensitiveNames.Any(sensitive => name.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
}
