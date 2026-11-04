using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Api.Middleware;

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

            // SEC-I05: no emitir HSTS sobre HTTP local; TLS/HSTS externo se valida
            // con el terminador autorizado, sin confiar en un X-Forwarded-Proto arbitrario.
            if (context.Request.IsHttps && !_environment.IsDevelopment())
            {
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000";
            }

            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "frame-ancestors 'self'; " +
                "form-action 'self'; " +
                "img-src 'self' data: https:; " +
                fontSrc + " " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self' 'unsafe-inline'; " +
                connectSrc;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=()";
            // No emitir Public-Key-Pins (HPKP) ni Feature-Policy: son obsoletos.
            return Task.CompletedTask;
        });

        await _requestDelegate(context);
    }
}
