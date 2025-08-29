namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 명령 인터페이스
/// </summary>
public interface ICommand
{
    /// <summary>
    /// 명령 ID
    /// </summary>
    string CommandId { get; }
    
    /// <summary>
    /// 명령 타입
    /// </summary>
    string CommandType { get; }
    
    /// <summary>
    /// 명령을 실행합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<IExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default);
}