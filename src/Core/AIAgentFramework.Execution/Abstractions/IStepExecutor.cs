using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.LLM.Services.Planning;

namespace AIAgentFramework.Execution.Abstractions;

/// <summary>
/// 단일 Step 실행 인터페이스
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// Step 실행
    /// </summary>
    /// <param name="step">실행할 단계</param>
    /// <param name="executable">실행 대상 (Tool 또는 LLM Function)</param>
    /// <param name="parameters">파라미터</param>
    /// <param name="userRequest">사용자 요청</param>
    /// <param name="agentContext">Agent 컨텍스트</param>
    /// <param name="onStreamChunk">스트리밍 청크 콜백 (선택적)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task<StepExecutionResult> ExecuteAsync(
        TaskStep step,
        ExecutableItem executable,
        string? parameters,
        string userRequest,
        IAgentContext agentContext,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default);
}
