using AIAgentFramework.Core.LLM.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// 단순한 Claude Provider 구현
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
        "claude-3-opus-20240229"
    };

    /// <inheritdoc />
    public override string DefaultModel => "claude-3-5-sonnet-20241022";

    /// <inheritdoc />
    protected override void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-beta", "tools-2024-04-04");
    }

    /// <inheritdoc />
    public override async Task<string> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestModel = model ?? DefaultModel;
            var requestBody = new
            {
                model = requestModel,
                max_tokens = 4096,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/messages", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseContent);
            var textContent = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return textContent ?? "No response generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate response from Claude");
            throw;
        }
    }

    /// <inheritdoc />
    public override Task<int> CountTokensAsync(string text, string? model = null)
    {
        // 간단한 추정: 단어 수 * 1.3
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Task.FromResult((int)(wordCount * 1.3));
    }
}