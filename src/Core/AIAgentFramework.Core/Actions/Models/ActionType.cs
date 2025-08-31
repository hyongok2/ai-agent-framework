namespace AIAgentFramework.Core.Actions.Models;

/// <summary>
/// 오케스트레이션 액션 타입
/// </summary>
public enum ActionType
{
    /// <summary>
    /// LLM 기능 실행
    /// </summary>
    LLMFunction,
    
    /// <summary>
    /// 도구 실행
    /// </summary>
    Tool,
    
    /// <summary>
    /// 제어 흐름 (조건문, 반복문 등)
    /// </summary>
    ControlFlow,
    
    /// <summary>
    /// 데이터 변환
    /// </summary>
    DataTransform,
    
    /// <summary>
    /// 검증
    /// </summary>
    Validation,
    
    /// <summary>
    /// 알 수 없는 타입
    /// </summary>
    Unknown
}