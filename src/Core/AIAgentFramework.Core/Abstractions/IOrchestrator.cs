namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// AI Agent 오케스트레이터
/// - 사용자 입력 → 계획 수립 → 실행 → 결과 반환
/// - [TaskPlanner → PlanExecutor → Evaluator] 조율
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// 전체 Agent 워크플로우 실행 (일반 모드)
    /// </summary>
    /// <param name="userInput">사용자 요청</param>
    /// <param name="context">Agent 컨텍스트 (메타정보 + 변수 저장소)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>최종 실행 결과</returns>
    Task<IResult> ExecuteAsync(
        string userInput,
        IAgentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 전체 Agent 워크플로우 실행 (스트리밍 모드)
    /// - 계획 수립, 실행, 평가 과정을 실시간 스트리밍
    /// </summary>
    /// <param name="userInput">사용자 요청</param>
    /// <param name="context">Agent 컨텍스트 (메타정보 + 변수 저장소)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실시간 스트리밍 청크</returns>
    IAsyncEnumerable<IStreamChunk> ExecuteStreamAsync(
        string userInput,
        IAgentContext context,
        CancellationToken cancellationToken = default);
}
