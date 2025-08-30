using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// LLM Provider 기본 추상 클래스
/// </summary>
public abstract class LLMProviderBase : ILLMProvider
{
    protected readonly ILogger _logger;
    protected readonly HttpClient _httpClient;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="httpClient">HTTP 클라이언트</param>
    /// <param name="logger">로거</param>
    protected LLMProviderBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedModels { get; }

    /// <inheritdoc />
    public abstract string DefaultModel { get; }

    /// <inheritdoc />
    public virtual async Task<bool> IsAvailableAsync()
    {
        try
        {
            return await CheckAvailabilityAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check availability for provider: {ProviderName}", Name);
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GenerateAsync(prompt, DefaultModel, cancellationToken);
    }

    /// <inheritdoc />
    public abstract Task<string> GenerateAsync(string prompt, string model, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<T?> GenerateStructuredAsync<T>(string prompt, CancellationToken cancellationToken = default) where T : class
    {
        return await GenerateStructuredAsync<T>(prompt, DefaultModel, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> GenerateStructuredAsync<T>(string prompt, string model, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var jsonPrompt = $"{prompt}\n\nPlease respond with valid JSON that matches the expected structure.";
            var response = await GenerateAsync(jsonPrompt, model, cancellationToken);
            
            // JSON 응답에서 실제 JSON 부분만 추출
            var jsonContent = ExtractJsonFromResponse(response);
            
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate structured response for type: {TypeName}", typeof(T).Name);
            return null;
        }
    }

    /// <inheritdoc />
    public abstract IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<int> CountTokensAsync(string text, string? model = null)
    {
        // 기본적인 토큰 수 추정 (실제 구현에서는 각 Provider별로 정확한 토큰화 로직 사용)
        return await Task.FromResult(EstimateTokenCount(text));
    }

    /// <summary>
    /// Provider 가용성 확인 (하위 클래스에서 구현)
    /// </summary>
    /// <returns>가용성 여부</returns>
    protected abstract Task<bool> CheckAvailabilityAsync();

    /// <summary>
    /// 응답에서 JSON 부분 추출
    /// </summary>
    /// <param name="response">전체 응답</param>
    /// <returns>JSON 문자열</returns>
    protected virtual string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "{}";

        // JSON 블록 찾기 (```json ... ``` 형태)
        var jsonBlockMatch = System.Text.RegularExpressions.Regex.Match(
            response, 
            @"```json\s*(.*?)\s*```", 
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (jsonBlockMatch.Success)
        {
            return jsonBlockMatch.Groups[1].Value.Trim();
        }

        // 중괄호로 시작하는 JSON 찾기
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(
            response, 
            @"\{.*\}", 
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (jsonMatch.Success)
        {
            return jsonMatch.Value;
        }

        // JSON을 찾을 수 없으면 전체 응답을 반환
        return response.Trim();
    }

    /// <summary>
    /// 토큰 수 추정
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <returns>추정 토큰 수</returns>
    protected virtual int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // 간단한 추정: 단어 수 * 1.3 (평균적으로 1단어 = 1.3토큰)
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount * 1.3);
    }

    /// <summary>
    /// HTTP 요청 헤더 설정
    /// </summary>
    /// <param name="request">HTTP 요청 메시지</param>
    protected virtual void SetRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", $"AIAgentFramework/{GetType().Assembly.GetName().Version}");
    }

    /// <summary>
    /// API 오류 처리
    /// </summary>
    /// <param name="response">HTTP 응답</param>
    /// <param name="content">응답 내용</param>
    protected virtual void HandleApiError(HttpResponseMessage response, string content)
    {
        var errorMessage = $"API request failed with status {response.StatusCode}: {content}";
        _logger.LogError("LLM API Error: {ErrorMessage}", errorMessage);
        
        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => new UnauthorizedAccessException("Invalid API key or authentication failed"),
            System.Net.HttpStatusCode.TooManyRequests => new InvalidOperationException("Rate limit exceeded"),
            System.Net.HttpStatusCode.BadRequest => new ArgumentException($"Bad request: {content}"),
            _ => new HttpRequestException(errorMessage)
        };
    }

    /// <summary>
    /// 리소스 해제
    /// </summary>
    public virtual void Dispose()
    {
        _httpClient?.Dispose();
    }
}