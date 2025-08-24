namespace Agent.Core.Abstractions.Streaming.Chunks;

/// <summary>
/// 텍스트 토큰 청크
/// </summary>
public sealed record TokenChunk : StreamChunk
{
    public required string Text { get; init; }
    public override string ChunkType => "token";
    
    /// <summary>
    /// 토큰이 문장의 끝인지 여부
    /// </summary>
    public bool IsEndOfSentence { get; init; }
}