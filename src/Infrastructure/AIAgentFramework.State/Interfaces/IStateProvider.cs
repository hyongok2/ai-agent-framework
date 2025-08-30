using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.State.Interfaces
{
    /// <summary>
    /// 상태 저장소 인터페이스
    /// </summary>
    public interface IStateProvider : IDisposable
    {
        /// <summary>
        /// 세션 상태를 조회합니다
        /// </summary>
        /// <typeparam name="T">상태 타입</typeparam>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>상태 객체 (없으면 null)</returns>
        Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 세션 상태를 저장합니다
        /// </summary>
        /// <typeparam name="T">상태 타입</typeparam>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="state">저장할 상태</param>
        /// <param name="expiry">만료 시간 (선택사항)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>저장 완료 Task</returns>
        Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 세션 상태가 존재하는지 확인합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>존재 여부</returns>
        Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 세션 상태를 삭제합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>삭제 완료 Task</returns>
        Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 트랜잭션을 시작합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>상태 트랜잭션</returns>
        Task<IStateTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 상태 저장소가 사용 가능한지 확인합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>사용 가능 여부</returns>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 상태 저장소 통계를 조회합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>통계 정보</returns>
        Task<StateProviderStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 만료된 상태들을 정리합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>정리된 항목 수</returns>
        Task<int> CleanupExpiredStatesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 상태 저장소 통계
    /// </summary>
    public sealed record StateProviderStatistics
    {
        /// <summary>
        /// 총 저장된 상태 수
        /// </summary>
        public int TotalStates { get; init; }

        /// <summary>
        /// 활성 세션 수
        /// </summary>
        public int ActiveSessions { get; init; }

        /// <summary>
        /// 사용된 메모리 크기 (바이트)
        /// </summary>
        public long UsedMemoryBytes { get; init; }

        /// <summary>
        /// 히트율 (0.0 ~ 1.0)
        /// </summary>
        public double HitRate { get; init; }

        /// <summary>
        /// 총 조회 횟수
        /// </summary>
        public long TotalReads { get; init; }

        /// <summary>
        /// 총 저장 횟수
        /// </summary>
        public long TotalWrites { get; init; }

        /// <summary>
        /// 평균 응답 시간 (밀리초)
        /// </summary>
        public double AverageResponseTimeMs { get; init; }

        /// <summary>
        /// 마지막 정리 시간
        /// </summary>
        public DateTime? LastCleanupTime { get; init; }

        /// <summary>
        /// 통계 수집 시간
        /// </summary>
        public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 상태 저장소 예외
    /// </summary>
    public class StateProviderException : Exception
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        public StateProviderException(string message) : base(message) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="innerException">내부 예외</param>
        public StateProviderException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 세션 ID
        /// </summary>
        public string? SessionId { get; init; }

        /// <summary>
        /// 작업 타입
        /// </summary>
        public string? OperationType { get; init; }
    }

    /// <summary>
    /// 상태 직렬화 예외
    /// </summary>
    public class StateSerializationException : StateProviderException
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="innerException">내부 예외</param>
        public StateSerializationException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 상태 타입
        /// </summary>
        public Type? StateType { get; init; }
    }

    /// <summary>
    /// 연결 예외
    /// </summary>
    public class StateConnectionException : StateProviderException
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="innerException">내부 예외</param>
        public StateConnectionException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 연결 문자열
        /// </summary>
        public string? ConnectionString { get; init; }
    }
}