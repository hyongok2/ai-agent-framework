using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.State.Providers
{
    /// <summary>
    /// 메모리 기반 상태 저장소 (개발/테스트용)
    /// </summary>
    public class InMemoryStateProvider : IStateProvider
    {
        private readonly ILogger<InMemoryStateProvider> _logger;
        private readonly ConcurrentDictionary<string, StateEntry> _states;
        private readonly Timer _cleanupTimer;
        
        // 통계 추적
        private long _totalReads;
        private long _totalWrites;
        private long _hitCount;
        private long _missCount;
        
        public InMemoryStateProvider(ILogger<InMemoryStateProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _states = new ConcurrentDictionary<string, StateEntry>();
            
            // 5분마다 만료된 상태 정리
            _cleanupTimer = new Timer(CleanupExpiredStates, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
                
            _logger.LogInformation("InMemoryStateProvider 초기화 완료");
        }
        
        public Task<T?> GetAsync<T>(string sessionId, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            
            Interlocked.Increment(ref _totalReads);
            
            if (_states.TryGetValue(sessionId, out var entry))
            {
                if (entry.IsExpired)
                {
                    _states.TryRemove(sessionId, out _);
                    Interlocked.Increment(ref _missCount);
                    return Task.FromResult<T?>(null);
                }
                
                try
                {
                    var state = JsonSerializer.Deserialize<T>(entry.Data);
                    Interlocked.Increment(ref _hitCount);
                    return Task.FromResult(state);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "상태 역직렬화 실패: {SessionId}", sessionId);
                    throw new StateSerializationException("상태 역직렬화 실패", ex) { StateType = typeof(T) };
                }
            }
            
            Interlocked.Increment(ref _missCount);
            return Task.FromResult<T?>(null);
        }
        
        public Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentNullException.ThrowIfNull(state);
            
            Interlocked.Increment(ref _totalWrites);
            
            try
            {
                var json = JsonSerializer.Serialize(state);
                var entry = new StateEntry
                {
                    Data = json,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : null,
                    StateType = typeof(T)
                };
                
                _states.AddOrUpdate(sessionId, entry, (key, oldValue) => entry);
                _logger.LogDebug("상태 저장 완료: {SessionId}, 타입: {Type}", sessionId, typeof(T).Name);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "상태 직렬화 실패: {SessionId}", sessionId);
                throw new StateSerializationException("상태 직렬화 실패", ex) { StateType = typeof(T) };
            }
            
            return Task.CompletedTask;
        }
        
        public Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            
            if (_states.TryGetValue(sessionId, out var entry))
            {
                if (entry.IsExpired)
                {
                    _states.TryRemove(sessionId, out _);
                    return Task.FromResult(false);
                }
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        
        public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            
            _states.TryRemove(sessionId, out _);
            _logger.LogDebug("상태 삭제 완료: {SessionId}", sessionId);
            return Task.CompletedTask;
        }
        
        public Task<IStateTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = new InMemoryStateTransaction(this, _logger);
            return Task.FromResult<IStateTransaction>(transaction);
        }
        
        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
        
        public Task<StateProviderStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var statistics = new StateProviderStatistics
            {
                TotalStates = _states.Count,
                ActiveSessions = _states.Count,
                UsedMemoryBytes = EstimateMemoryUsage(),
                HitRate = _totalReads > 0 ? (double)_hitCount / _totalReads : 0,
                TotalReads = _totalReads,
                TotalWrites = _totalWrites,
                AverageResponseTimeMs = 1.0, // 메모리 기반이므로 매우 빠름
                LastCleanupTime = null,
                CollectedAt = DateTime.UtcNow
            };
            
            return Task.FromResult(statistics);
        }
        
        public Task<int> CleanupExpiredStatesAsync(CancellationToken cancellationToken = default)
        {
            var expiredKeys = _states
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();
                
            var removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_states.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }
            
            if (removedCount > 0)
            {
                _logger.LogDebug("만료된 상태 정리 완료: {Count}개", removedCount);
            }
            
            return Task.FromResult(removedCount);
        }
        
        private long EstimateMemoryUsage()
        {
            return _states.Sum(kvp => kvp.Key.Length * sizeof(char) + kvp.Value.Data.Length * sizeof(char));
        }
        
        private void CleanupExpiredStates(object? state)
        {
            try
            {
                CleanupExpiredStatesAsync(CancellationToken.None).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "자동 정리 중 오류 발생");
            }
        }
        
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _states.Clear();
            _logger.LogInformation("InMemoryStateProvider 정리 완료");
        }
    }
    
    /// <summary>
    /// 메모리 상태 항목
    /// </summary>
    internal class StateEntry
    {
        public required string Data { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public Type? StateType { get; init; }
        
        public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
    }
    
    /// <summary>
    /// 메모리 기반 상태 트랜잭션
    /// </summary>
    internal class InMemoryStateTransaction : IStateTransaction
    {
        private readonly InMemoryStateProvider _provider;
        private readonly ILogger _logger;
        private readonly Dictionary<string, object?> _transactionData;
        private TransactionState _state;
        
        public string TransactionId { get; }
        public DateTime StartTime { get; }
        public TransactionState State => _state;
        public IReadOnlySet<string> SessionIds => _transactionData.Keys.ToHashSet();
        
        public InMemoryStateTransaction(InMemoryStateProvider provider, ILogger logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
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
                        // Type safety check
                        if (kvp.Value is not null)
                        {
                            var method = typeof(InMemoryStateProvider).GetMethod(nameof(InMemoryStateProvider.SetAsync));
                            var genericMethod = method!.MakeGenericMethod(kvp.Value.GetType());
                            var task = (Task)genericMethod.Invoke(_provider, [kvp.Key, kvp.Value, null, cancellationToken])!;
                            await task;
                        }
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
            throw new NotSupportedException("InMemory 트랜잭션은 세이브포인트를 지원하지 않습니다");
        }
        
        public Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("InMemory 트랜잭션은 세이브포인트를 지원하지 않습니다");
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