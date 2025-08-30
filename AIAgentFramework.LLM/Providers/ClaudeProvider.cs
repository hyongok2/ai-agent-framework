using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// Claude (Anthropic) Provider 구현
/// </summary>
public class ClaudeProvider : LLMProviderBase
{
    private readonly string _apiKey;
    private readonly string _baseUrl;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="httpClient">HTTP 클라이언트</param>
    /// <param name="logger">로거</param>
    /// <param name="apiKey">API 키</param>
    /// <param name="baseUrl">기본 URL</param>
    public ClaudeProvider(HttpClient httpClient, ILogger<ClaudeProvider> logger, string apiKey, string? baseUrl = null)
        : base(httpClient, logger)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl ?? "https://api.anthropic.com/v1";
        
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public override string Name => "Claude";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedModels => new[]
    {
        "claude-3-5-sonnet-20241022",
        "claude-3-5-haiku-20241022", 
        "claude-3-opus-20240229",
        "claude-3-sonnet-20240229",
        "claude-3-haiku-20240307",
        "claude-2.1",
        "claude-2.0"
    };

    /// <inheritdoc />
    public override string DefaultModel => "claude-3-5-sonnet-20241022";

    /// <inheritdoc />
    public override async Task<string> GenerateAsync(string prompt, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        if (!SupportedModels.Contains(model))
            throw new ArgumentException($"Model '{model}' is not supported", nameof(model));

        try
        {
            var requestBody = new
            {
                model = model,
                max_tokens = 4000,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/messages")
            {
                Content = content
            };

            SetRequestHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                HandleApiError(response, responseContent);
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var contentArray = responseJson.GetProperty("content");
            
            if (contentArray.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No response generated");
            }

            var textContent = contentArray[0].GetProperty("text").GetString();

            _logger.LogDebug("Generated text using model {Model}: {Length} characters", model, textContent?.Length ?? 0);

            return textContent ?? string.Empty;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is UnauthorizedAccessException))
        {
            _logger.LogError(ex, "Failed to generate text using Claude model: {Model}", model);
            throw new InvalidOperationException($"Failed to generate text: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var requestBody = new
        {
            model = DefaultModel,
            max_tokens = 4000,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/messages")
        {
            Content = content
        };

        SetRequestHeaders(request);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            HandleApiError(response, errorContent);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                
                if (data == "[DONE]")
                    yield break;

                JsonElement jsonElement;
                try
                {
                    jsonElement = JsonSerializer.Deserialize<JsonElement>(data);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response: {Data}", data);
                    continue;
                }
                
                if (jsonElement.TryGetProperty("type", out var typeElement) && 
                    typeElement.GetString() == "content_block_delta")
                {
                    var delta = jsonElement.GetProperty("delta");
                    if (delta.TryGetProperty("text", out var textElement))
                    {
                        var chunk = textElement.GetString();
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            yield return chunk;
                        }
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> CheckAvailabilityAsync()
    {
        try
        {
            // Claude API는 별도의 health check 엔드포인트가 없으므로
            // 간단한 메시지 요청으로 가용성 확인
            var testRequestBody = new
            {
                model = DefaultModel,
                max_tokens = 10,
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                }
            };

            var json = JsonSerializer.Serialize(testRequestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/messages")
            {
                Content = content
            };

            SetRequestHeaders(request);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude availability check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public override async Task<int> CountTokensAsync(string text, string? model = null)
    {
        // Claude의 경우 실제 토큰 계산 API를 사용할 수 있지만,
        // 여기서는 간단한 추정을 사용
        return await Task.FromResult(EstimateTokenCount(text));
    }

    /// <summary>
    /// HTTP 클라이언트 설정
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    protected override void SetRequestHeaders(HttpRequestMessage request)
    {
        base.SetRequestHeaders(request);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", "messages-2023-12-15");
    }
}