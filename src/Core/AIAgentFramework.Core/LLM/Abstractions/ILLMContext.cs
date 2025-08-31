using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.Tools.Abstractions;

namespace AIAgentFramework.Core.LLM.Abstractions;

/// <summary>
/// LLM 컨텍스트 인터페이스
/// </summary>
public interface ILLMContext
{
    /// <summary>
    /// 세션 ID
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// 사용할 모델
    /// </summary>
    string? Model { get; set; }
    
    /// <summary>
    /// 파라미터
    /// </summary>
    Dictionary<string, object> Parameters { get; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// 실행 이력
    /// </summary>
    List<IExecutionStep> ExecutionHistory { get; }
    
    /// <summary>
    /// 공유 데이터
    /// </summary>
    Dictionary<string, object> SharedData { get; }
    
    /// <summary>
    /// 사용자 요청
    /// </summary>
    string? UserRequest { get; set; }
    
    /// <summary>
    /// 도구 이름
    /// </summary>
    string? ToolName { get; set; }
    
    /// <summary>
    /// 도구 계약
    /// </summary>
    IToolContract? ToolContract { get; set; }
}