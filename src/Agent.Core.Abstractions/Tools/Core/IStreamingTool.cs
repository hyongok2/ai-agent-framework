using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using Agent.Core.Abstractions.Tools.Execution;

namespace Agent.Core.Abstractions.Tools.Core;

/// <summary>
/// 비동기 스트리밍 도구 인터페이스
/// </summary>
public interface IStreamingTool : ITool
{
    /// <summary>
    /// 스트리밍 실행
    /// </summary>
    /// <param name="input">입력 데이터</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 스트림 결과</returns>
    IAsyncEnumerable<ToolStreamChunk> ExecuteStreamAsync(
        JsonNode input, 
        ToolContext context, 
        CancellationToken cancellationToken = default);
}