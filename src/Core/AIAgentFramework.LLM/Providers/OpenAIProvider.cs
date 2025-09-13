using AIAgentFramework.Core.LLM.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// 단순한 OpenAI Provider 구현
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
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo"
    };

    /// <inheritdoc />
    public override string DefaultModel => "gpt-4o-mini";

    /// <inheritdoc />
    protected override void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
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
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 4096
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseContent);
            var textContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return textContent ?? "No response generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate response from OpenAI");
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