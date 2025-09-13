

using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using AIAgentFramework.Registry;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace AIAgentFramework.Tools.BuiltIn.Search;

/// <summary>
/// 웹 검색 도구 - HTTP 기반 검색 기능 제공
/// </summary>
public class WebSearchTool : ToolBase
{
    private readonly HttpClient _httpClient;
    private readonly WebSearchOptions _options;
    private readonly SemaphoreSlim _rateLimiter;
    private DateTime _lastRequestTime = DateTime.MinValue;

    /// <summary>
    /// 웹 검색 옵션
    /// </summary>
    public class WebSearchOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.search.example.com";
        public int MaxResults { get; set; } = 10;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int RequestsPerMinute { get; set; } = 60;
    }

    /// <summary>
    /// 검색 결과 모델
    /// </summary>
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public double Relevance { get; set; }
    }

    /// <summary>
    /// 생성자
    /// </summary>
    public WebSearchTool(
        HttpClient httpClient,
        ILogger<WebSearchTool> logger,
        IAdvancedRegistry registry,
        WebSearchOptions? options = null)
        : base(logger, registry)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new WebSearchOptions();
        
        // Rate limiting을 위한 세마포어 (동시 요청 1개로 제한)
        _rateLimiter = new SemaphoreSlim(1, 1);
        
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public override string Name => "WebSearch";

    /// <inheritdoc />
    public override string Description => "웹에서 정보를 검색합니다";

    /// <inheritdoc />
    public override IToolContract Contract => new ToolContract
    {
        RequiredParameters = new List<string> { "query" },
        OptionalParameters = new List<string> { "max_results" }
    };

    /// <inheritdoc />
    protected override async Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        try
        {

            // 검색 쿼리 추출
            if (!input.Parameters.TryGetValue("query", out var queryObj) || queryObj == null)
            {
                return ToolResult.CreateFailure("검색 쿼리가 제공되지 않았습니다");
            }

            var query = queryObj.ToString();
            if (string.IsNullOrWhiteSpace(query))
            {
                return ToolResult.CreateFailure("검색 쿼리가 비어있습니다");
            }

            // Rate limiting 적용
            await ApplyRateLimitingAsync(cancellationToken);

            // 검색 수행
            var results = await PerformSearchAsync(query, cancellationToken);

            _logger.LogInformation("웹 검색 완료: {Query}, 결과 수: {Count}", query, results.Count);

            return ToolResult.CreateSuccess(new Dictionary<string, object>
            {
                ["Query"] = query,
                ["ResultCount"] = results.Count,
                ["Results"] = results
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "웹 검색 중 네트워크 오류 발생");
            return ToolResult.CreateFailure($"네트워크 오류: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "웹 검색 시간 초과");
            return ToolResult.CreateFailure("검색 요청 시간 초과");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "웹 검색 중 예기치 않은 오류 발생");
            return ToolResult.CreateFailure($"검색 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 실제 검색 수행
    /// </summary>
    private async Task<List<SearchResult>> PerformSearchAsync(string query, CancellationToken cancellationToken)
    {
        // API 키가 없으면 목업 데이터 반환 (개발/테스트용)
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return GenerateMockResults(query);
        }

        // 실제 API 호출
        var encodedQuery = HttpUtility.UrlEncode(query);
        var requestUrl = $"{_options.BaseUrl}/search?q={encodedQuery}&max={_options.MaxResults}";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
        request.Headers.Add("Accept", "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.Timeout);

        var response = await _httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResponse = JsonSerializer.Deserialize<SearchApiResponse>(content);

        return ConvertToSearchResults(searchResponse);
    }

    /// <summary>
    /// Rate limiting 적용
    /// </summary>
    private async Task ApplyRateLimitingAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            // 분당 요청 제한 확인
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromMilliseconds(60000.0 / _options.RequestsPerMinute);

            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                _logger.LogDebug("Rate limiting: {Delay}ms 대기", delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    /// <summary>
    /// HTTP 클라이언트 설정
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.Timeout = _options.Timeout;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AIAgentFramework/1.0");
    }

    /// <summary>
    /// 목업 검색 결과 생성 (개발/테스트용)
    /// </summary>
    private List<SearchResult> GenerateMockResults(string query)
    {
        _logger.LogDebug("목업 검색 결과 생성: {Query}", query);

        return new List<SearchResult>
        {
            new SearchResult
            {
                Title = $"검색 결과 1: {query}",
                Url = $"https://example.com/1?q={HttpUtility.UrlEncode(query)}",
                Snippet = $"'{query}'에 대한 첫 번째 검색 결과입니다. 이것은 테스트 데이터입니다.",
                PublishedDate = DateTime.UtcNow.AddDays(-1),
                Relevance = 0.95
            },
            new SearchResult
            {
                Title = $"검색 결과 2: {query} 관련 정보",
                Url = $"https://example.com/2?q={HttpUtility.UrlEncode(query)}",
                Snippet = $"'{query}'와 관련된 유용한 정보를 제공합니다. 목업 데이터입니다.",
                PublishedDate = DateTime.UtcNow.AddDays(-3),
                Relevance = 0.85
            },
            new SearchResult
            {
                Title = $"검색 결과 3: {query} 가이드",
                Url = $"https://example.com/3?q={HttpUtility.UrlEncode(query)}",
                Snippet = $"'{query}'에 대한 종합적인 가이드와 튜토리얼입니다.",
                PublishedDate = DateTime.UtcNow.AddDays(-7),
                Relevance = 0.75
            }
        };
    }

    /// <summary>
    /// API 응답을 SearchResult로 변환
    /// </summary>
    private List<SearchResult> ConvertToSearchResults(SearchApiResponse? apiResponse)
    {
        if (apiResponse?.Items == null)
        {
            return new List<SearchResult>();
        }

        return apiResponse.Items.Select(item => new SearchResult
        {
            Title = item.Title ?? string.Empty,
            Url = item.Link ?? string.Empty,
            Snippet = item.Snippet ?? string.Empty,
            PublishedDate = item.PublishedDate ?? DateTime.UtcNow,
            Relevance = item.Score ?? 0.0
        }).ToList();
    }

    /// <summary>
    /// 검색 API 응답 모델
    /// </summary>
    private class SearchApiResponse
    {
        public List<SearchApiItem>? Items { get; set; }
        public int TotalResults { get; set; }
    }

    /// <summary>
    /// 검색 API 아이템 모델
    /// </summary>
    private class SearchApiItem
    {
        public string? Title { get; set; }
        public string? Link { get; set; }
        public string? Snippet { get; set; }
        public DateTime? PublishedDate { get; set; }
        public double? Score { get; set; }
    }
}