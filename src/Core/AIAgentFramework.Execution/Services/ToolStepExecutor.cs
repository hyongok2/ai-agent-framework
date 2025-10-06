using System.Diagnostics;
using System.Text.Json;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.LLM.Services.Planning;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// Tool 실행 담당
/// </summary>
public class ToolStepExecutor : IStepExecutor
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    private readonly ILogger? _logger;

    public ToolStepExecutor(ILogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<StepExecutionResult> ExecuteAsync(
        TaskStep step,
        ExecutableItem executable,
        string? parameters,
        string userRequest,
        IAgentContext agentContext,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default)
    {
        if (executable.Type != ExecutableType.Tool || executable.Tool == null)
        {
            throw new ArgumentException("ExecutableItem must be a Tool", nameof(executable));
        }

        var sw = Stopwatch.StartNew();
        var executionId = Guid.NewGuid().ToString("N")[..8];
        var tool = executable.Tool;
        IToolResult? toolResult = null;
        Exception? error = null;

        try
        {
            // Tool에 전달할 컨텍스트 (메타정보만 필요, 변수는 불필요)
            var toolContext = AgentContext.Create(agentContext.UserId);
            object? toolInput = parameters;

            // JSON 문자열이면 Dictionary로 파싱
            if (!string.IsNullOrEmpty(parameters))
            {
                try
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(parameters);
                    toolInput = ConvertJsonElementToDictionary(jsonElement);
                }
                catch
                {
                    // JSON 파싱 실패하면 문자열 그대로 사용
                    toolInput = parameters;
                }
            }

            toolResult = await tool.ExecuteAsync(toolInput, toolContext, cancellationToken);

            // Output을 JSON 직렬화
            string? output = null;
            if (toolResult.Data != null)
            {
                if (toolResult.Data is string str)
                {
                    output = str;
                }
                else
                {
                    output = JsonSerializer.Serialize(toolResult.Data, _jsonOptions);
                }
            }

            return new StepExecutionResult
            {
                StepNumber = step.StepNumber,
                Description = step.Description,
                ToolName = step.ToolName,
                IsSuccess = toolResult.IsSuccess,
                Output = output,
                ErrorMessage = toolResult.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            await LogExecutionAsync(executionId, tool.Metadata.Name, parameters, toolResult, sw.ElapsedMilliseconds, error, cancellationToken);
        }
    }

    private async Task LogExecutionAsync(
        string executionId,
        string toolName,
        string? request,
        IToolResult? result,
        long durationMs,
        Exception? error,
        CancellationToken cancellationToken)
    {
        if (_logger == null) return;

        try
        {
            var logEntry = LogEntry.Create(
                logType: "Tool",
                targetName: toolName,
                executionId: executionId,
                timestamp: DateTimeOffset.UtcNow,
                request: request,
                response: result,
                durationMs: durationMs,
                success: error == null && (result?.IsSuccess ?? false),
                errorMessage: error?.Message ?? result?.ErrorMessage
            );

            await _logger.LogAsync(logEntry, cancellationToken);
        }
        catch
        {
            // 로깅 실패는 조용히 무시
        }
    }

    private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonValue(property.Value);
            }
        }

        return dict;
    }

    private static object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            _ => element.ToString()
        };
    }
}
