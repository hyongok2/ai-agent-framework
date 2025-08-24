namespace Agent.Core.Abstractions.Streaming.Processing;

using Agent.Core.Abstractions.Streaming.Chunks;

/// <summary>
/// 스트림 청크 필터
/// </summary>
public interface IStreamFilter
{
    /// <summary>
    /// 청크 필터링
    /// </summary>
    bool ShouldProcess(StreamChunk chunk);
    
    /// <summary>
    /// 청크 변환
    /// </summary>
    StreamChunk? Transform(StreamChunk chunk);
}