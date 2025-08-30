using AIAgentFramework.Configuration.Interfaces;
using AIAgentFramework.Configuration.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.Configuration.Cache
{
    /// <summary>
    /// 타입 안전한 설정 캐시 구현
    /// </summary>
    public class ConfigurationCache : IConfigurationCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ConfigurationCache> _logger;
        private readonly ConcurrentSet<string> _cacheKeys;
        private readonly CacheStatisticsTracker _statistics;
        private volatile bool _disposed;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="memoryCache">메모리 캐시</param>
        /// <param name="logger">로거</param>
        public ConfigurationCache(IMemoryCache memoryCache, ILogger<ConfigurationCache> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheKeys = new ConcurrentSet<string>();
            _statistics = new CacheStatisticsTracker();
        }

        /// <inheritdoc />
        public void Invalidate(string? keyPattern = null)
        {
            ThrowIfDisposed();

            if (keyPattern == null)
            {
                InvalidateAll();
                return;
            }

            try
            {
                var matchingKeys = _cacheKeys
                    .Where(key => IsKeyMatching(key, keyPattern))
                    .ToList();

                var removedCount = 0;
                foreach (var key in matchingKeys)
                {
                    if (RemoveKeyInternal(key))
                        removedCount++;
                }

                _logger.LogInformation("패턴 '{Pattern}' 캐시 무효화 완료: {RemovedCount}개 키 제거", keyPattern, removedCount);
                _statistics.RecordInvalidation(removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "패턴 기반 캐시 무효화 실패: {Pattern}", keyPattern);
                throw;
            }
        }

        /// <inheritdoc />
        public void InvalidateAll()
        {
            ThrowIfDisposed();

            try
            {
                var keySnapshot = _cacheKeys.ToArray();
                var removedCount = 0;

                foreach (var key in keySnapshot)
                {
                    if (RemoveKeyInternal(key))
                        removedCount++;
                }

                _cacheKeys.Clear();

                _logger.LogInformation("전체 설정 캐시 무효화 완료: {RemovedCount}개 키 제거", removedCount);
                _statistics.RecordInvalidation(removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "전체 캐시 무효화 실패");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task WarmupAsync(IEnumerable<string> keys)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(keys);

            var keyList = keys.ToList();
            _logger.LogInformation("캐시 워밍업 시작: {KeyCount}개 키", keyList.Count);

            try
            {
                var warmupTasks = keyList.Select(async key =>
                {
                    try
                    {
                        // 실제 설정 로딩 로직은 ConfigurationManager에서 처리
                        // 여기서는 키가 유효한지만 확인
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            _cacheKeys.TryAdd(key);
                            _logger.LogDebug("캐시 키 예약됨: {Key}", key);
                        }
                        await Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "캐시 키 워밍업 실패: {Key}", key);
                    }
                });

                await Task.WhenAll(warmupTasks);
                _logger.LogInformation("캐시 워밍업 완료: {KeyCount}개 키", keyList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "캐시 워밍업 실패");
                throw;
            }
        }

        /// <inheritdoc />
        public CacheStatistics GetStatistics()
        {
            ThrowIfDisposed();
            return _statistics.GetStatistics(_cacheKeys.Count);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var exists = _cacheKeys.Contains(key) && _memoryCache.TryGetValue(key, out _);
            
            if (exists)
                _statistics.RecordHit();
            else
                _statistics.RecordMiss();

            return exists;
        }

        /// <inheritdoc />
        public bool RemoveKey(string key)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            return RemoveKeyInternal(key);
        }

        /// <inheritdoc />
        public IReadOnlySet<string> GetCachedKeys()
        {
            ThrowIfDisposed();
            return _cacheKeys.ToList().ToHashSet();
        }

        /// <summary>
        /// 캐시에 키를 추가합니다 (내부 사용)
        /// </summary>
        /// <param name="key">추가할 키</param>
        internal void TrackKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _cacheKeys.TryAdd(key);
                _logger.LogDebug("캐시 키 추적 시작: {Key}", key);
            }
        }

        /// <summary>
        /// 키 패턴 매칭 확인
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <param name="pattern">패턴</param>
        /// <returns>매칭 여부</returns>
        private static bool IsKeyMatching(string key, string pattern)
        {
            // 간단한 와일드카드 패턴 지원 (* → .*, ? → .)
            if (pattern.Contains('*') || pattern.Contains('?'))
            {
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                
                return Regex.IsMatch(key, regexPattern, RegexOptions.IgnoreCase);
            }

            // 부분 문자열 매칭
            return key.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 키를 내부적으로 제거합니다
        /// </summary>
        /// <param name="key">제거할 키</param>
        /// <returns>제거 성공 여부</returns>
        private bool RemoveKeyInternal(string key)
        {
            var removed = false;

            if (_cacheKeys.TryRemove(key))
            {
                _memoryCache.Remove(key);
                removed = true;
                _logger.LogDebug("캐시 키 제거됨: {Key}", key);
            }

            return removed;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 리소스를 해제합니다
        /// </summary>
        /// <param name="disposing">관리 리소스 해제 여부</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cacheKeys.Dispose();
                _statistics.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConfigurationCache));
        }
    }

    /// <summary>
    /// 캐시 통계 추적기
    /// </summary>
    internal class CacheStatisticsTracker : IDisposable
    {
        private long _hitCount;
        private long _missCount;
        private long _invalidationCount;
        private readonly DateTime _createdAt;
        private DateTime _lastAccessedAt;
        private volatile bool _disposed;

        public CacheStatisticsTracker()
        {
            _createdAt = DateTime.UtcNow;
            _lastAccessedAt = _createdAt;
        }

        public void RecordHit()
        {
            if (_disposed) return;
            
            Interlocked.Increment(ref _hitCount);
            _lastAccessedAt = DateTime.UtcNow;
        }

        public void RecordMiss()
        {
            if (_disposed) return;
            
            Interlocked.Increment(ref _missCount);
            _lastAccessedAt = DateTime.UtcNow;
        }

        public void RecordInvalidation(int count)
        {
            if (_disposed) return;
            
            Interlocked.Add(ref _invalidationCount, count);
        }

        public CacheStatistics GetStatistics(int currentKeyCount)
        {
            if (_disposed)
                return new CacheStatistics();

            var hitCount = Interlocked.Read(ref _hitCount);
            var missCount = Interlocked.Read(ref _missCount);
            var total = hitCount + missCount;
            var hitRate = total > 0 ? (double)hitCount / total : 0.0;

            return new CacheStatistics
            {
                TotalKeys = currentKeyCount,
                HitRate = hitRate,
                HitCount = hitCount,
                MissCount = missCount,
                CreatedAt = _createdAt,
                LastAccessedAt = _lastAccessedAt
            };
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}