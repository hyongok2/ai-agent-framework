namespace Agent.Abstractions.Orchestration.Execution;

/// <summary>
/// Step 종류
/// </summary>
public enum StepKind
{
    /// <summary>LLM 호출</summary>
    LlmCall,
    
    /// <summary>도구 호출</summary>
    ToolCall,
    
    /// <summary>조건 분기</summary>
    Branch,
    
    /// <summary>병렬 실행</summary>
    Parallel,
    
    /// <summary>순차 실행</summary>
    Sequential,
    
    /// <summary>루프 실행</summary>
    Loop,
    
    /// <summary>대기</summary>
    Wait,
    
    /// <summary>사용자 입력</summary>
    UserInput
}