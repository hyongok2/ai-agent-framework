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
    /// Redis 기반 상태 저장소 (프로덕션용)
    /// </summary>
    public class RedisStateProvider : IStateProvider
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly ILogger<RedisStateProvider> _logger;
        private readonly RedisStateOptions _options;
        
        // 통계 추적
        private long _totalReads;
        private long _totalWrites;
        private long _hitCount;
        private long _missCount;
        
        public RedisStateProvider(
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<RedisStateOptions> options,
            ILogger<RedisStateProvider> logger)
        {
            _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
            _options = options?.Value ?? new RedisStateOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _database = _connectionMultiplexer.GetDatabase(_options.Database);
            
            _logger.LogInformation("RedisStateProvider 초기화 완료: DB={Database}, Prefix={KeyPrefix}", 
                _options.Database, _options.KeyPrefix);
        }
        
        public async Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            
            Interlocked.Increment(ref _totalReads);
            
            try
            {
                var key = GetRedisKey(sessionId);
                var value = await _database.StringGetAsync(key);

                if (!value.HasValue)
                {
                    Interlocked.Increment(ref _missCount);
                    _logger.LogDebug("상태 없음: {SessionId}", sessionId);
                    return null;
                }

                try
                {
                    var state = JsonSerializer.Deserialize<T>(value!);
                    Interlocked.Increment(ref _hitCount);
                    _logger.LogDebug("상태 조회 성공: {SessionId}, 타입: {Type}", sessionId, typeof(T).Name);
                    return state;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "상태 역직렬화 실패: {SessionId}", sessionId);
                    throw new StateSerializationException("상태 역직렬화 실패", ex) { StateType = typeof(T) };
                }
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 상태 조회 실패: {SessionId}", sessionId);
                throw new StateConnectionException("Redis 연결 오류", ex);
            }
        }
        
        public async Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentNullException.ThrowIfNull(state);
            
            Interlocked.Increment(ref _totalWrites);
            
            try
            {
                var key = GetRedisKey(sessionId);
                var json = JsonSerializer.Serialize(state);
                var actualExpiry = expiry ?? _options.DefaultExpiry;
                
                bool success;
                if (actualExpiry.HasValue)
                {
                    success = await _database.StringSetAsync(key, json, actualExpiry.Value);
                }
                else
                {
                    success = await _database.StringSetAsync(key, json);
                }
                
                if (success)
                {
                    _logger.LogDebug("상태 저장 성공: {SessionId}, 타입: {Type}, 만료: {Expiry}", 
                        sessionId, typeof(T).Name, actualExpiry);
                }
                else
                {
                    throw new StateProviderException($"상태 저장 실패: {sessionId}");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "상태 직렬화 실패: {SessionId}", sessionId);
                throw new StateSerializationException("상태 직렬화 실패", ex) { StateType = typeof(T) };
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 상태 저장 실패: {SessionId}", sessionId);
                throw new StateConnectionException("Redis 연결 오류", ex);
            }
        }
        
        public async Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            
            try
            {
                var key = GetRedisKey(sessionId);
                var exists = await _database.KeyExistsAsync(key);
                return exists;
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 존재 확인 실패: {SessionId}", sessionId);
                return false;
            }
        }
        
        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            
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
            var transaction = new RedisStateTransaction(this, _database, _logger);
            return Task.FromResult<IStateTransaction>(transaction);
        }
        
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Redis 연결 및 기본 명령 테스트
                var pingResult = await _database.PingAsync();
                return pingResult != TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 건강 상태 확인 실패");
                return false;
            }
        }
        
        public Task<StateProviderStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var statistics = new StateProviderStatistics
            {
                TotalStates = 0, // Redis에서 직접 계산하기 어려움
                ActiveSessions = 0, // Redis에서 직접 계산하기 어려움
                UsedMemoryBytes = 0, // Redis INFO 명령으로 가져올 수 있지만 단순화
                HitRate = _totalReads > 0 ? (double)_hitCount / _totalReads : 0,
                TotalReads = _totalReads,
                TotalWrites = _totalWrites,
                AverageResponseTimeMs = 10.0, // Redis는 일반적으로 빠름
                LastCleanupTime = null,
                CollectedAt = DateTime.UtcNow
            };
            
            return Task.FromResult(statistics);
        }
        
        public async Task<int> CleanupExpiredStatesAsync(CancellationToken cancellationToken = default)
        {
            // Redis는 자동으로 TTL 기반 만료를 처리하므로 실제로는 정리할 필요 없음
            // 통계를 위해 0을 반환
            _logger.LogDebug("Redis 자동 만료 처리로 인해 수동 정리 불필요");
            return await Task.FromResult(0);
        }
        
        private string GetRedisKey(string sessionId)
        {
            return $"{_options.KeyPrefix}{sessionId}";
        }
        
        public void Dispose()
        {
            // ConnectionMultiplexer는 DI 컨테이너에서 관리되므로 여기서 dispose하지 않음
            _logger.LogInformation("RedisStateProvider 정리 완료");
        }
    }
    
    /// <summary>
    /// Redis 상태 저장소 옵션
    /// </summary>
    public class RedisStateOptions
    {
        public int Database { get; set; } = 0;
        public string KeyPrefix { get; set; } = "state:";
        public TimeSpan? DefaultExpiry { get; set; } = TimeSpan.FromHours(24);
    }
    
    /// <summary>
    /// Redis 기반 상태 트랜잭션 (단순 구현)
    /// </summary>
    internal class RedisStateTransaction : IStateTransaction
    {
        private readonly RedisStateProvider _provider;
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private readonly Dictionary<string, object?> _transactionData;
        private TransactionState _state;
        
        public string TransactionId { get; }
        public DateTime StartTime { get; }
        public TransactionState State => _state;
        public IReadOnlySet<string> SessionIds => _transactionData.Keys.ToHashSet();
        
        public RedisStateTransaction(RedisStateProvider provider, IDatabase database, ILogger logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transactionData = new Dictionary<string, object?>();
            
            TransactionId = Guid.NewGuid().ToString();
            StartTime = DateTime.UtcNow;
            _state = TransactionState.Active;
        }
        
        public Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class
        {
            if (_state != TransactionState.Active)
                throw new StateTransactionException("트랜잭션이 활성 상태가 아닙니다");
                
            return _provider.GetAsync<T>(sessionId, cancellationToken);
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
                
            _transactionData[sessionId] = null;
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
                    if (kvp.Value == null)
                    {
                        await _provider.DeleteAsync(kvp.Key, cancellationToken);
                    }
                    else
                    {
                        // Type safety를 위한 reflection 사용
                        var method = typeof(RedisStateProvider).GetMethod(nameof(RedisStateProvider.SetAsync));
                        var genericMethod = method!.MakeGenericMethod(kvp.Value.GetType());
                        var task = (Task)genericMethod.Invoke(_provider, [kvp.Key, kvp.Value, null, cancellationToken])!;
                        await task;
                    }
                }
                
                _state = TransactionState.Committed;
                _logger.LogDebug("Redis 트랜잭션 커밋 완료: {TransactionId}", TransactionId);
            }
            catch (Exception ex)
            {
                _state = TransactionState.Failed;
                _logger.LogError(ex, "Redis 트랜잭션 커밋 실패: {TransactionId}", TransactionId);
                throw;
            }
        }
        
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            _state = TransactionState.RolledBack;
            _transactionData.Clear();
            _logger.LogDebug("Redis 트랜잭션 롤백 완료: {TransactionId}", TransactionId);
            return Task.CompletedTask;
        }
        
        public Task CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("단순 Redis 트랜잭션은 세이브포인트를 지원하지 않습니다");
        }
        
        public Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("단순 Redis 트랜잭션은 세이브포인트를 지원하지 않습니다");
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