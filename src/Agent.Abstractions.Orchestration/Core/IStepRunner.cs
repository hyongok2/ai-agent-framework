using System.Collections.Generic;
using System.Threading;
using Agent.Abstractions.Core.Streaming.Chunks;
using Agent.Abstractions.Orchestration.Execution;

namespace Agent.Abstractions.Orchestration.Core;

/// <summary>
/// Step 단위 실행 엔진 인터페이스
/// </summary>
public interface IStepRunner
{
    /// <summary>
    /// Step을 실행하고 결과를 스트리밍으로 반환
    /// </summary>
    /// <param name="step">실행할 Step</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트림 청크</returns>
    IAsyncEnumerable<StreamChunk> RunStepAsync(
        ExecutionStep executionStep, 
        RunContext context, 
        CancellationToken cancellationToken = default);
}