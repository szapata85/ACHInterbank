using System;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class RequestResponseLoggingMiddleware
{
    private const int MaxLoggedBodyLength = 4096;
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "x-api-key"
    };
    private static readonly HashSet<string> SensitiveBodyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "client_secret",
        "clientSecret",
        "secretKetJwt",
        "token",
        "refreshToken",
        "accessToken"
    };

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

        var requestBody = "";
        if (ShouldReadBody(context.Request.ContentType, context.Request.ContentLength))
        {
            requestBody = await ReadStreamAsync(context.Request.Body);
        }

        var queryString = context.Request.Query;

        // Copiar el stream del Response para capturarlo
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Continuar con el pipeline
        await _requestDelegate(context);

        // Capturar el Response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await ReadStreamAsync(context.Response.Body);
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        var requestData = requestBody != string.Empty ? requestBody : JsonConvert.SerializeObject(queryString);
        requestData = RedactBody(requestData);
        responseBody = RedactBody(responseBody);
        var sanitizedHeaders = JsonConvert.SerializeObject(RedactHeaders(context.Request.Headers));

        var LogTransactional = string.Empty;
        LogTransactional = $"Solicitud \n Method -> {context.Request.Method}";
        LogTransactional += $"\n Request Uri -> {context.Request.Path}";
        LogTransactional += $"\n Datos -> {Truncate(requestData)}";
        LogTransactional += $"\n Cabeceras -> {sanitizedHeaders}";
        _loggerManager.LogInfo(LogTransactional);

        LogTransactional = $"Respuesta \n Method -> {context.Request.Method}";
        LogTransactional += $"\n Request Uri -> {context.Request.Path}";
        LogTransactional += $"\n Datos -> {Truncate(responseBody)}";
        LogTransactional += $"\n Cabeceras -> {sanitizedHeaders}";
        _loggerManager.LogInfo(LogTransactional);

        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    }

    private static bool ShouldReadBody(string? contentType, long? contentLength)
    {
        if (contentLength is null or <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        stream.Seek(0, SeekOrigin.Begin);
        return content;
    }

    private static IDictionary<string, string> RedactHeaders(IHeaderDictionary headers)
    {
        return headers.ToDictionary(
            header => header.Key,
            header => SensitiveHeaders.Contains(header.Key) ? "***REDACTED***" : header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string RedactBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        try
        {
            var token = JToken.Parse(body);
            RedactToken(token);
            return token.ToString(Formatting.None);
        }
        catch
        {
            return body;
        }
    }

    private static void RedactToken(JToken token)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                if (SensitiveBodyKeys.Contains(property.Name))
                {
                    property.Value = "***REDACTED***";
                    continue;
                }

                RedactToken(property.Value);
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
            {
                RedactToken(item);
            }
        }
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxLoggedBodyLength)
        {
            return value;
        }

        return $"{value[..MaxLoggedBodyLength]}...<truncated>";
    }
}
