using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class WafMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    public WafMiddleware(RequestDelegate requestDelegate)
    {
        _requestDelegate = requestDelegate;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        if (IsRequestMalicious(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Petición Bloqueada por el Waf.");
            return;
        }

        await _requestDelegate(context);

        InspectResponse(context.Response);
    }

    private bool IsRequestMalicious(HttpRequest request)
    {
        // Implementa reglas personalizadas para detectar ataques (e.g., SQLi, XSS)
        var queryString = request.QueryString.Value ?? "";
        if (queryString.Contains("' OR 1=1", StringComparison.OrdinalIgnoreCase) ||
            queryString.Contains("--", StringComparison.OrdinalIgnoreCase) ||
            queryString.Contains(";", StringComparison.OrdinalIgnoreCase) ||
            queryString.Contains("/*", StringComparison.OrdinalIgnoreCase))
        {
            return true; // Ataque SQL detectado
        }

        if (queryString.Contains("<script>", StringComparison.OrdinalIgnoreCase) ||
            queryString.Contains("</script>", StringComparison.OrdinalIgnoreCase) ||
            queryString.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return true; // XSS detectado
        }



        return false; // Solicitud limpia
    }

    private void InspectResponse(HttpResponse response)
    {
        var responseBody = response.Body.ToString();
        if (responseBody!.Contains("<script>", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            response.WriteAsync("Respuesta bloqueada por el WAF debido a contenido malicioso.");
        }

    }

}
