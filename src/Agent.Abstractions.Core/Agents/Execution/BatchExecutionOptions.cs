namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 배치 실행 옵션
/// </summary>
public sealed record BatchExecutionOptions
{
    /// <summary>병렬 실행 수</summary>
    public int Parallelism { get; init; } = 5;
    
    /// <summary>실패 시 중단</summary>
    public bool StopOnFirstFailure { get; init; }
    
    /// <summary>배치 타임아웃 (초)</summary>
    public int BatchTimeoutSeconds { get; init; } = 300;
}