namespace Agent.Core.Abstractions.Streaming.Processing;

using Agent.Core.Abstractions.Streaming.Chunks;
using Agent.Core.Abstractions.Streaming.Metrics;

/// <summary>
/// 스트림 청크 집계 인터페이스
/// </summary>
public interface IStreamAggregator
{
    /// <summary>
    /// 청크를 집계에 추가
    /// </summary>
    Task AddChunkAsync(StreamChunk chunk, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 현재까지 집계된 결과 가져오기
    /// </summary>
    Task<AggregatedResult> GetCurrentResultAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 집계 완료 처리
    /// </summary>
    Task<AggregatedResult> FinalizeAsync(CancellationToken cancellationToken = default);
}



