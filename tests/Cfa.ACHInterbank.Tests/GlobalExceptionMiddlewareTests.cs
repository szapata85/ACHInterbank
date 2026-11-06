using System.Text;
using Cfa.ACHInterbank.Api.Middleware;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Tests;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task UnhandledException_LogsCompleteExceptionOnce_AndReturnsSafe500()
    {
        var exception = new InvalidOperationException("SENSITIVE-UNEXPECTED-FAILURE");
        var logger = new RecordingLogger();
        var context = CreateContext();
        context.TraceIdentifier = "test-trace-id";
        context.Request.Method = HttpMethods.Post;
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        var body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        Assert.Contains("Error interno. Incidente", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SENSITIVE-UNEXPECTED-FAILURE", body, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Contains("RuleId=UNHANDLED", entry.Message, StringComparison.Ordinal);
        Assert.Contains("TraceId=test-trace-id", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NachaGenerationException_PreservesResponseContract_AndReportsRuleId()
    {
        var exception = new NachaGenerationException("NACHA_INVALID", "regla de prueba");
        var logger = new RecordingLogger();
        var context = CreateContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        Assert.Contains("NACHA_INVALID: regla de prueba", body, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Same(exception, entry.Exception);
        Assert.Contains("RuleId=NACHA_INVALID", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartedResponse_IsNotOverwritten_AndExceptionIsRethrown()
    {
        var exception = new InvalidOperationException("downstream failure");
        var logger = new RecordingLogger();
        var context = CreateContext();
        var originalFeature = context.Features.Get<IHttpResponseFeature>()!;
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature(originalFeature));
        var middleware = new GlobalExceptionMiddleware(_ => throw exception, logger);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Same(exception, actual);
        Assert.Single(logger.Entries);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ClientCancellation_DoesNotEmitFalseServerFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var logger = new RecordingLogger();
        var context = CreateContext();
        context.RequestAborted = cancellation.Token;
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new OperationCanceledException(cancellation.Token), logger);

        await middleware.InvokeAsync(context);

        Assert.Empty(logger.Entries);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class StartedResponseFeature(IHttpResponseFeature inner) : IHttpResponseFeature
    {
        public int StatusCode { get => inner.StatusCode; set => inner.StatusCode = value; }
        public string? ReasonPhrase { get => inner.ReasonPhrase; set => inner.ReasonPhrase = value; }
        public IHeaderDictionary Headers { get => inner.Headers; set => inner.Headers = value; }
        public Stream Body { get => inner.Body; set => inner.Body = value; }
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) => inner.OnStarting(callback, state);
        public void OnCompleted(Func<object, Task> callback, object state) => inner.OnCompleted(callback, state);
    }

    private sealed class RecordingLogger : ILogger<GlobalExceptionMiddleware>
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => EmptyScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new();
            public void Dispose() { }
        }
    }
}
