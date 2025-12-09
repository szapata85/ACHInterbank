using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    public SecurityHeadersMiddleware(RequestDelegate requestDelegate)
    {
        _requestDelegate = requestDelegate;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("X-AspNet-Version");
            context.Response.Headers.Remove("Server");

            // Añade los headers de seguridad
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=63072000");
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self', frame-src 'self', script-src 'self';");
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,PATCH,DELETE,OPTIONS";
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("Referrer-Policy", "no-referrer-when-downgrade");
            context.Response.Headers.Add("Feature-Policy", "geolocation 'self'");
            context.Response.Headers.Add("Public-Key-Pins", "pin-sha256=&quot;y6FNDn9EJFGbn7i0a5olrWh7ECZKlFTl7Pfavyjwtg8=&quot;; pin-sha256=&quot;YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=&quot;; pin-sha256=&quot;Vjs8r4z+80wjNcr1YKepWQboSIRi63WsWXhIMN+eWys=&quot;; max-age=31536000; includeSubDomains");
            return Task.CompletedTask;
        });
        await _requestDelegate(context);

    }

}


