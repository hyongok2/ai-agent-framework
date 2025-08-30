using System;
using AIAgentFramework.State.Interfaces;
using AIAgentFramework.State.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AIAgentFramework.State.Extensions
{
    /// <summary>
    /// State Management 시스템 DI 확장 메서드
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// InMemory State Provider를 등록합니다 (개발/테스트용)
        /// </summary>
        public static IServiceCollection AddInMemoryStateProvider(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            
            services.AddSingleton<IStateProvider, InMemoryStateProvider>();
            
            return services;
        }
        
        /// <summary>
        /// Redis State Provider를 등록합니다 (프로덕션용)
        /// </summary>
        public static IServiceCollection AddRedisStateProvider(this IServiceCollection services, string connectionString)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrEmpty(connectionString);
            
            // Redis 연결 설정
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse(connectionString);
                return ConnectionMultiplexer.Connect(configuration);
            });
            
            services.AddSingleton<IStateProvider, RedisStateProvider>();
            
            return services;
        }
        
        /// <summary>
        /// Redis State Provider를 Configuration에서 연결 문자열을 읽어 등록합니다
        /// </summary>
        public static IServiceCollection AddRedisStateProvider(this IServiceCollection services, IConfiguration configuration, string configurationKey = "Redis:ConnectionString")
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            
            var connectionString = configuration.GetConnectionString(configurationKey) 
                                 ?? configuration[configurationKey];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Redis 연결 문자열을 찾을 수 없습니다. 키: {configurationKey}");
            }
            
            return AddRedisStateProvider(services, connectionString);
        }
        
        /// <summary>
        /// State Provider를 조건부로 등록합니다
        /// </summary>
        public static IServiceCollection AddStateProvider(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            
            var stateProviderType = configuration["StateProvider:Type"] ?? "InMemory";
            
            return stateProviderType.ToLowerInvariant() switch
            {
                "redis" => services.AddRedisStateProvider(configuration),
                "inmemory" => services.AddInMemoryStateProvider(),
                _ => throw new NotSupportedException($"지원되지 않는 State Provider 타입: {stateProviderType}")
            };
        }
        
        /// <summary>
        /// State Management 시스템의 건강 상태 확인을 등록합니다
        /// </summary>
        public static IServiceCollection AddStateProviderHealthChecks(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            
            services.AddHealthChecks()
                    .AddCheck<StateProviderHealthCheck>("state_provider");
            
            return services;
        }
    }
    
    /// <summary>
    /// State Provider 건강 상태 확인
    /// </summary>
    public class StateProviderHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly IStateProvider _stateProvider;
        private readonly ILogger<StateProviderHealthCheck> _logger;
        
        public StateProviderHealthCheck(IStateProvider stateProvider, ILogger<StateProviderHealthCheck> logger)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = await _stateProvider.IsHealthyAsync(cancellationToken);
                
                if (isHealthy)
                {
                    var statistics = await _stateProvider.GetStatisticsAsync(cancellationToken);
                    
                    var data = new Dictionary<string, object>
                    {
                        ["total_states"] = statistics.TotalStates,
                        ["active_sessions"] = statistics.ActiveSessions,
                        ["used_memory_bytes"] = statistics.UsedMemoryBytes,
                        ["hit_rate"] = statistics.HitRate,
                        ["total_reads"] = statistics.TotalReads,
                        ["total_writes"] = statistics.TotalWrites,
                        ["average_response_time_ms"] = statistics.AverageResponseTimeMs,
                        ["last_cleanup"] = statistics.LastCleanupTime?.ToString() ?? "Never"
                    };
                    
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                        "State Provider is healthy", data);
                }
                else
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                        "State Provider is not healthy");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "State Provider 건강 상태 확인 중 오류 발생");
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "State Provider health check failed", ex);
            }
        }
    }
}