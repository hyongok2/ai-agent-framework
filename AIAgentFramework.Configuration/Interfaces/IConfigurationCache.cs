using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIAgentFramework.Configuration.Interfaces
{
    /// <summary>
    /// 설정 캐시 관리 인터페이스
    /// </summary>
    public interface IConfigurationCache
    {
        /// <summary>
        /// 특정 패턴의 캐시 키를 무효화합니다
        /// </summary>
        /// <param name="keyPattern">키 패턴 (null이면 전체 캐시 클리어)</param>
        void Invalidate(string? keyPattern = null);

        /// <summary>
        /// 모든 캐시를 무효화합니다
        /// </summary>
        void InvalidateAll();

        /// <summary>
        /// 지정된 키들을 미리 워밍업합니다
        /// </summary>
        /// <param name="keys">워밍업할 키 목록</param>
        /// <returns>워밍업 작업</returns>
        Task WarmupAsync(IEnumerable<string> keys);

        /// <summary>
        /// 캐시 통계 정보를 가져옵니다
        /// </summary>
        /// <returns>캐시 통계</returns>
        CacheStatistics GetStatistics();

        /// <summary>
        /// 캐시 키가 존재하는지 확인합니다
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <returns>존재 여부</returns>
        bool ContainsKey(string key);

        /// <summary>
        /// 특정 키의 캐시를 제거합니다
        /// </summary>
        /// <param name="key">제거할 키</param>
        /// <returns>제거 성공 여부</returns>
        bool RemoveKey(string key);

        /// <summary>
        /// 캐시된 모든 키 목록을 가져옵니다
        /// </summary>
        /// <returns>캐시 키 목록</returns>
        IReadOnlySet<string> GetCachedKeys();
    }

    /// <summary>
    /// 캐시 통계 정보
    /// </summary>
    public sealed record CacheStatistics
    {
        /// <summary>
        /// 총 캐시 항목 수
        /// </summary>
        public int TotalKeys { get; init; }

        /// <summary>
        /// 캐시 적중률 (0.0 ~ 1.0)
        /// </summary>
        public double HitRate { get; init; }

        /// <summary>
        /// 총 히트 수
        /// </summary>
        public long HitCount { get; init; }

        /// <summary>
        /// 총 미스 수
        /// </summary>
        public long MissCount { get; init; }

        /// <summary>
        /// 캐시 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// 마지막 액세스 시간
        /// </summary>
        public DateTime LastAccessedAt { get; init; }
    }
}