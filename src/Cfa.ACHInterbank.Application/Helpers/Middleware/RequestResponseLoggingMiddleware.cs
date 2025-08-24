using System;
using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly ILoggerManager _loggerManager;

    public RequestResponseLoggingMiddleware(RequestDelegate requestDelegate, ILoggerManager loggerManager)
    {
        _requestDelegate = requestDelegate;
        _loggerManager = loggerManager;
    }

    public async Task Invoke(HttpContext context)
    {
        // Capturar el Request (Body y QueryString)
        context.Request.EnableBuffering();

        var requestBody = "";
        if (context.Request.ContentLength > 0) // Verifica si hay Body antes de leerlo.
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
        var LogTransactional = string.Empty;
        LogTransactional = $"Solicitud \n Method -> {context.Request.Method}";
        LogTransactional += $"\n Request Uri -> {context.Request.Path}";
        LogTransactional += $"\n Datos -> {requestData}";
        LogTransactional += $"\n Cabeceras -> {JsonConvert.SerializeObject(context.Request.Headers)}";
        _loggerManager.LogInfo(LogTransactional);

        LogTransactional = $"Respuesta \n Method -> {context.Request.Method}";
        LogTransactional += $"\n Request Uri -> {context.Request.Path}";
        LogTransactional += $"\n Datos -> {responseBody}";
        LogTransactional += $"\n Cabeceras -> {JsonConvert.SerializeObject(context.Request.Headers)}";
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
}
