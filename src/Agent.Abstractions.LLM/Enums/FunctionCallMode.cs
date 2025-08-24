namespace Agent.Abstractions.LLM.Enums;

/// <summary>
/// 함수 호출 모드
/// </summary>
public enum FunctionCallMode
{
    /// <summary>자동 결정</summary>
    Auto,
    
    /// <summary>함수 호출 없음</summary>
    None,
    
    /// <summary>함수 호출 필수</summary>
    Required,
    
    /// <summary>특정 함수 강제</summary>
    Force
}