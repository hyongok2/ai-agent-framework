using System.Collections.Generic;
using System.Threading;
using Agent.Abstractions.Core.Streaming.Chunks;
using Agent.Abstractions.Orchestration.Execution;

namespace Agent.Abstractions.Orchestration.Core;

/// <summary>
/// 오케스트레이션 타입별 실행기 인터페이스
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// 실행 컨텍스트를 받아 스트리밍 응답 생성
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트림 청크</returns>
    IAsyncEnumerable<StreamChunk> ExecuteAsync(
        RunContext context, 
        CancellationToken cancellationToken = default);
}