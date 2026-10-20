using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cfa.ACHInterbank.Tests.TestSupport;

/// <summary>
/// Minimal HTTP loopback server for SOAP client characterization tests.
/// It keeps the IPv4 listener bound while publishing its dynamic port, so a
/// logical localhost endpoint can safely be bridged to 127.0.0.1.
/// </summary>
internal sealed class LocalSoapServer : IDisposable
{
    private const int MaxHeaderBytes = 64 * 1024;
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly Func<CapturedSoapRequest, string, HttpResponseMessage> _handler;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;

    private LocalSoapServer(Func<CapturedSoapRequest, string, HttpResponseMessage> handler)
    {
        _handler = handler;
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _loopTask = ListenLoopAsync();
    }

    public int Port { get; }

    public string Url => new UriBuilder(Uri.UriSchemeHttp, IPAddress.Loopback.ToString(), Port).Uri.AbsoluteUri;

    public List<CapturedSoapRequest> Requests { get; } = [];

    public static Task<LocalSoapServer> StartAsync(
        Func<CapturedSoapRequest, string, HttpResponseMessage> handler)
        => Task.FromResult(new LocalSoapServer(handler));

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (_cts.IsCancellationRequested)
            {
                break;
            }

            await ProcessClientAsync(client, _cts.Token).ConfigureAwait(false);
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var request = await ReadRequestAsync(stream, ct).ConfigureAwait(false);
            Requests.Add(request);

            using var response = _handler(request, request.Body);
            await WriteResponseAsync(stream, response, ct).ConfigureAwait(false);
        }
    }

    private static async Task<CapturedSoapRequest> ReadRequestAsync(NetworkStream stream, CancellationToken ct)
    {
        await using var received = new MemoryStream();
        var buffer = new byte[4096];
        var headerEnd = -1;

        while (headerEnd < 0)
        {
            var count = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (count == 0)
            {
                throw new InvalidOperationException("Local SOAP test server received an incomplete HTTP request.");
            }

            await received.WriteAsync(buffer.AsMemory(0, count), ct).ConfigureAwait(false);
            if (received.Length > MaxHeaderBytes)
            {
                throw new InvalidOperationException("Local SOAP test server request headers exceeded the safe limit.");
            }

            var bytes = received.GetBuffer();
            headerEnd = FindHeaderEnd(bytes, (int)received.Length);
        }

        var requestBytes = received.ToArray();
        var headerText = Encoding.ASCII.GetString(requestBytes, 0, headerEnd);
        var lines = headerText.Split("\r\n", StringSplitOptions.None);
        var requestTarget = ParseRequestTarget(lines.FirstOrDefault());
        var headers = ParseHeaders(lines.Skip(1));
        var contentLength = headers.TryGetValue("Content-Length", out var contentLengthText)
            && int.TryParse(contentLengthText, out var parsedContentLength)
            ? parsedContentLength
            : 0;

        if (contentLength < 0)
        {
            throw new InvalidOperationException("Local SOAP test server received an invalid content length.");
        }

        var body = new byte[contentLength];
        var bodyOffset = headerEnd + 4;
        var alreadyRead = Math.Min(contentLength, requestBytes.Length - bodyOffset);
        if (alreadyRead > 0)
        {
            Buffer.BlockCopy(requestBytes, bodyOffset, body, 0, alreadyRead);
        }

        while (alreadyRead < contentLength)
        {
            var read = await stream.ReadAsync(body.AsMemory(alreadyRead, contentLength - alreadyRead), ct).ConfigureAwait(false);
            if (read == 0)
            {
                throw new InvalidOperationException("Local SOAP test server received an incomplete HTTP body.");
            }

            alreadyRead += read;
        }

        return new CapturedSoapRequest(
            Encoding.UTF8.GetString(body),
            headers.GetValueOrDefault("SOAPAction") ?? string.Empty,
            headers.GetValueOrDefault("Content-Type") ?? string.Empty,
            headers.GetValueOrDefault("Host") ?? string.Empty,
            requestTarget);
    }

    private static async Task WriteResponseAsync(NetworkStream stream, HttpResponseMessage response, CancellationToken ct)
    {
        var payload = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var contentType = response.Content?.Headers.ContentType?.ToString() ?? "text/xml";
        var reasonPhrase = string.IsNullOrWhiteSpace(response.ReasonPhrase)
            ? response.StatusCode.ToString()
            : response.ReasonPhrase;
        var headers = $"HTTP/1.1 {(int)response.StatusCode} {reasonPhrase}\r\n" +
                      $"Content-Type: {contentType}\r\n" +
                      $"Content-Length: {payloadBytes.Length}\r\n" +
                      "Connection: close\r\n\r\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(headers), ct).ConfigureAwait(false);
        if (payloadBytes.Length > 0)
        {
            await stream.WriteAsync(payloadBytes, ct).ConfigureAwait(false);
        }

        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static int FindHeaderEnd(byte[] bytes, int count)
    {
        for (var index = 3; index < count; index++)
        {
            if (bytes[index - 3] == (byte)'\r'
                && bytes[index - 2] == (byte)'\n'
                && bytes[index - 1] == (byte)'\r'
                && bytes[index] == (byte)'\n')
            {
                return index - 3;
            }
        }

        return -1;
    }

    private static string ParseRequestTarget(string? requestLine)
    {
        var parts = (requestLine ?? string.Empty).Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !parts[1].StartsWith('/'))
        {
            throw new InvalidOperationException("Local SOAP test server received an invalid HTTP request line.");
        }

        return parts[1].Split('?', 2)[0];
    }

    private static Dictionary<string, string> ParseHeaders(IEnumerable<string> lines)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            headers[line[..separator]] = line[(separator + 1)..].Trim();
        }

        return headers;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        try
        {
            _loopTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (_cts.IsCancellationRequested)
        {
        }

        _cts.Dispose();
    }
}

internal sealed record CapturedSoapRequest(
    string Body,
    string SoapAction,
    string ContentType,
    string Host,
    string Path);
