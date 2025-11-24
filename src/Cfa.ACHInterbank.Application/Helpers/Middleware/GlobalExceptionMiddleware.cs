using System.Net;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggerManager _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILoggerManager logger)
    {
        _next = next;
        _logger = logger;
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ResponseApiService.Response(context.Response.StatusCode, null!, exception.Message);
        var payload = JsonConvert.SerializeObject(response);

        return context.Response.WriteAsync(payload);
    }
}
