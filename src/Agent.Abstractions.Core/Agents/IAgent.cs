using Agent.Abstractions.Core.Common.Identifiers;
using Agent.Abstractions.Core.Streaming.Chunks;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 인터페이스 - Orchestration 시스템의 간단한 래퍼
/// </summary>
public interface IAgent : IDisposable
{
    /// <summary>에이전트 고유 ID</summary>
    AgentId Id { get; }
    
    /// <summary>에이전트 이름</summary>
    string Name { get; }
    
    /// <summary>
    /// 요청 실행 (동기 모드)
    /// </summary>
    /// <param name="message">사용자 메시지</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 응답</returns>
    Task<string> ExecuteAsync(string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 요청 실행 (스트리밍 모드) - 기존 StreamChunk 활용
    /// </summary>
    /// <param name="message">사용자 메시지</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트림 청크들</returns>
    IAsyncEnumerable<StreamChunk> ExecuteStreamAsync(string message, CancellationToken cancellationToken = default);
}