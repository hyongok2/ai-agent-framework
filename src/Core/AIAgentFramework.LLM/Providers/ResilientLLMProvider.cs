
using AIAgentFramework.Core.LLM.Abstractions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// 복원력 있는 LLM Provider - 여러 Provider 간 자동 Failover 지원
/// Circuit Breaker 패턴과 자동 Provider 선택 로직 포함
/// </summary>
public class ResilientLLMProvider : ILLMProvider
{
    private readonly List<ILLMProvider> _providers;
    private readonly ILogger<ResilientLLMProvider> _logger;
    private readonly Dictionary<string, CircuitBreakerState> _circuitBreakers;
    private readonly TimeSpan _circuitBreakerTimeout;
    private readonly int _maxFailureCount;

    /// <summary>
    /// Circuit Breaker 상태
    /// </summary>
    private class CircuitBreakerState
    {
        public bool IsOpen { get; set; }
        public int FailureCount { get; set; }
        public DateTime LastFailureTime { get; set; }
        public DateTime? LastSuccessTime { get; set; }
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="providers">사용할 Provider 목록</param>
    /// <param name="logger">로거</param>
    /// <param name="circuitBreakerTimeout">Circuit Breaker 타임아웃 (기본: 1분)</param>
    /// <param name="maxFailureCount">최대 실패 횟수 (기본: 3회)</param>
    public ResilientLLMProvider(
        IEnumerable<ILLMProvider> providers, 
        ILogger<ResilientLLMProvider> logger,
        TimeSpan? circuitBreakerTimeout = null,
        int maxFailureCount = 3)
    {
        _providers = providers.ToList();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _circuitBreakerTimeout = circuitBreakerTimeout ?? TimeSpan.FromMinutes(1);
        _maxFailureCount = maxFailureCount;
        _circuitBreakers = new Dictionary<string, CircuitBreakerState>();

        if (!_providers.Any())
            throw new ArgumentException("최소 하나의 Provider가 필요합니다.", nameof(providers));

        // 각 Provider에 대한 Circuit Breaker 초기화
        foreach (var provider in _providers)
        {
            _circuitBreakers[provider.Name] = new CircuitBreakerState
            {
                IsOpen = false,
                FailureCount = 0,
                LastFailureTime = DateTime.MinValue
            };
        }

        _logger.LogInformation("ResilientLLMProvider 초기화 완료. Provider 수: {ProviderCount}", _providers.Count);
    }

    /// <inheritdoc />
    public string Name => $"Resilient[{string.Join(",", _providers.Select(p => p.Name))}]";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedModels => 
        _providers.SelectMany(p => p.SupportedModels).Distinct().ToList();

    /// <inheritdoc />
    public string DefaultModel => GetAvailableProvider()?.DefaultModel ?? _providers.First().DefaultModel;

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GenerateAsync(prompt, DefaultModel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string prompt, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var availableProviders = GetAvailableProviders(model);
        if (!availableProviders.Any())
        {
            var allProviderNames = string.Join(", ", _providers.Select(p => p.Name));
            throw new InvalidOperationException($"모든 Provider가 사용 불가능합니다: {allProviderNames}");
        }

        Exception? lastException = null;
        
        foreach (var provider in availableProviders)
        {
            try
            {
                _logger.LogDebug("Provider {ProviderName}으로 텍스트 생성 시도: {Model}", provider.Name, model);
                
                var result = await provider.GenerateAsync(prompt, model, cancellationToken);
                
                // 성공 시 Circuit Breaker 리셋
                ResetCircuitBreaker(provider.Name);
                
                _logger.LogDebug("Provider {ProviderName}으로 텍스트 생성 성공", provider.Name);
                return result;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {ProviderName}에서 오류 발생, 다음 Provider로 전환", provider.Name);
                
                // Circuit Breaker 업데이트
                UpdateCircuitBreaker(provider.Name);
            }
        }

        // 모든 Provider가 실패한 경우
        var failedProviderNames = string.Join(", ", availableProviders.Select(p => p.Name));
        _logger.LogError("모든 사용 가능한 Provider가 실패했습니다: {ProviderNames}", failedProviderNames);
        
        throw new InvalidOperationException(
            $"모든 사용 가능한 Provider가 실패했습니다: {failedProviderNames}", 
            lastException);
    }

    /// <inheritdoc />
    public async Task<T?> GenerateStructuredAsync<T>(string prompt, CancellationToken cancellationToken = default) 
        where T : class
    {
        return await GenerateStructuredAsync<T>(prompt, DefaultModel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> GenerateStructuredAsync<T>(string prompt, string model, CancellationToken cancellationToken = default) 
        where T : class
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var availableProviders = GetAvailableProviders(model);
        if (!availableProviders.Any())
        {
            var allProviderNames = string.Join(", ", _providers.Select(p => p.Name));
            throw new InvalidOperationException($"모든 Provider가 사용 불가능합니다: {allProviderNames}");
        }

        Exception? lastException = null;
        
        foreach (var provider in availableProviders)
        {
            try
            {
                _logger.LogDebug("Provider {ProviderName}으로 구조화된 응답 생성 시도: {Model}", provider.Name, model);
                
                var result = await provider.GenerateStructuredAsync<T>(prompt, model, cancellationToken);
                
                // 성공 시 Circuit Breaker 리셋
                ResetCircuitBreaker(provider.Name);
                
                _logger.LogDebug("Provider {ProviderName}으로 구조화된 응답 생성 성공", provider.Name);
                return result;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {ProviderName}에서 구조화된 응답 생성 오류, 다음 Provider로 전환", provider.Name);
                
                // Circuit Breaker 업데이트
                UpdateCircuitBreaker(provider.Name);
            }
        }

        // 모든 Provider가 실패한 경우
        var failedProviderNames = string.Join(", ", availableProviders.Select(p => p.Name));
        _logger.LogError("모든 사용 가능한 Provider의 구조화된 응답 생성이 실패했습니다: {ProviderNames}", failedProviderNames);
        
        throw new InvalidOperationException(
            $"모든 사용 가능한 Provider의 구조화된 응답 생성이 실패했습니다: {failedProviderNames}", 
            lastException);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var availableProviders = GetAvailableProviders();
        if (!availableProviders.Any())
        {
            var allProviderNames = string.Join(", ", _providers.Select(p => p.Name));
            throw new InvalidOperationException($"모든 Provider가 사용 불가능합니다: {allProviderNames}");
        }

        Exception? lastException = null;
        
        foreach (var provider in availableProviders)
        {
            var chunks = new List<string>();
            bool hasContent = false;
            bool success = false;

            try
            {
                _logger.LogDebug("Provider {ProviderName}으로 스트리밍 시작", provider.Name);
                
                await foreach (var chunk in provider.GenerateStreamAsync(prompt, cancellationToken))
                {
                    hasContent = true;
                    chunks.Add(chunk);
                }

                if (hasContent)
                {
                    // 성공 시 Circuit Breaker 리셋
                    ResetCircuitBreaker(provider.Name);
                    _logger.LogDebug("Provider {ProviderName} 스트리밍 완료", provider.Name);
                    success = true;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {ProviderName} 스트리밍 실패, 다음 Provider로 전환", provider.Name);
                
                // Circuit Breaker 업데이트
                UpdateCircuitBreaker(provider.Name);
            }

            // 수집된 청크들을 반환
            if (success && hasContent)
            {
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
                yield break; // 성공적으로 완료되면 다른 Provider 시도하지 않음
            }
        }

        // 모든 Provider가 실패한 경우
        var failedProviderNames = string.Join(", ", availableProviders.Select(p => p.Name));
        throw new InvalidOperationException(
            $"모든 사용 가능한 Provider 스트리밍이 실패했습니다: {failedProviderNames}", 
            lastException);
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        var results = await Task.WhenAll(_providers.Select(p => CheckProviderAvailability(p)));
        return results.Any(available => available);
    }

    /// <inheritdoc />
    public async Task<int> CountTokensAsync(string text, string? model = null)
    {
        var provider = GetAvailableProvider();
        if (provider == null)
        {
            throw new InvalidOperationException("사용 가능한 Provider가 없습니다.");
        }

        try
        {
            return await provider.CountTokensAsync(text, model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {ProviderName}의 토큰 카운팅 실패", provider.Name);
            
            // 다른 Provider로 재시도
            var otherProviders = GetAvailableProviders().Where(p => p.Name != provider.Name);
            foreach (var otherProvider in otherProviders)
            {
                try
                {
                    return await otherProvider.CountTokensAsync(text, model);
                }
                catch
                {
                    // 다음 Provider 시도
                    continue;
                }
            }

            // 모든 Provider 실패 시 기본 추정
            _logger.LogWarning("모든 Provider의 토큰 카운팅 실패, 기본 추정 사용");
            return EstimateTokenCount(text);
        }
    }

    /// <summary>
    /// 사용 가능한 Provider 목록 반환
    /// </summary>
    private List<ILLMProvider> GetAvailableProviders(string? model = null)
    {
        var available = new List<ILLMProvider>();
        
        foreach (var provider in _providers)
        {
            // 모델 지원 확인
            if (model != null && !provider.SupportedModels.Contains(model))
                continue;
                
            // Circuit Breaker 상태 확인
            var breaker = _circuitBreakers[provider.Name];
            if (breaker.IsOpen)
            {
                // 타임아웃 경과 시 Half-Open 상태로 전환
                if (DateTime.UtcNow - breaker.LastFailureTime > _circuitBreakerTimeout)
                {
                    breaker.IsOpen = false;
                    _logger.LogInformation("Circuit Breaker Half-Open: {ProviderName}", provider.Name);
                }
                else
                {
                    _logger.LogDebug("Circuit Breaker Open: {ProviderName}", provider.Name);
                    continue;
                }
            }
            
            available.Add(provider);
        }
        
        return available;
    }

    /// <summary>
    /// 첫 번째 사용 가능한 Provider 반환
    /// </summary>
    private ILLMProvider? GetAvailableProvider()
    {
        return GetAvailableProviders().FirstOrDefault();
    }

    /// <summary>
    /// Provider 가용성 확인
    /// </summary>
    private async Task<bool> CheckProviderAvailability(ILLMProvider provider)
    {
        try
        {
            var isAvailable = await provider.IsAvailableAsync();
            
            if (isAvailable)
            {
                ResetCircuitBreaker(provider.Name);
            }
            
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {ProviderName} 가용성 확인 실패", provider.Name);
            UpdateCircuitBreaker(provider.Name);
            return false;
        }
    }

    /// <summary>
    /// Circuit Breaker 업데이트 (실패 시)
    /// </summary>
    private void UpdateCircuitBreaker(string providerName)
    {
        var breaker = _circuitBreakers[providerName];
        breaker.FailureCount++;
        breaker.LastFailureTime = DateTime.UtcNow;
        
        if (breaker.FailureCount >= _maxFailureCount)
        {
            breaker.IsOpen = true;
            _logger.LogWarning("Circuit Breaker 열림: {ProviderName} (실패 횟수: {FailureCount})", 
                providerName, breaker.FailureCount);
        }
    }

    /// <summary>
    /// Circuit Breaker 리셋 (성공 시)
    /// </summary>
    private void ResetCircuitBreaker(string providerName)
    {
        var breaker = _circuitBreakers[providerName];
        var wasOpen = breaker.IsOpen;
        
        breaker.IsOpen = false;
        breaker.FailureCount = 0;
        breaker.LastSuccessTime = DateTime.UtcNow;
        
        if (wasOpen)
        {
            _logger.LogInformation("Circuit Breaker 리셋: {ProviderName}", providerName);
        }
    }

    /// <summary>
    /// 기본 토큰 수 추정
    /// </summary>
    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
            
        // 대략적인 추정: 영어는 4글자당 1토큰, 한글은 2글자당 1토큰
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return (int)Math.Ceiling(words.Length * 1.3); // 1.3배 여유분
    }
}