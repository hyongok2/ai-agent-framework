using System;
using System.Text.Json;
using Agent.Core.Abstractions.Common.Identifiers;

namespace Agent.Core.Abstractions.Streaming.Chunks;

/// <summary>
/// 스트리밍 이벤트의 기본 타입
/// </summary>
public abstract record StreamChunk
{
    public required RunId RunId { get; init; }
    public required StepId StepId { get; init; }
    public required long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 청크 타입 식별자
    /// </summary>
    public abstract string ChunkType { get; }
}
