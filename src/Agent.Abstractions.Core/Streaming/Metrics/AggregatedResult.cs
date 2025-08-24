using Agent.Abstractions.Core.Streaming.Chunks;

namespace Agent.Abstractions.Core.Streaming.Metrics;
/// <summary>
/// 집계된 결과
/// </summary>
public sealed record AggregatedResult
{
    public string? FullText { get; init; }
    public List<ToolCallChunk> ToolCalls { get; init; } = new();
    public JsonDocument? FinalJson { get; init; }
    public StatusType CurrentStatus { get; init; }
    public List<ErrorChunk> Errors { get; init; } = new();
    public ExecutionMetrics? Metrics { get; init; }
}