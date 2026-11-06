using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Desconexion del cliente: no es una excepcion no controlada del servidor.
        }
        catch (Exception exception)
        {
            var incidentId = Guid.NewGuid().ToString("N");
            var ruleId = exception is NachaGenerationException nacha
                ? nacha.RuleId ?? nacha.Code
                : "UNHANDLED";

            // Una sola emision estructurada; NLog registra mensaje, excepcion y stack trace.
            // No incluir cuerpos, query strings, tokens ni rutas con datos personales.
            _logger.LogError(
                exception,
                "Unhandled exception. Incident={IncidentId}; ErrorType={ErrorType}; RuleId={RuleId}; TraceId={TraceId}; Method={Method}",
                incidentId,
                exception.GetType().Name,
                ruleId,
                context.TraceIdentifier,
                context.Request.Method);

            if (context.Response.HasStarted || !context.Response.Body.CanWrite)
            {
                // Ya no es posible construir un JSON 500: permitir el cierre de la conexion.
                throw;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Conservar el contrato de respuesta actual para errores de generacion NACHA.
            var safeMessage = exception is NachaGenerationException generationException
                ? generationException.Message
                : $"Error interno. Incidente {incidentId}.";
            var response = ResponseApiService.Response(context.Response.StatusCode, null, safeMessage);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}
