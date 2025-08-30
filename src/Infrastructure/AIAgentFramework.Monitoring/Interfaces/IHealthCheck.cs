using AIAgentFramework.Monitoring.Models;

namespace AIAgentFramework.Monitoring.Interfaces;

/// <summary>
/// Health Check 인터페이스
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Health Check 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Health Check 설명
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 타임아웃 (초)
    /// </summary>
    int TimeoutSeconds { get; }
    
    /// <summary>
    /// Health Check 실행
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>Health Check 결과</returns>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 비동기 Health Check 인터페이스
/// </summary>
public interface IAsyncHealthCheck : IHealthCheck
{
    /// <summary>
    /// 백그라운드에서 주기적으로 실행될 Health Check
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>연속 Health Check 결과</returns>
    IAsyncEnumerable<HealthCheckResult> CheckContinuouslyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 구성 가능한 Health Check 인터페이스
/// </summary>
public interface IConfigurableHealthCheck : IHealthCheck
{
    /// <summary>
    /// Health Check 구성 업데이트
    /// </summary>
    /// <param name="configuration">새로운 구성</param>
    void UpdateConfiguration(HealthCheckConfiguration configuration);
    
    /// <summary>
    /// 현재 구성 조회
    /// </summary>
    /// <returns>현재 Health Check 구성</returns>
    HealthCheckConfiguration GetConfiguration();
}