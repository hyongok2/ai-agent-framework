using AIAgentFramework.Core.Interfaces;

namespace AIAgentFramework.Tools.Execution;

/// <summary>
/// 도구 실행기 인터페이스
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// 파라미터 없는 도구 직접 실행
    /// </summary>
    Task<IToolResult> ExecuteDirectAsync(string toolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 파라미터와 함께 도구 실행
    /// </summary>
    Task<IToolResult> ExecuteWithParametersAsync(string toolName, IToolInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// LLM을 통한 파라미터 설정 후 도구 실행
    /// </summary>
    Task<IToolResult> ExecuteWithLLMParametersAsync(string toolName, string userRequest, CancellationToken cancellationToken = default);
}