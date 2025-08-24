using System;
using System.Collections.Generic;
using Agent.Abstractions.LLM.Models.Completion;

namespace Agent.Abstractions.LLM.Models.Embeddings;

/// <summary>
/// 임베딩 응답
/// </summary>
public sealed record EmbeddingResponse
{
    /// <summary>
    /// 모델명
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// 임베딩 목록
    /// </summary>
    public required IReadOnlyList<Embedding> Embeddings { get; init; }
    
    /// <summary>
    /// 사용량 정보
    /// </summary>
    public LlmUsage? Usage { get; init; }
    
    /// <summary>
    /// 생성 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}