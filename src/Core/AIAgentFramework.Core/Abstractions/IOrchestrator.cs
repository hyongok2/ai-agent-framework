namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// 오케스트레이터 인터페이스
/// [계획 - 실행] 패턴의 핵심 조율자
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// 사용자 입력을 받아 전체 실행 흐름을 조율
    /// </summary>
    /// <param name="userInput">사용자 입력</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>최종 실행 결과</returns>
    Task<IResult> ExecuteAsync(
        string userInput,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}
