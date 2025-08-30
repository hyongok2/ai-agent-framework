using AIAgentFramework.Monitoring.HealthChecks;
using AIAgentFramework.Monitoring.Interfaces;
using AIAgentFramework.Monitoring.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AIAgentFramework.Monitoring.Extensions;

/// <summary>
/// 모니터링 서비스 등록 확장
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Health Check 시스템 추가
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configureOptions">옵션 구성</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        Action<HealthCheckServiceOptions>? configureOptions = null)
    {
        var options = new HealthCheckServiceOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IHealthCheckService, HealthCheckService>();

        return services;
    }

    /// <summary>
    /// 오케스트레이션 Health Check 추가
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddOrchestrationHealthCheck(this IServiceCollection services)
    {
        services.AddTransient<OrchestrationHealthCheck>();
        return services;
    }

    /// <summary>
    /// LLM Health Check 추가
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddLLMHealthCheck(this IServiceCollection services)
    {
        services.AddTransient<LLMHealthCheck>();
        return services;
    }

    /// <summary>
    /// State Health Check 추가
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddStateHealthCheck(this IServiceCollection services)
    {
        services.AddTransient<StateHealthCheck>();
        return services;
    }

    /// <summary>
    /// 모든 기본 Health Check 추가
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configureOptions">옵션 구성</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddAllHealthChecks(
        this IServiceCollection services,
        Action<HealthCheckServiceOptions>? configureOptions = null)
    {
        services.AddHealthChecks(configureOptions);
        services.AddOrchestrationHealthCheck();
        services.AddLLMHealthCheck();
        services.AddStateHealthCheck();

        return services;
    }

    /// <summary>
    /// Health Check를 서비스에 등록
    /// </summary>
    /// <param name="serviceProvider">서비스 프로바이더</param>
    public static void RegisterHealthChecks(this IServiceProvider serviceProvider)
    {
        var healthCheckService = serviceProvider.GetRequiredService<IHealthCheckService>();

        // OrchestrationHealthCheck 등록 시도
        try
        {
            var orchestrationHealthCheck = serviceProvider.GetService<OrchestrationHealthCheck>();
            if (orchestrationHealthCheck != null)
            {
                healthCheckService.RegisterHealthCheck(orchestrationHealthCheck);
            }
        }
        catch (Exception)
        {
            // 의존성이 없으면 무시
        }

        // LLMHealthCheck 등록 시도
        try
        {
            var llmHealthCheck = serviceProvider.GetService<LLMHealthCheck>();
            if (llmHealthCheck != null)
            {
                healthCheckService.RegisterHealthCheck(llmHealthCheck);
            }
        }
        catch (Exception)
        {
            // 의존성이 없으면 무시
        }

        // StateHealthCheck 등록 시도
        try
        {
            var stateHealthCheck = serviceProvider.GetService<StateHealthCheck>();
            if (stateHealthCheck != null)
            {
                healthCheckService.RegisterHealthCheck(stateHealthCheck);
            }
        }
        catch (Exception)
        {
            // 의존성이 없으면 무시
        }
    }
}