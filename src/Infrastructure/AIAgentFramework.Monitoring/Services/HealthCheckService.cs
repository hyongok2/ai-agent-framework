using AIAgentFramework.Monitoring.Interfaces;
using AIAgentFramework.Monitoring.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.Services;

/// <summary>
/// Health Check 통합 서비스
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks;
    private readonly ConcurrentDictionary<string, HealthCheckResult> _lastResults;
    private readonly Timer? _backgroundTimer;
    private readonly HealthCheckServiceOptions _options;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    /// <param name="options">서비스 옵션</param>
    public HealthCheckService(ILogger<HealthCheckService> logger, HealthCheckServiceOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new HealthCheckServiceOptions();
        _healthChecks = new ConcurrentDictionary<string, IHealthCheck>();
        _lastResults = new ConcurrentDictionary<string, HealthCheckResult>();
        
        // 백그라운드 Health Check 타이머 (선택적)
        if (_options.EnableBackgroundChecks)
        {
            _backgroundTimer = new Timer(RunBackgroundChecks, null, 
                TimeSpan.FromSeconds(_options.BackgroundCheckIntervalSeconds),
                TimeSpan.FromSeconds(_options.BackgroundCheckIntervalSeconds));
        }
    }

    /// <inheritdoc />
    public void RegisterHealthCheck(IHealthCheck healthCheck)
    {
        if (healthCheck == null) throw new ArgumentNullException(nameof(healthCheck));
        
        _healthChecks.TryAdd(healthCheck.Name, healthCheck);
        _logger.LogInformation("Health check registered: {HealthCheckName}", healthCheck.Name);
    }

    /// <inheritdoc />
    public void UnregisterHealthCheck(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));
        
        _healthChecks.TryRemove(name, out _);
        _lastResults.TryRemove(name, out _);
        _logger.LogInformation("Health check unregistered: {HealthCheckName}", name);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> RunHealthCheckAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_healthChecks.TryGetValue(name, out var healthCheck))
        {
            return HealthCheckResult.Unhealthy(name, "Health check not found");
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(healthCheck.TimeoutSeconds));
            
            var result = await healthCheck.CheckAsync(cts.Token);
            _lastResults.TryAdd(name, result);
            _lastResults[name] = result;
            
            _logger.LogDebug("Health check completed: {HealthCheckName} - {Status} in {ResponseTime}ms",
                name, result.Status, result.ResponseTimeMs);
                
            return result;
        }
        catch (OperationCanceledException)
        {
            var result = HealthCheckResult.Unhealthy(name, "Health check timed out");
            _lastResults[name] = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed: {HealthCheckName}", name);
            var result = HealthCheckResult.Unhealthy(name, "Health check failed with exception", ex.Message);
            _lastResults[name] = result;
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<HealthCheckSummary> RunAllHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        var summary = new HealthCheckSummary();
        var stopwatch = Stopwatch.StartNew();

        var tasks = _healthChecks.Values.Select(async healthCheck =>
        {
            try
            {
                var result = await RunHealthCheckAsync(healthCheck.Name, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run health check: {HealthCheckName}", healthCheck.Name);
                return HealthCheckResult.Unhealthy(healthCheck.Name, "Health check execution failed", ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        summary.Results = results.ToList();
        summary.TotalDurationMs = stopwatch.ElapsedMilliseconds;
        summary.CheckedAt = DateTime.UtcNow;
        
        // 전체 상태 계산
        summary.OverallStatus = CalculateOverallStatus(results);
        summary.HealthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
        summary.WarningCount = results.Count(r => r.Status == HealthStatus.Warning);
        summary.UnhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
        summary.TotalCount = results.Length;

        _logger.LogInformation("All health checks completed: Overall={OverallStatus}, " +
                              "Healthy={Healthy}, Warning={Warning}, Unhealthy={Unhealthy}, Duration={Duration}ms",
            summary.OverallStatus, summary.HealthyCount, summary.WarningCount, 
            summary.UnhealthyCount, summary.TotalDurationMs);

        return summary;
    }

    /// <inheritdoc />
    public HealthCheckResult? GetLastResult(string name)
    {
        _lastResults.TryGetValue(name, out var result);
        return result;
    }

    /// <inheritdoc />
    public Dictionary<string, HealthCheckResult> GetAllLastResults()
    {
        return _lastResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <inheritdoc />
    public List<string> GetRegisteredHealthCheckNames()
    {
        return _healthChecks.Keys.ToList();
    }

    /// <inheritdoc />
    public HealthCheckServiceStatus GetServiceStatus()
    {
        var lastResults = GetAllLastResults();
        var now = DateTime.UtcNow;

        return new HealthCheckServiceStatus
        {
            RegisteredHealthChecks = _healthChecks.Keys.ToList(),
            TotalRegistered = _healthChecks.Count,
            BackgroundChecksEnabled = _options.EnableBackgroundChecks,
            BackgroundCheckInterval = TimeSpan.FromSeconds(_options.BackgroundCheckIntervalSeconds),
            RecentResults = lastResults.Values
                .Where(r => now - r.CheckedAt < TimeSpan.FromMinutes(10))
                .ToList(),
            ServiceUptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
        };
    }

    /// <summary>
    /// 전체 상태 계산
    /// </summary>
    private HealthStatus CalculateOverallStatus(HealthCheckResult[] results)
    {
        if (results.Length == 0) return HealthStatus.Unknown;
        
        if (results.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;
            
        if (results.Any(r => r.Status == HealthStatus.Warning))
            return HealthStatus.Warning;
            
        if (results.All(r => r.Status == HealthStatus.Healthy))
            return HealthStatus.Healthy;
            
        return HealthStatus.Unknown;
    }

    /// <summary>
    /// 백그라운드 Health Check 실행
    /// </summary>
    private async void RunBackgroundChecks(object? state)
    {
        if (!_options.EnableBackgroundChecks) return;

        try
        {
            _logger.LogDebug("Running background health checks");
            var summary = await RunAllHealthChecksAsync(CancellationToken.None);
            
            // 중요한 상태 변화 시 로깅
            if (summary.UnhealthyCount > 0)
            {
                _logger.LogWarning("Background health check found {UnhealthyCount} unhealthy components", 
                    summary.UnhealthyCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background health check execution failed");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _backgroundTimer?.Dispose();
    }
}

/// <summary>
/// Health Check Service 인터페이스
/// </summary>
public interface IHealthCheckService : IDisposable
{
    /// <summary>
    /// Health Check 등록
    /// </summary>
    void RegisterHealthCheck(IHealthCheck healthCheck);
    
    /// <summary>
    /// Health Check 등록 해제
    /// </summary>
    void UnregisterHealthCheck(string name);
    
    /// <summary>
    /// 특정 Health Check 실행
    /// </summary>
    Task<HealthCheckResult> RunHealthCheckAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모든 Health Check 실행
    /// </summary>
    Task<HealthCheckSummary> RunAllHealthChecksAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 마지막 결과 조회
    /// </summary>
    HealthCheckResult? GetLastResult(string name);
    
    /// <summary>
    /// 모든 마지막 결과 조회
    /// </summary>
    Dictionary<string, HealthCheckResult> GetAllLastResults();
    
    /// <summary>
    /// 등록된 Health Check 이름 목록
    /// </summary>
    List<string> GetRegisteredHealthCheckNames();
    
    /// <summary>
    /// 서비스 상태 조회
    /// </summary>
    HealthCheckServiceStatus GetServiceStatus();
}

/// <summary>
/// Health Check Service 옵션
/// </summary>
public class HealthCheckServiceOptions
{
    /// <summary>
    /// 백그라운드 체크 활성화 여부
    /// </summary>
    public bool EnableBackgroundChecks { get; set; } = true;
    
    /// <summary>
    /// 백그라운드 체크 간격 (초)
    /// </summary>
    public int BackgroundCheckIntervalSeconds { get; set; } = 300; // 5분
    
    /// <summary>
    /// 기본 타임아웃 (초)
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Health Check 요약
/// </summary>
public class HealthCheckSummary
{
    /// <summary>
    /// 전체 상태
    /// </summary>
    public HealthStatus OverallStatus { get; set; } = HealthStatus.Unknown;
    
    /// <summary>
    /// 결과 목록
    /// </summary>
    public List<HealthCheckResult> Results { get; set; } = new();
    
    /// <summary>
    /// 전체 소요 시간 (밀리초)
    /// </summary>
    public long TotalDurationMs { get; set; }
    
    /// <summary>
    /// 정상 상태 개수
    /// </summary>
    public int HealthyCount { get; set; }
    
    /// <summary>
    /// 경고 상태 개수
    /// </summary>
    public int WarningCount { get; set; }
    
    /// <summary>
    /// 비정상 상태 개수
    /// </summary>
    public int UnhealthyCount { get; set; }
    
    /// <summary>
    /// 전체 개수
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 검사 시간
    /// </summary>
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Health Check Service 상태
/// </summary>
public class HealthCheckServiceStatus
{
    /// <summary>
    /// 등록된 Health Check 목록
    /// </summary>
    public List<string> RegisteredHealthChecks { get; set; } = new();
    
    /// <summary>
    /// 총 등록 개수
    /// </summary>
    public int TotalRegistered { get; set; }
    
    /// <summary>
    /// 백그라운드 체크 활성화 여부
    /// </summary>
    public bool BackgroundChecksEnabled { get; set; }
    
    /// <summary>
    /// 백그라운드 체크 간격
    /// </summary>
    public TimeSpan BackgroundCheckInterval { get; set; }
    
    /// <summary>
    /// 최근 결과들
    /// </summary>
    public List<HealthCheckResult> RecentResults { get; set; } = new();
    
    /// <summary>
    /// 서비스 가동 시간
    /// </summary>
    public TimeSpan ServiceUptime { get; set; }
}