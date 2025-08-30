using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.State.Interfaces
{
    /// <summary>
    /// 배치 연산을 지원하는 상태 저장소 인터페이스
    /// 성능 최적화를 위한 여러 상태 동시 처리 지원
    /// </summary>
    public interface IBatchStateProvider
    {
        /// <summary>
        /// 여러 세션의 상태를 배치로 조회
        /// </summary>
        /// <typeparam name="T">상태 타입</typeparam>
        /// <param name="sessionIds">조회할 세션 ID 목록</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>세션 ID와 상태의 매핑</returns>
        Task<IDictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> sessionIds, 
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 여러 세션의 상태를 배치로 저장
        /// </summary>
        /// <typeparam name="T">상태 타입</typeparam>
        /// <param name="sessionData">세션 ID와 상태의 매핑</param>
        /// <param name="expiry">만료 시간 (선택적)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task SetBatchAsync<T>(
            IDictionary<string, T> sessionData,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 여러 세션의 상태를 배치로 삭제
        /// </summary>
        /// <param name="sessionIds">삭제할 세션 ID 목록</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task DeleteBatchAsync(
            IEnumerable<string> sessionIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 여러 세션의 존재 여부를 배치로 확인
        /// </summary>
        /// <param name="sessionIds">확인할 세션 ID 목록</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>세션 ID와 존재 여부의 매핑</returns>
        Task<IDictionary<string, bool>> ExistsBatchAsync(
            IEnumerable<string> sessionIds,
            CancellationToken cancellationToken = default);
    }
}