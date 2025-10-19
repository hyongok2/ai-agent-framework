namespace AIAgentFramework.Core.Models;

/// <summary>
/// Agent 이벤트 기본 클래스
/// </summary>
public abstract record AgentEventBase
{
    public string ExecutionId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 사용자 입력 이벤트
/// </summary>
public record AgentInputEvent : AgentEventBase
{
    public string Input { get; init; } = string.Empty;
}

/// <summary>
/// Agent 실행 단계 이벤트
/// </summary>
public record AgentPhaseEvent : AgentEventBase
{
    public string Phase { get; init; } = string.Empty; // "IntentAnalysis", "Planning", "Execution"
    public string? Message { get; init; }
}

/// <summary>
/// Intent 분석 완료 이벤트
/// </summary>
public record IntentAnalysisCompleteEvent : AgentEventBase
{
    public string IntentType { get; init; } = string.Empty; // "Chat", "Question", "Task"
    public string? TaskDescription { get; init; }
    public string? DirectResponse { get; init; }
    public double Confidence { get; init; }
}

/// <summary>
/// 계획 수립 완료 이벤트
/// </summary>
public record PlanningCompleteEvent : AgentEventBase
{
    public string Summary { get; init; } = string.Empty;
    public int StepCount { get; init; }
    public int EstimatedSeconds { get; init; }
    public bool IsExecutable { get; init; }
}

/// <summary>
/// 단계 실행 이벤트
/// </summary>
public record StepExecutionEvent : AgentEventBase
{
    public int StepNumber { get; init; }
    public int TotalSteps { get; init; }
    public string Description { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>
/// 응답 청크 이벤트 (스트리밍)
/// </summary>
public record ResponseChunkEvent : AgentEventBase
{
    public string Content { get; init; } = string.Empty;
    public string ChunkType { get; init; } = "Text"; // "Text", "Status", "Thinking"
}

/// <summary>
/// 응답 완료 이벤트
/// </summary>
public record ResponseCompleteEvent : AgentEventBase
{
    public string FullResponse { get; init; } = string.Empty;
    public long TotalDurationMs { get; init; }
    public int TokensUsed { get; init; }
}

/// <summary>
/// 오류 이벤트
/// </summary>
public record AgentErrorEvent : AgentEventBase
{
    public string ErrorMessage { get; init; } = string.Empty;
    public string? Phase { get; init; }
    public string? StackTrace { get; init; }
    public bool IsRecoverable { get; init; }
}
