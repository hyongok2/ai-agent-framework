namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 실행 컨텍스트 인터페이스
/// 액션 실행 시 필요한 컨텍스트 정보 제공
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// 세션 ID
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// 사용자 요청
    /// </summary>
    string UserRequest { get; }
    
    /// <summary>
    /// 실행 기록
    /// </summary>
    List<IExecutionStep> ExecutionHistory { get; }
    
    /// <summary>
    /// 공유 데이터
    /// </summary>
    Dictionary<string, object> SharedData { get; }
    
    /// <summary>
    /// 레지스트리 (도구 및 LLM 기능 접근)
    /// </summary>
    IRegistry Registry { get; }
}