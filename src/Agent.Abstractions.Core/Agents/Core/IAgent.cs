using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agent.Abstractions.Core.Common.Identifiers;
using Agent.Abstractions.Core.Streaming.Chunks;
using Agent.Abstractions.Core.Memory;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// AI 에이전트 인터페이스
/// </summary>
public interface IAgent : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 에이전트 고유 ID
    /// </summary>
    AgentId Id { get; }
    
    /// <summary>
    /// 에이전트 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 에이전트 설명
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 에이전트 버전
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 에이전트 기능
    /// </summary>
    AgentCapabilities Capabilities { get; }
    
    /// <summary>
    /// 에이전트 상태
    /// </summary>
    AgentStatus Status { get; }
    
    /// <summary>
    /// 에이전트 메타데이터
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
    
    #region 실행 메서드
    
    /// <summary>
    /// 동기 실행 (전체 응답 반환)
    /// </summary>
    /// <param name="request">요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답</returns>
    Task<AgentResponse> ExecuteAsync(
        AgentRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 스트리밍 실행 (청크 단위 반환)
    /// </summary>
    /// <param name="request">요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트림 청크</returns>
    IAsyncEnumerable<StreamChunk> ExecuteStreamAsync(
        AgentRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 배치 실행 (여러 요청 동시 처리)
    /// </summary>
    /// <param name="requests">요청 목록</param>
    /// <param name="options">배치 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 목록</returns>
    Task<IReadOnlyList<AgentResponse>> ExecuteBatchAsync(
        IReadOnlyList<AgentRequest> requests,
        BatchExecutionOptions? options = null,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 대화 관리
    
    /// <summary>
    /// 새 대화 시작
    /// </summary>
    /// <param name="userId">사용자 ID</param>
    /// <param name="initialContext">초기 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>대화 ID</returns>
    Task<string> StartConversationAsync(
        string? userId = null,
        Dictionary<string, object>? initialContext = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 대화 종료
    /// </summary>
    /// <param name="conversationId">대화 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task EndConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 대화 기록 조회
    /// </summary>
    /// <param name="conversationId">대화 ID</param>
    /// <param name="limit">최대 항목 수</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>대화 기록</returns>
    Task<IReadOnlyList<ConversationEntry>> GetConversationHistoryAsync(
        string conversationId,
        int limit = 50,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 대화 컨텍스트 업데이트
    /// </summary>
    /// <param name="conversationId">대화 ID</param>
    /// <param name="context">컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task UpdateConversationContextAsync(
        string conversationId,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 상태 및 제어
    
    /// <summary>
    /// 에이전트 초기화
    /// </summary>
    /// <param name="configuration">설정</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task InitializeAsync(
        AgentConfiguration? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 에이전트 시작
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 에이전트 중지
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 에이전트 재시작
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RestartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 상태 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>상태 정보</returns>
    Task<AgentStatusInfo> GetStatusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 헬스 체크
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>헬스 체크 결과</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 설정 및 확장
    
    /// <summary>
    /// 설정 업데이트
    /// </summary>
    /// <param name="configuration">새 설정</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task UpdateConfigurationAsync(
        AgentConfiguration configuration,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 현재 설정 조회
    /// </summary>
    /// <returns>현재 설정</returns>
    AgentConfiguration GetConfiguration();
    
    /// <summary>
    /// 도구 추가
    /// </summary>
    /// <param name="toolId">도구 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task AddToolAsync(ToolId toolId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 도구 제거
    /// </summary>
    /// <param name="toolId">도구 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RemoveToolAsync(ToolId toolId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 사용 가능한 도구 목록
    /// </summary>
    /// <returns>도구 ID 목록</returns>
    IReadOnlyList<ToolId> GetAvailableTools();
    
    #endregion
    
    #region 이벤트
    
    /// <summary>
    /// 상태 변경 이벤트
    /// </summary>
    event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    
    /// <summary>
    /// 에러 발생 이벤트
    /// </summary>
    event EventHandler<AgentErrorEventArgs>? ErrorOccurred;
    
    /// <summary>
    /// 실행 시작 이벤트
    /// </summary>
    event EventHandler<ExecutionStartedEventArgs>? ExecutionStarted;
    
    /// <summary>
    /// 실행 완료 이벤트
    /// </summary>
    event EventHandler<ExecutionCompletedEventArgs>? ExecutionCompleted;
    
    #endregion
}