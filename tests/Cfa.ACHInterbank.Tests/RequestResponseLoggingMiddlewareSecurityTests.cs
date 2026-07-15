using System.Text;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class RequestResponseLoggingMiddlewareSecurityTests
{
    [Fact]
    public async Task Middleware_ShouldOmitMultipartAndRedactAuthorizationAndResponseToken()
    {
        const string sensitiveMarker = "SENSITIVE-CERTIFICATE-MATERIAL";
        const string bearerMarker = "SENSITIVE-BEARER-TOKEN";
        var logger = new Mock<ILoggerManager>();
        var messages = new List<string>();
        logger.Setup(x => x.LogInfo(It.IsAny<string>())).Callback<string>(messages.Add);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/nacha-security/certificates/management/private";
        context.Request.ContentType = "multipart/form-data; boundary=test";
        context.Request.Headers.Authorization = $"Bearer {bearerMarker}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(sensitiveMarker));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Response.Body = new MemoryStream();

        var middleware = new RequestResponseLoggingMiddleware(async httpContext =>
        {
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync($"{{\"token\":\"{bearerMarker}\",\"message\":\"ok\"}}");
        }, logger.Object);

        await middleware.Invoke(context);

        var log = string.Join(Environment.NewLine, messages);
        Assert.Contains("[MULTIPART CONTENT OMITTED]", log);
        Assert.Contains("[REDACTED]", log);
        Assert.DoesNotContain(sensitiveMarker, log);
        Assert.DoesNotContain(bearerMarker, log);
    }

    [Fact]
    public async Task Middleware_ShouldRedactPasswordFromJsonRequest()
    {
        const string passwordMarker = "SENSITIVE-PASSWORD";
        var logger = new Mock<ILoggerManager>();
        var messages = new List<string>();
        logger.Setup(x => x.LogInfo(It.IsAny<string>())).Callback<string>(messages.Add);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/auth/login";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"{{\"username\":\"test\",\"password\":\"{passwordMarker}\"}}"));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Response.Body = new MemoryStream();

        var middleware = new RequestResponseLoggingMiddleware(async httpContext =>
        {
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync("{\"ok\":true}");
        }, logger.Object);

        await middleware.Invoke(context);

        var log = string.Join(Environment.NewLine, messages);
        Assert.Contains("[REDACTED]", log);
        Assert.DoesNotContain(passwordMarker, log);
    }
}
