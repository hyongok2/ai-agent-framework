namespace Agent.Core.Abstractions.Streaming.Metrics;

using System.Text.Json;
using Agent.Core.Abstractions.Streaming.Chunks;

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