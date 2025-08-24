using System;
using System.Collections.Generic;

namespace Agent.Core.Abstractions.Llm.Models.Streaming;

/// <summary>
/// 토큰 정보
/// </summary>
public sealed record TokenInfo
{
    /// <summary>
    /// 토큰 텍스트
    /// </summary>
    public string? Text { get; init; }
    
    /// <summary>
    /// 토큰 ID
    /// </summary>
    public int? TokenId { get; init; }
    
    /// <summary>
    /// 로그 확률
    /// </summary>
    public double? LogProb { get; init; }
    
    /// <summary>
    /// 바이트 오프셋
    /// </summary>
    public IReadOnlyList<int> ByteOffsets { get; init; } = Array.Empty<int>();
    
    /// <summary>
    /// 대체 토큰들 (top-k)
    /// </summary>
    public IReadOnlyList<AlternativeToken> Alternatives { get; init; } = Array.Empty<AlternativeToken>();
}