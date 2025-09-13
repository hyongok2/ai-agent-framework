using AIAgentFramework.Core.Common.Registry;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Tools.BuiltIn.Search;

/// <summary>
/// 간단한 웹 검색 도구 예제
/// </summary>
public class WebSearchTool : ToolBase
{
    public WebSearchTool(ILogger<WebSearchTool> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public override string Name => "WebSearch";

    /// <inheritdoc />
    public override string Description => "웹 검색 도구";

    /// <inheritdoc />
    public override string Category => "Search";

    /// <inheritdoc />
    public override async Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = GetParameter<string>(input, "query") ?? "기본 검색어";

            // 실제로는 검색 API를 호출하겠지만, 지금은 목업 결과 반환
            await Task.Delay(100, cancellationToken); // API 호출 시뮬레이션

            var results = new Dictionary<string, object>
            {
                ["query"] = query,
                ["results"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = "검색 결과 1",
                        ["url"] = "https://example.com/1",
                        ["snippet"] = "검색 결과 1의 설명"
                    },
                    new Dictionary<string, object>
                    {
                        ["title"] = "검색 결과 2",
                        ["url"] = "https://example.com/2",
                        ["snippet"] = "검색 결과 2의 설명"
                    },
                    new Dictionary<string, object>
                    {
                        ["title"] = "검색 결과 3",
                        ["url"] = "https://example.com/3",
                        ["snippet"] = "검색 결과 3의 설명"
                    }
                },
                ["totalResults"] = 3
            };

            return ToolResult.CreateSuccess(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search failed");
            return ToolResult.CreateFailure($"검색 실패: {ex.Message}");
        }
    }
}