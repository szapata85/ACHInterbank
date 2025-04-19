using Cfa.ACHInterbank.Application.Features;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class TokenJwtMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    public TokenJwtMiddleware(RequestDelegate requestDelegate)
    {
        _requestDelegate = requestDelegate;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _requestDelegate(context);
        if (context.Response.StatusCode == 401 & context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.ContentType = "application/json";
            var response = ResponseApiService.Response(StatusCodes.Status401Unauthorized, "Token expirado no válido", "Unauthorized");
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        if (context.Response.StatusCode == 403)
        {
            context.Response.ContentType = "application/json";

            var response = ResponseApiService.Response(StatusCodes.Status403Forbidden, "Válide los parametros con los que está realizando la solicitud, servicio denegado", "Forbidden");
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

    }
}
