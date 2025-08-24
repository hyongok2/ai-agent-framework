namespace Agent.Abstractions.Core.Streaming.Metrics;

/// <summary>
/// 실행 메트릭
/// </summary>
public sealed record ExecutionMetrics
{
    public TimeSpan Duration { get; init; }
    public int TokensUsed { get; init; }
    public int ToolCallsCount { get; init; }
    public int StepsExecuted { get; init; }
    public decimal? Cost { get; init; }
}