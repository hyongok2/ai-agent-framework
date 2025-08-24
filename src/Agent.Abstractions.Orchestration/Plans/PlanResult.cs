using System.Text.Json;

namespace Agent.Abstractions.Orchestration.Plans;

/// <summary>
/// 계획 실행 결과
/// </summary>
public sealed record PlanResult
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// 완료된 Step 수
    /// </summary>
    public int CompletedSteps { get; init; }
    
    /// <summary>
    /// 실패한 Step 수
    /// </summary>
    public int FailedSteps { get; init; }
    
    /// <summary>
    /// 건너뛴 Step 수
    /// </summary>
    public int SkippedSteps { get; init; }
    
    /// <summary>
    /// 총 실행 시간
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// 최종 출력
    /// </summary>
    public JsonDocument? FinalOutput { get; init; }
    
    /// <summary>
    /// 에러 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
}

