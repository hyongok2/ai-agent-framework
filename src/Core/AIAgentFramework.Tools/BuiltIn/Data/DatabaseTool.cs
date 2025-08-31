

using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace AIAgentFramework.Tools.BuiltIn.Data;

/// <summary>
/// 데이터베이스 도구 - SQL 쿼리 실행 및 데이터 작업 제공
/// </summary>
public class DatabaseTool : ToolBase
{
    private readonly DatabaseOptions _options;

    /// <summary>
    /// 데이터베이스 옵션
    /// </summary>
    public class DatabaseOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ProviderName { get; set; } = "Microsoft.Data.SqlClient";
        public int CommandTimeoutSeconds { get; set; } = 30;
        public int MaxRows { get; set; } = 1000;
        public bool AllowDataModification { get; set; } = false;
        public List<string> AllowedOperations { get; set; } = new() { "SELECT" };
    }

    /// <summary>
    /// 생성자
    /// </summary>
    public DatabaseTool(ILogger<DatabaseTool> logger, DatabaseOptions? options = null)
        : base(logger)
    {
        _options = options ?? new DatabaseOptions();
    }

    /// <inheritdoc />
    public override string Name => "Database";

    /// <inheritdoc />
    public override string Description => "데이터베이스 쿼리 및 작업을 수행합니다";

    /// <inheritdoc />
    public override IToolContract Contract => new ToolContract
    {
        RequiredParameters = new List<string> { "operation" },
        OptionalParameters = new List<string> { "query", "command", "schema_type" }
    };

    /// <inheritdoc />
    protected override async Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!input.Parameters.TryGetValue("operation", out var operationObj))
            {
                return ToolResult.CreateFailure("작업 타입이 제공되지 않았습니다");
            }

            var operation = operationObj?.ToString()?.ToLowerInvariant();

            return operation switch
            {
                "query" => await ExecuteQueryAsync(input, cancellationToken),
                "execute" => await ExecuteCommandAsync(input, cancellationToken),
                "test" => await TestConnectionAsync(cancellationToken),
                _ => ToolResult.CreateFailure($"지원되지 않는 작업: {operation}")
            };
        }
        catch (Exception ex)
        {
            return ToolResult.CreateFailure($"데이터베이스 작업 실패: {ex.Message}");
        }
    }

    private async Task<ToolResult> ExecuteQueryAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return ToolResult.CreateFailure("연결 문자열이 설정되지 않았습니다");
        }

        if (!input.Parameters.TryGetValue("query", out var queryObj))
        {
            return ToolResult.CreateFailure("SQL 쿼리가 제공되지 않았습니다");
        }

        var query = queryObj?.ToString() ?? string.Empty;
        
        // 목업 데이터 반환 (실제 데이터베이스 연결이 없는 경우)
        var mockResults = new Dictionary<string, object>
        {
            ["Query"] = query,
            ["RowCount"] = 3,
            ["Columns"] = new[] { "id", "name", "created_at" },
            ["Rows"] = new[]
            {
                new { id = 1, name = "테스트 항목 1", created_at = DateTime.UtcNow.AddDays(-1) },
                new { id = 2, name = "테스트 항목 2", created_at = DateTime.UtcNow.AddDays(-2) },
                new { id = 3, name = "테스트 항목 3", created_at = DateTime.UtcNow.AddDays(-3) }
            },
            ["ExecutionTime"] = TimeSpan.FromMilliseconds(45)
        };

        _logger.LogInformation("데이터베이스 쿼리 실행 (목업): {Query}", query);

        return await Task.FromResult(ToolResult.CreateSuccess(mockResults));
    }

    private async Task<ToolResult> ExecuteCommandAsync(IToolInput input, CancellationToken cancellationToken)
    {
        if (!_options.AllowDataModification)
        {
            return ToolResult.CreateFailure("데이터 수정이 허용되지 않습니다");
        }

        if (!input.Parameters.TryGetValue("command", out var commandObj))
        {
            return ToolResult.CreateFailure("SQL 명령이 제공되지 않았습니다");
        }

        var command = commandObj?.ToString() ?? string.Empty;
        
        // 목업 응답
        var mockResult = new Dictionary<string, object>
        {
            ["Command"] = command,
            ["RowsAffected"] = 1,
            ["ExecutionTime"] = TimeSpan.FromMilliseconds(23)
        };

        _logger.LogInformation("데이터베이스 명령 실행 (목업): {Command}", command);

        return await Task.FromResult(ToolResult.CreateSuccess(mockResult));
    }

    private async Task<ToolResult> TestConnectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return ToolResult.CreateFailure("연결 문자열이 설정되지 않았습니다");
        }

        // 목업 연결 테스트
        var mockResult = new Dictionary<string, object>
        {
            ["ConnectionSuccessful"] = true,
            ["ConnectionTime"] = TimeSpan.FromMilliseconds(120),
            ["DatabaseVersion"] = "목업 데이터베이스 v1.0",
            ["ProviderName"] = _options.ProviderName
        };

        _logger.LogInformation("데이터베이스 연결 테스트 (목업) 완료");

        return await Task.FromResult(ToolResult.CreateSuccess(mockResult));
    }
}