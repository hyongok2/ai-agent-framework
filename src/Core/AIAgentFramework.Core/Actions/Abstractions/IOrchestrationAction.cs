using AIAgentFramework.Core.Actions.Models;
using AIAgentFramework.Core.Orchestration.Execution;

namespace AIAgentFramework.Core.Actions.Abstractions;

/// <summary>
/// 오케스트레이션 액션 인터페이스
/// 타입 안전한 액션 실행을 위한 기본 계약
/// </summary>
public interface IOrchestrationAction
{
    /// <summary>
    /// 액션 타입
    /// </summary>
    ActionType Type { get; }
    
    /// <summary>
    /// 액션 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 액션 매개변수
    /// </summary>
    Dictionary<string, object> Parameters { get; }
    
    /// <summary>
    /// 액션 비동기 실행
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>액션 실행 결과</returns>
    Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);
}