using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Models;

namespace AIAgentFramework.Execution.Abstractions;

/// <summary>
/// 계획을 실행하는 Executor 인터페이스
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// 계획을 순차적으로 실행합니다
    /// </summary>
    /// <param name="input">실행 입력 정보</param>
    /// <param name="agentContext">Agent 글로벌 컨텍스트</param>
    /// <param name="onStepCompleted">각 단계 완료 시 호출되는 콜백 (선택적)</param>
    /// <param name="onStreamChunk">LLM 스트리밍 청크 콜백 (선택적)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<ExecutionResult> ExecuteAsync(
        ExecutionInput input,
        IAgentContext agentContext,
        Action<StepExecutionResult>? onStepCompleted = null,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default);
}
