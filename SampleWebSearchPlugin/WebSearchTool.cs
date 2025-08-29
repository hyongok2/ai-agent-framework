using AIAgentFramework.Core.Attributes;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Tools.Plugin;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SampleWebSearchPlugin;

/// <summary>
/// 웹 검색 플러그인 도구
/// </summary>
[PluginTool(
    name: "WebSearch",
    description: "웹에서 정보를 검색하는 도구",
    version: "1.0.0",
    author: "AI Agent Framework Team",
    Category = "Search",
    Dependencies = "System.Net.Http",
    TimeoutSeconds = 30,
    IsCacheable = true,
    CacheTTLMinutes = 60,
    Tags = "web,search,internet"
)]
public class WebSearchTool : PluginToolBase
{
    private HttpClient? _httpClient;
    private string _searchApiUrl = string.Empty;
    private string _apiKey = string.Empty;

    /// <inheritdoc />
    public override string Name => "WebSearch";

    /// <inheritdoc />
    public override string Description => "웹에서 정보를 검색하는 도구";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Author => "AI Agent Framework Team";

    /// <inheritdoc />
    public override IEnumerable<string> Dependencies => new[] { "System.Net.Http" };

    /// <inheritdoc />
    public override IToolContract Contract { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public WebSearchTool(ILogger<WebSearchTool> logger) : base(logger)
    {
        Contract = new ToolContract()
            .WithInputSchema(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""query"": {
                        ""type"": ""string"",
                        ""description"": ""검색할 쿼리""
                    },
                    ""max_results"": {
                        ""type"": ""integer"",
                        ""description"": ""최대 결과 수"",
                        ""default"": 10
                    },
                    ""language"": {
                        ""type"": ""string"",
                        ""description"": ""검색 언어"",
                        ""default"": ""ko""
                    }
                },
                ""required"": [""query""]
            }")
            .WithOutputSchema(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""success"": { ""type"": ""boolean"" },
                    ""results"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""title"": { ""type"": ""string"" },
                                ""url"": { ""type"": ""string"" },
                                ""snippet"": { ""type"": ""string"" },
                                ""published_date"": { ""type"": ""string"" }
                            }
                        }
                    },
                    ""total_count"": { ""type"": ""integer"" },
                    ""message"": { ""type"": ""string"" }
                }
            }")
            .WithRequiredParameter("query")
            .WithOptionalParameter("max_results")
            .WithOptionalParameter("language");
    }

    /// <inheritdoc />
    protected override Task InitializeInternalAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
    {
        _searchApiUrl = GetConfigurationValue<string>("search_api_url", "https://api.example.com/search") ?? string.Empty;
        _apiKey = GetConfigurationValue<string>("api_key", "") ?? string.Empty;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AIAgentFramework-WebSearchPlugin/1.0");

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        _logger.LogDebug("WebSearchTool initialized with API URL: {ApiUrl}", _searchApiUrl);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task DisposeInternalAsync(CancellationToken cancellationToken = default)
    {
        _httpClient?.Dispose();
        _httpClient = null;
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task<Dictionary<string, object>> GetCustomStatusAsync()
    {
        return Task.FromResult(new Dictionary<string, object>
        {
            ["api_url"] = _searchApiUrl,
            ["has_api_key"] = !string.IsNullOrWhiteSpace(_apiKey),
            ["http_client_ready"] = _httpClient != null
        });
    }

    /// <inheritdoc />
    protected override async Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        var query = GetRequiredParameter<string>(input, "query");
        var maxResults = GetParameter<int>(input, "max_results", 10);
        var language = GetParameter<string>(input, "language", "ko");

        try
        {
            _logger.LogDebug("Performing web search for query: {Query}", query);

            // 실제 웹 검색 API 호출 대신 모의 결과 반환
            // 실제 구현에서는 Google Search API, Bing Search API 등을 사용
            var mockResults = await PerformMockSearchAsync(query, maxResults, language ?? "ko", cancellationToken);

            return ToolResult.CreateSuccess()
                .WithData("success", true)
                .WithData("results", mockResults)
                .WithData("total_count", mockResults.Count)
                .WithData("message", $"Found {mockResults.Count} results for '{query}'")
                .WithMetadata("query", query)
                .WithMetadata("language", language ?? "ko");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform web search for query: {Query}", query);
            return ToolResult.CreateFailure($"Web search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 모의 웹 검색 수행 (실제 구현에서는 실제 검색 API 호출)
    /// </summary>
    /// <param name="query">검색 쿼리</param>
    /// <param name="maxResults">최대 결과 수</param>
    /// <param name="language">언어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검색 결과</returns>
    private async Task<List<Dictionary<string, object>>> PerformMockSearchAsync(
        string query, int maxResults, string language, CancellationToken cancellationToken)
    {
        // 실제 구현에서는 여기서 HTTP 요청을 수행
        await Task.Delay(100, cancellationToken); // 네트워크 지연 시뮬레이션

        var results = new List<Dictionary<string, object>>();
        var resultCount = Math.Min(maxResults, 5); // 최대 5개의 모의 결과

        for (int i = 1; i <= resultCount; i++)
        {
            results.Add(new Dictionary<string, object>
            {
                ["title"] = $"Search Result {i} for '{query}'",
                ["url"] = $"https://example.com/result-{i}?q={Uri.EscapeDataString(query)}",
                ["snippet"] = $"This is a sample search result snippet for '{query}'. It contains relevant information about the search query.",
                ["published_date"] = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd")
            });
        }

        _logger.LogDebug("Generated {Count} mock search results for query: {Query}", results.Count, query);
        return results;
    }

    /// <summary>
    /// 실제 웹 검색 API 호출 (예시)
    /// </summary>
    /// <param name="query">검색 쿼리</param>
    /// <param name="maxResults">최대 결과 수</param>
    /// <param name="language">언어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검색 결과</returns>
    private async Task<List<Dictionary<string, object>>> PerformRealSearchAsync(
        string query, int maxResults, string language, CancellationToken cancellationToken)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HTTP client is not initialized");
        }

        // 실제 검색 API 호출 예시 (Google Custom Search API 형태)
        var requestUrl = $"{_searchApiUrl}?q={Uri.EscapeDataString(query)}&num={maxResults}&hl={language}";
        
        var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // JSON 응답 파싱 (실제 API 응답 구조에 따라 조정 필요)
        using var document = JsonDocument.Parse(responseContent);
        var results = new List<Dictionary<string, object>>();

        if (document.RootElement.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var result = new Dictionary<string, object>();
                
                if (item.TryGetProperty("title", out var title))
                    result["title"] = title.GetString() ?? "";
                
                if (item.TryGetProperty("link", out var link))
                    result["url"] = link.GetString() ?? "";
                
                if (item.TryGetProperty("snippet", out var snippet))
                    result["snippet"] = snippet.GetString() ?? "";
                
                result["published_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
                
                results.Add(result);
            }
        }

        return results;
    }
}