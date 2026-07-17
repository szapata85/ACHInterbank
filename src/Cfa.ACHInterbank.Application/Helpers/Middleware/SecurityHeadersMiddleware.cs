using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Application.Helpers.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate requestDelegate, IHostEnvironment environment)
    {
        _requestDelegate = requestDelegate;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("X-AspNet-Version");
            context.Response.Headers.Remove("Server");

            var connectSrc = "connect-src 'self';";
            var fontSrc = "font-src 'self' data:;";

            if (_environment.IsDevelopment())
            {
                connectSrc = "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*;";
            }

            if (context.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase))
            {
                fontSrc = "font-src 'self' data: https:;";
            }

            context.Response.Headers.Add("Strict-Transport-Security", "max-age=63072000");
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "frame-ancestors 'self'; " +
                "form-action 'self'; " +
                "img-src 'self' data: https:; " +
                fontSrc + " " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self' 'unsafe-inline'; " +
                connectSrc);
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("Referrer-Policy", "no-referrer-when-downgrade");
            context.Response.Headers.Add("Feature-Policy", "geolocation 'self'");
            context.Response.Headers.Add("Public-Key-Pins", "pin-sha256=&quot;y6FNDn9EJFGbn7i0a5olrWh7ECZKlFTl7Pfavyjwtg8=&quot;; pin-sha256=&quot;YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=&quot;; pin-sha256=&quot;Vjs8r4z+80wjNcr1YKepWQboSIRi63WsWXhIMN+eWys=&quot;; max-age=31536000; includeSubDomains");
            return Task.CompletedTask;
        });

        await _requestDelegate(context);
    }
}
