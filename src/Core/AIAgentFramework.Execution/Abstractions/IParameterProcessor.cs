using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Tools.Abstractions;

namespace AIAgentFramework.Execution.Abstractions;

/// <summary>
/// 파라미터 처리 (변수 치환, 검증, 자동 생성)
/// </summary>
public interface IParameterProcessor
{
    /// <summary>
    /// 파라미터 처리 (변수 치환 + 검증 + 필요시 자동 생성)
    /// </summary>
    Task<ParameterProcessingResult> ProcessAsync(
        string targetName,
        string? inputSchema,
        bool requiresParameters,
        string? rawParameters,
        string userRequest,
        string stepDescription,
        IAgentContext agentContext,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 파라미터 처리 결과
/// </summary>
public record ParameterProcessingResult
{
    public bool IsSuccess { get; init; }
    public string? ProcessedParameters { get; init; }
    public string? ErrorMessage { get; init; }
}
