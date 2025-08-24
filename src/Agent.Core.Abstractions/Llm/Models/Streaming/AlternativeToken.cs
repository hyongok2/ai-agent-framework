namespace Agent.Core.Abstractions.Llm.Models.Streaming;

/// <summary>
/// 대체 토큰
/// </summary>
public sealed record AlternativeToken
{
    /// <summary>
    /// 토큰 텍스트
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// 토큰 ID
    /// </summary>
    public int? TokenId { get; init; }
    
    /// <summary>
    /// 로그 확률
    /// </summary>
    public double LogProb { get; init; }
}