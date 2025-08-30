using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// OpenAI Provider 구현
/// </summary>
public class OpenAIProvider : LLMProviderBase
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
    public OpenAIProvider(HttpClient httpClient, ILogger<OpenAIProvider> logger, string apiKey, string? baseUrl = null)
        : base(httpClient, logger)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl ?? "https://api.openai.com/v1";
        
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public override string Name => "OpenAI";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedModels => new[]
    {
        "gpt-4",
        "gpt-4-turbo",
        "gpt-4-turbo-preview",
        "gpt-3.5-turbo",
        "gpt-3.5-turbo-16k"
    };

    /// <inheritdoc />
    public override string DefaultModel => "gpt-4-turbo";

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
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 4000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
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
            var choices = responseJson.GetProperty("choices");
            
            if (choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No response generated");
            }

            var message = choices[0].GetProperty("message");
            var generatedText = message.GetProperty("content").GetString();

            _logger.LogDebug("Generated text using model {Model}: {Length} characters", model, generatedText?.Length ?? 0);

            return generatedText ?? string.Empty;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is UnauthorizedAccessException))
        {
            _logger.LogError(ex, "Failed to generate text using OpenAI model: {Model}", model);
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
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 4000,
            temperature = 0.7,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
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
                
                var choices = jsonElement.GetProperty("choices");
                
                if (choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentElement))
                    {
                        var chunk = contentElement.GetString();
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
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/models");
            SetRequestHeaders(request);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI availability check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public override async Task<int> CountTokensAsync(string text, string? model = null)
    {
        // OpenAI의 경우 실제 토큰 계산 API를 사용할 수 있지만,
        // 여기서는 간단한 추정을 사용
        return await Task.FromResult(EstimateTokenCount(text));
    }

    /// <summary>
    /// HTTP 클라이언트 설정
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    protected override void SetRequestHeaders(HttpRequestMessage request)
    {
        base.SetRequestHeaders(request);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
    }
}