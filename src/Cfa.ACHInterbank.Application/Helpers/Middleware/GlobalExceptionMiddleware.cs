using System.Net;
using System.Text;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggerManager _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILoggerManager logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception: {ex.Message} - {ex.StackTrace}");
            await WriteCrashLogAsync(context, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task WriteCrashLogAsync(HttpContext context, Exception exception)
    {
        try
        {
            var logPath = Path.Combine(_environment.ContentRootPath, "crash.log");
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var message = new StringBuilder()
                .AppendLine($"[{DateTimeOffset.UtcNow:O}] Unhandled exception")
                .AppendLine($"Method: {context.Request.Method}")
                .AppendLine($"Path: {context.Request.Path}")
                .AppendLine($"Query: {context.Request.QueryString}")
                .AppendLine($"Message: {exception.Message}")
                .AppendLine(exception.StackTrace)
                .AppendLine(new string('-', 80))
                .ToString();

            await File.AppendAllTextAsync(logPath, message);
        }
        catch
        {
            // Avoid throwing from logging.
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted || context.Response.Body == null || !context.Response.Body.CanWrite)
        {
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ResponseApiService.Response(context.Response.StatusCode, null!, exception.Message);
        var payload = JsonConvert.SerializeObject(response);

        return context.Response.WriteAsync(payload);
    }
}
