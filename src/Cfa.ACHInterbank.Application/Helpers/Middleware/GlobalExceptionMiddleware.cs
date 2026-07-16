using System.Net;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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
            var incidentId = Guid.NewGuid().ToString("N");
            var ruleId = ex is NachaGenerationException nachaException
                ? nachaException.RuleId ?? nachaException.Code
                : "UNHANDLED";
            _logger.LogError($"Unhandled exception. Incident={incidentId}; ErrorType={ex.GetType().Name}; RuleId={ruleId}");
            await WriteCrashLogAsync(context, ex, incidentId, ruleId);
            await HandleExceptionAsync(context, ex, incidentId);
        }
    }

    private async Task WriteCrashLogAsync(HttpContext context, Exception exception, string incidentId, string ruleId)
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
                .AppendLine($"Incident: {incidentId}")
                .AppendLine($"ErrorType: {exception.GetType().Name}")
                .AppendLine($"RuleId: {ruleId}")
                .AppendLine($"Method: {context.Request.Method}")
                .AppendLine($"Path: {context.Request.Path}")
                .AppendLine(new string('-', 80))
                .ToString();

            await File.AppendAllTextAsync(logPath, message);
        }
        catch
        {
            // Avoid throwing from logging.
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string incidentId)
    {
        if (context.Response.HasStarted || context.Response.Body == null || !context.Response.Body.CanWrite)
        {
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var safeMessage = exception is NachaGenerationException nachaException
            ? nachaException.Message
            : $"Error interno. Incidente {incidentId}.";
        var response = ResponseApiService.Response(context.Response.StatusCode, null!, safeMessage);
        var payload = JsonConvert.SerializeObject(response);

        return context.Response.WriteAsync(payload);
    }
}
