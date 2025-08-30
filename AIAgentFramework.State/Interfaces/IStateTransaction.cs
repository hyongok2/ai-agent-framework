using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.State.Interfaces
{
    /// <summary>
    /// 상태 트랜잭션 인터페이스
    /// </summary>
    public interface IStateTransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 트랜잭션 ID
        /// </summary>
        string TransactionId { get; }

        /// <summary>
        /// 트랜잭션 시작 시간
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// 트랜잭션 상태
        /// </summary>
        TransactionState State { get; }

        /// <summary>
        /// 트랜잭션에 포함된 세션 ID들
        /// </summary>
        IReadOnlySet<string> SessionIds { get; }

        /// <summary>
        /// 트랜잭션 내에서 상태를 조회합니다
        /// </summary>
        /// <typeparam name="T">상태 타입</typeparam>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>상태 객체</returns>
        Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 트랜잭션 내에서 상태를 설정합니다
        /// </summary>
        /// <typeparam name="T">상태 타입</typeparam>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="state">저장할 상태</param>
        /// <param name="expiry">만료 시간</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>설정 완료 Task</returns>
        Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 트랜잭션 내에서 상태를 삭제합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>삭제 완료 Task</returns>
        Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 트랜잭션을 커밋합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>커밋 완료 Task</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 트랜잭션을 롤백합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>롤백 완료 Task</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 세이브포인트를 생성합니다
        /// </summary>
        /// <param name="savepointName">세이브포인트 이름</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>세이브포인트 생성 완료 Task</returns>
        Task CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 세이브포인트로 롤백합니다
        /// </summary>
        /// <param name="savepointName">세이브포인트 이름</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>롤백 완료 Task</returns>
        Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 트랜잭션 상태
    /// </summary>
    public enum TransactionState
    {
        /// <summary>
        /// 진행 중
        /// </summary>
        Active,

        /// <summary>
        /// 커밋 준비됨
        /// </summary>
        Prepared,

        /// <summary>
        /// 커밋됨
        /// </summary>
        Committed,

        /// <summary>
        /// 롤백됨
        /// </summary>
        RolledBack,

        /// <summary>
        /// 실패함
        /// </summary>
        Failed,

        /// <summary>
        /// 시간 초과됨
        /// </summary>
        TimedOut
    }

    /// <summary>
    /// 트랜잭션 작업 타입
    /// </summary>
    public enum TransactionOperation
    {
        /// <summary>
        /// 조회
        /// </summary>
        Read,

        /// <summary>
        /// 생성
        /// </summary>
        Create,

        /// <summary>
        /// 수정
        /// </summary>
        Update,

        /// <summary>
        /// 삭제
        /// </summary>
        Delete
    }

    /// <summary>
    /// 트랜잭션 로그 항목
    /// </summary>
    public sealed record TransactionLogEntry
    {
        /// <summary>
        /// 작업 시퀀스 번호
        /// </summary>
        public required int SequenceNumber { get; init; }

        /// <summary>
        /// 세션 ID
        /// </summary>
        public required string SessionId { get; init; }

        /// <summary>
        /// 작업 타입
        /// </summary>
        public required TransactionOperation Operation { get; init; }

        /// <summary>
        /// 상태 타입
        /// </summary>
        public Type? StateType { get; init; }

        /// <summary>
        /// 이전 상태 (롤백용)
        /// </summary>
        public object? PreviousState { get; init; }

        /// <summary>
        /// 새 상태
        /// </summary>
        public object? NewState { get; init; }

        /// <summary>
        /// 작업 시간
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 만료 시간
        /// </summary>
        public TimeSpan? Expiry { get; init; }
    }

    /// <summary>
    /// 트랜잭션 예외
    /// </summary>
    public class StateTransactionException : StateProviderException
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        public StateTransactionException(string message) : base(message) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="innerException">내부 예외</param>
        public StateTransactionException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 트랜잭션 ID
        /// </summary>
        public string? TransactionId { get; init; }

        /// <summary>
        /// 트랜잭션 상태
        /// </summary>
        public TransactionState? State { get; init; }
    }

    /// <summary>
    /// 트랜잭션 시간 초과 예외
    /// </summary>
    public class TransactionTimeoutException : StateTransactionException
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="timeout">시간 초과 값</param>
        public TransactionTimeoutException(string message, TimeSpan timeout) : base(message)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// 시간 초과 값
        /// </summary>
        public TimeSpan Timeout { get; }
    }

    /// <summary>
    /// 트랜잭션 충돌 예외
    /// </summary>
    public class TransactionConflictException : StateTransactionException
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="conflictingSessionId">충돌한 세션 ID</param>
        public TransactionConflictException(string message, string conflictingSessionId) : base(message)
        {
            ConflictingSessionId = conflictingSessionId;
        }

        /// <summary>
        /// 충돌한 세션 ID
        /// </summary>
        public string ConflictingSessionId { get; }
    }
}