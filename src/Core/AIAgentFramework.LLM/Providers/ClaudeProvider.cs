
using AIAgentFramework.LLM.Interfaces;
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
    private readonly ITokenCounter _tokenCounter;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="httpClient">HTTP 클라이언트</param>
    /// <param name="logger">로거</param>
    /// <param name="tokenCounter">토큰 카운터</param>
    /// <param name="apiKey">API 키</param>
    /// <param name="baseUrl">기본 URL</param>
    public ClaudeProvider(HttpClient httpClient, ILogger<ClaudeProvider> logger, ITokenCounter tokenCounter, string apiKey, string? baseUrl = null)
        : base(httpClient, logger)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl ?? "https://api.anthropic.com/v1";
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        
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
            // 토큰 사용량 예상 (향후 토큰 예산 관리에서 활용)
            var estimatedInputTokens = await CountTokensAsync(prompt, model);
            _logger.LogDebug("예상 입력 토큰: {InputTokens}, 모델: {Model}", estimatedInputTokens, model);

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

            // 실제 출력 토큰 계산 (응답 분석)
            if (!string.IsNullOrEmpty(textContent))
            {
                var actualOutputTokens = await CountTokensAsync(textContent, model);
                _logger.LogDebug("실제 출력 토큰: {OutputTokens}, 모델: {Model}", actualOutputTokens, model);
            }

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

        // 토큰 사용량 예상
        var estimatedInputTokens = await CountTokensAsync(prompt, DefaultModel);
        _logger.LogDebug("스트리밍 예상 입력 토큰: {InputTokens}, 모델: {Model}", estimatedInputTokens, DefaultModel);

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

        var streamedContent = new StringBuilder();
        string? line;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                
                if (data == "[DONE]")
                {
                    // 스트리밍 완료 시 총 출력 토큰 계산
                    var totalStreamedContent = streamedContent.ToString();
                    if (!string.IsNullOrEmpty(totalStreamedContent))
                    {
                        try
                        {
                            var actualOutputTokens = await CountTokensAsync(totalStreamedContent, DefaultModel);
                            _logger.LogDebug("스트리밍 완료 - 실제 출력 토큰: {OutputTokens}, 모델: {Model}", 
                                actualOutputTokens, DefaultModel);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "스트리밍 완료 후 토큰 계산 실패");
                        }
                    }
                    yield break;
                }

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
                            streamedContent.Append(chunk);
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
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var targetModel = model ?? DefaultModel;
        
        if (!_tokenCounter.IsModelSupported(targetModel))
        {
            _logger.LogWarning("지원되지 않는 모델, 기본 추정 사용: {Model}", targetModel);
            return await Task.FromResult(EstimateTokenCount(text));
        }

        try
        {
            return await _tokenCounter.CountTokensAsync(text, targetModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "토큰 카운팅 실패, 기본 추정 사용: {Model}", targetModel);
            return await Task.FromResult(EstimateTokenCount(text));
        }
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