using Agent.Abstractions.Core.Streaming.Chunks;

namespace Agent.Abstractions.Core.Streaming.Processing;
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