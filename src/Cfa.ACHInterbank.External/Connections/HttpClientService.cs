using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class HttpClientService : IHttpClientService
{
    private readonly ILoggerManager _logger;
    private readonly Token _tokenSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpClientService(
        ILoggerManager logger,
        IOptions<Token> tokenOptions,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _tokenSettings = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> SendPostRequestAsync<T>(
        string url,
        object content,
        HttpMethod method,
        TypeBody typeBody,
        string? token = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException("External HTTP endpoint must be a safe absolute URI.");
        }

        using var client = _httpClientFactory.CreateClient(ExternalHttpClientNames.GenericNoRetry);
        using var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        if (content is not null)
        {
            request.Content = typeBody switch
            {
                TypeBody.Query => new FormUrlEncodedContent(ParseFormUrlEncodedContent(JsonSerializer.Serialize(content))),
                _ => new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
            };
        }

        if (!string.IsNullOrWhiteSpace(_tokenSettings.x_api_key))
        {
            request.Headers.TryAddWithoutValidation("x-api-key", _tokenSettings.x_api_key);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var started = Stopwatch.GetTimestamp();
        _logger.LogInfo(
            $"External HTTP request; method={method.Method}; endpoint={ExternalLogSanitizer.Endpoint(endpoint)}; " +
            $"contentType={request.Content?.Headers.ContentType?.MediaType ?? "none"}; retry=disabled.");

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            var responseContent = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            _logger.LogInfo(
                $"External HTTP response; method={method.Method}; endpoint={ExternalLogSanitizer.Endpoint(endpoint)}; " +
                $"status={(int)response.StatusCode}; elapsedMs={elapsedMs:F0}; bodyLength={responseContent.Length}.");

            if (string.IsNullOrEmpty(responseContent))
            {
                // Preserve the legacy contract: an empty response body returns default even when the status is not successful.
                return default!;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Preserve the legacy exception shape without echoing the potentially sensitive external response body.
                throw new Exception(
                    $"External HTTP request failed with status {(int)response.StatusCode} ({response.StatusCode}); response body redacted.");
            }

            return JsonSerializer.Deserialize<T>(responseContent)!;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarn(
                $"External HTTP request cancelled by caller; method={method.Method}; endpoint={ExternalLogSanitizer.Endpoint(endpoint)}.");
            throw;
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            _logger.LogError(
                $"External HTTP request failed; method={method.Method}; endpoint={ExternalLogSanitizer.Endpoint(endpoint)}; " +
                $"elapsedMs={elapsedMs:F0}; exception={ex.GetType().Name}; retry=disabled.");
            throw;
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseFormUrlEncodedContent(string content)
        => JsonSerializer.Deserialize<Dictionary<string, string>>(content)?.ToArray()
           ?? Array.Empty<KeyValuePair<string, string>>();
}
