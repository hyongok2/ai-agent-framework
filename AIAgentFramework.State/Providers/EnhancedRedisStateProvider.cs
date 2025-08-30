using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AIAgentFramework.State.Providers
{
    /// <summary>
    /// 향상된 Redis 상태 저장소 - 배치 연산 지원
    /// </summary>
    public class EnhancedRedisStateProvider : IStateProvider, IBatchStateProvider
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly RedisStateOptions _options;
        private readonly ILogger<EnhancedRedisStateProvider> _logger;
        
        // 성능 카운터
        private long _totalReads;
        private long _totalWrites;
        private long _hitCount;
        private long _missCount;
        private volatile bool _disposed;
        
        public EnhancedRedisStateProvider(
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<RedisStateOptions> options,
            ILogger<EnhancedRedisStateProvider> logger)
        {
            _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
            _options = options?.Value ?? new RedisStateOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _database = _connectionMultiplexer.GetDatabase(_options.Database);
            
            _logger.LogInformation("EnhancedRedisStateProvider 초기화 완료: DB={Database}, Prefix={KeyPrefix}", 
                _options.Database, _options.KeyPrefix);
        }
        
        #region IStateProvider Implementation
        
        public async Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ThrowIfDisposed();
            
            Interlocked.Increment(ref _totalReads);
            
            try
            {
                var key = GetRedisKey(sessionId);
                var value = await _database.StringGetAsync(key);
                
                if (value.HasValue)
                {
                    Interlocked.Increment(ref _hitCount);
                    var deserializedValue = JsonSerializer.Deserialize<T>(value!);
                    _logger.LogDebug("상태 조회 성공: {SessionId}", sessionId);
                    return deserializedValue;
                }
                else
                {
                    Interlocked.Increment(ref _missCount);
                    _logger.LogDebug("상태 없음: {SessionId}", sessionId);
                    return null;
                }
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 상태 조회 실패: {SessionId}", sessionId);
                throw new StateConnectionException("Redis 연결 오류", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 조회 실패: {SessionId}", sessionId);
                throw new StateProviderException($"상태 조회 실패: {sessionId}", ex);
            }
        }
        
        public async Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentNullException.ThrowIfNull(state);
            ThrowIfDisposed();
            
            Interlocked.Increment(ref _totalWrites);
            
            try
            {
                var key = GetRedisKey(sessionId);
                var serializedValue = JsonSerializer.Serialize(state);
                var actualExpiry = expiry ?? _options.DefaultExpiry;
                
                bool success;
                if (actualExpiry.HasValue)
                {
                    success = await _database.StringSetAsync(key, serializedValue, actualExpiry.Value);
                }
                else
                {
                    success = await _database.StringSetAsync(key, serializedValue);
                }
                
                if (!success)
                {
                    throw new StateProviderException($"Redis 상태 저장 실패: {sessionId}");
                }
                
                _logger.LogDebug("상태 저장 성공: {SessionId}, TTL: {TTL}", sessionId, actualExpiry);
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 상태 저장 실패: {SessionId}", sessionId);
                throw new StateConnectionException("Redis 연결 오류", ex);
            }
            catch (Exception ex) when (!(ex is StateProviderException))
            {
                _logger.LogError(ex, "상태 저장 실패: {SessionId}", sessionId);
                throw new StateProviderException($"상태 저장 실패: {sessionId}", ex);
            }
        }
        
        public async Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ThrowIfDisposed();
            
            try
            {
                var key = GetRedisKey(sessionId);
                var exists = await _database.KeyExistsAsync(key);
                return exists;
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 상태 존재 확인 실패: {SessionId}", sessionId);
                throw new StateConnectionException("Redis 연결 오류", ex);
            }
        }
        
        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ThrowIfDisposed();
            
            try
            {
                var key = GetRedisKey(sessionId);
                var deleted = await _database.KeyDeleteAsync(key);
                
                if (deleted)
                {
                    _logger.LogDebug("상태 삭제 성공: {SessionId}", sessionId);
                }
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 상태 삭제 실패: {SessionId}", sessionId);
                throw new StateConnectionException("Redis 연결 오류", ex);
            }
        }
        
        public Task<IStateTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var transaction = new EnhancedRedisStateTransaction(_database, _logger);
            return Task.FromResult<IStateTransaction>(transaction);
        }
        
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            try
            {
                var pingResult = await _database.PingAsync();
                return pingResult != TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 연결 상태 확인 실패");
                return false;
            }
        }
        
        public Task<StateProviderStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            var statistics = new StateProviderStatistics
            {
                TotalStates = -1, // Redis에서 계산하기 어려움
                ActiveSessions = -1,
                UsedMemoryBytes = 0,
                HitRate = _totalReads > 0 ? (double)_hitCount / _totalReads : 0,
                TotalReads = _totalReads,
                TotalWrites = _totalWrites,
                AverageResponseTimeMs = 10.0, // Redis는 일반적으로 빠름
                LastCleanupTime = null,
                CollectedAt = DateTime.UtcNow
            };
            
            return Task.FromResult(statistics);
        }
        
        public Task<int> CleanupExpiredStatesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            // Redis는 TTL 기반으로 자동 만료되므로 별도 정리가 필요 없음
            _logger.LogDebug("Redis 자동 만료 정리 완료");
            return Task.FromResult(0);
        }
        
        #endregion
        
        #region IBatchStateProvider Implementation
        
        /// <summary>
        /// 여러 상태를 배치로 가져오기
        /// </summary>
        public async Task<IDictionary<string, T?>> GetBatchAsync<T>(
            IEnumerable<string> sessionIds, 
            CancellationToken cancellationToken = default) where T : class
        {
            var sessionIdList = sessionIds?.ToList() ?? throw new ArgumentNullException(nameof(sessionIds));
            ThrowIfDisposed();
            
            if (!sessionIdList.Any())
                return new Dictionary<string, T?>();
            
            try
            {
                var results = new Dictionary<string, T?>();
                
                // 파이프라인을 사용한 배치 처리
                var tasks = new List<Task<(string SessionId, RedisValue Value)>>();
                
                foreach (var sessionId in sessionIdList)
                {
                    var key = GetRedisKey(sessionId);
                    var task = _database.StringGetAsync(key)
                        .ContinueWith(t => (sessionId, t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
                    tasks.Add(task);
                }
                
                var batchResults = await Task.WhenAll(tasks);
                
                foreach (var (sessionId, value) in batchResults)
                {
                    if (value.HasValue)
                    {
                        try
                        {
                            var deserializedValue = JsonSerializer.Deserialize<T>(value!);
                            results[sessionId] = deserializedValue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "배치 역직렬화 실패: {SessionId}", sessionId);
                            results[sessionId] = null;
                        }
                    }
                    else
                    {
                        results[sessionId] = null;
                    }
                }
                
                Interlocked.Add(ref _totalReads, sessionIdList.Count);
                Interlocked.Add(ref _hitCount, results.Count(r => r.Value != null));
                Interlocked.Add(ref _missCount, results.Count(r => r.Value == null));
                
                _logger.LogDebug("배치 읽기 완료: {Count}개 키, {SuccessCount}개 성공", 
                    sessionIdList.Count, results.Count(r => r.Value != null));
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "배치 상태 조회 실패: {Count}개 키", sessionIdList.Count);
                throw new StateProviderException($"배치 상태 조회 실패: {sessionIdList.Count}개 키", ex);
            }
        }
        
        /// <summary>
        /// 여러 상태를 배치로 저장하기
        /// </summary>
        public async Task SetBatchAsync<T>(
            IDictionary<string, T> sessionData,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(sessionData);
            ThrowIfDisposed();
            
            if (!sessionData.Any())
                return;
            
            try
            {
                var batch = _database.CreateBatch();
                var tasks = new List<Task>();
                
                foreach (var kvp in sessionData)
                {
                    var key = GetRedisKey(kvp.Key);
                    var serializedValue = JsonSerializer.Serialize(kvp.Value);
                    var actualExpiry = expiry ?? _options.DefaultExpiry;
                    
                    Task task;
                    if (actualExpiry.HasValue)
                    {
                        task = batch.StringSetAsync(key, serializedValue, actualExpiry.Value);
                    }
                    else
                    {
                        task = batch.StringSetAsync(key, serializedValue);
                    }
                    tasks.Add(task);
                }
                
                batch.Execute();
                await Task.WhenAll(tasks);
                
                Interlocked.Add(ref _totalWrites, sessionData.Count);
                
                _logger.LogDebug("배치 상태 저장 성공: {Count}개 키", sessionData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "배치 상태 저장 실패: {Count}개 키", sessionData.Count);
                throw new StateProviderException($"배치 상태 저장 실패: {sessionData.Count}개 키", ex);
            }
        }
        
        /// <summary>
        /// 여러 상태를 배치로 삭제하기
        /// </summary>
        public async Task DeleteBatchAsync(
            IEnumerable<string> sessionIds,
            CancellationToken cancellationToken = default)
        {
            var sessionIdList = sessionIds?.ToList() ?? throw new ArgumentNullException(nameof(sessionIds));
            ThrowIfDisposed();
            
            if (!sessionIdList.Any())
                return;
            
            try
            {
                var keys = sessionIdList.Select(GetRedisKey).ToArray();
                var deletedCount = await _database.KeyDeleteAsync(keys);
                
                _logger.LogDebug("배치 상태 삭제 성공: {DeletedCount}/{TotalCount}개 키", 
                    deletedCount, sessionIdList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "배치 상태 삭제 실패: {Count}개 키", sessionIdList.Count);
                throw new StateProviderException($"배치 상태 삭제 실패: {sessionIdList.Count}개 키", ex);
            }
        }
        
        /// <summary>
        /// 여러 상태의 존재 여부를 배치로 확인
        /// </summary>
        public async Task<IDictionary<string, bool>> ExistsBatchAsync(
            IEnumerable<string> sessionIds,
            CancellationToken cancellationToken = default)
        {
            var sessionIdList = sessionIds?.ToList() ?? throw new ArgumentNullException(nameof(sessionIds));
            ThrowIfDisposed();
            
            if (!sessionIdList.Any())
                return new Dictionary<string, bool>();
            
            try
            {
                var results = new Dictionary<string, bool>();
                
                // 개별적으로 존재 확인 (배치 대신)
                foreach (var sessionId in sessionIdList)
                {
                    var key = GetRedisKey(sessionId);
                    var exists = await _database.KeyExistsAsync(key);
                    results[sessionId] = exists;
                }
                
                _logger.LogDebug("배치 존재 확인 완료: {Count}개 키", sessionIdList.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "배치 상태 존재 확인 실패: {Count}개 키", sessionIdList.Count);
                throw new StateProviderException($"배치 상태 존재 확인 실패: {sessionIdList.Count}개 키", ex);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private RedisKey GetRedisKey(string sessionId)
        {
            return $"{_options.KeyPrefix}{sessionId}";
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EnhancedRedisStateProvider));
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            
            // ConnectionMultiplexer는 DI 컨테이너에서 관리되므로 여기서 dispose하지 않음
            _logger.LogInformation("EnhancedRedisStateProvider 리소스 해제 완료");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 향상된 Redis 상태 트랜잭션 (간단한 구현)
    /// </summary>
    internal class EnhancedRedisStateTransaction : IStateTransaction
    {
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private readonly Dictionary<string, object?> _transactionData;
        private TransactionState _state;
        
        public string TransactionId { get; }
        public DateTime StartTime { get; }
        public TransactionState State => _state;
        public IReadOnlySet<string> SessionIds => _transactionData.Keys.ToHashSet();
        
        public EnhancedRedisStateTransaction(IDatabase database, ILogger logger)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transactionData = new Dictionary<string, object?>();
            
            TransactionId = Guid.NewGuid().ToString();
            StartTime = DateTime.UtcNow;
            _state = TransactionState.Active;
        }
        
        public async Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class
        {
            if (_state != TransactionState.Active)
                throw new StateTransactionException("트랜잭션이 활성 상태가 아닙니다");
                
            // 트랜잭션 내에서 수정된 값이 있는지 확인
            if (_transactionData.ContainsKey(sessionId))
            {
                return _transactionData[sessionId] as T;
            }
            
            // Redis에서 현재 값 조회
            var key = $"state:{sessionId}";
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
                return null;
                
            return JsonSerializer.Deserialize<T>(value!);
        }
        
        public Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            if (_state != TransactionState.Active)
                throw new StateTransactionException("트랜잭션이 활성 상태가 아닙니다");
                
            _transactionData[sessionId] = state;
            return Task.CompletedTask;
        }
        
        public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (_state != TransactionState.Active)
                throw new StateTransactionException("트랜잭션이 활성 상태가 아닙니다");
                
            _transactionData[sessionId] = null; // null = 삭제 표시
            return Task.CompletedTask;
        }
        
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_state != TransactionState.Active)
                throw new StateTransactionException("트랜잭션이 활성 상태가 아닙니다");
                
            try
            {
                foreach (var kvp in _transactionData)
                {
                    var key = $"state:{kvp.Key}";
                    
                    if (kvp.Value == null)
                    {
                        await _database.KeyDeleteAsync(key);
                    }
                    else
                    {
                        var json = JsonSerializer.Serialize(kvp.Value);
                        await _database.StringSetAsync(key, json);
                    }
                }
                
                _state = TransactionState.Committed;
                _logger.LogDebug("트랜잭션 커밋 완료: {TransactionId}", TransactionId);
            }
            catch (Exception ex)
            {
                _state = TransactionState.Failed;
                _logger.LogError(ex, "트랜잭션 커밋 실패: {TransactionId}", TransactionId);
                throw;
            }
        }
        
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            _state = TransactionState.RolledBack;
            _transactionData.Clear();
            _logger.LogDebug("트랜잭션 롤백 완료: {TransactionId}", TransactionId);
            return Task.CompletedTask;
        }
        
        public Task CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("간단한 트랜잭션은 세이브포인트를 지원하지 않습니다");
        }
        
        public Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("간단한 트랜잭션은 세이브포인트를 지원하지 않습니다");
        }
        
        public void Dispose()
        {
            if (_state == TransactionState.Active)
            {
                RollbackAsync().Wait();
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_state == TransactionState.Active)
            {
                await RollbackAsync();
            }
        }
    }
}