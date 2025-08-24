namespace Agent.Core.Abstractions.Llm.Models.Embeddings;

/// <summary>
/// 임베딩
/// </summary>
public sealed record Embedding
{
    /// <summary>
    /// 인덱스
    /// </summary>
    public int Index { get; init; }
    
    /// <summary>
    /// 벡터
    /// </summary>
    public required float[] Vector { get; init; }
    
    /// <summary>
    /// 원본 텍스트
    /// </summary>
    public string? Text { get; init; }
    
    /// <summary>
    /// 토큰 수
    /// </summary>
    public int? TokenCount { get; init; }
}