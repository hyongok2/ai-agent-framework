using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AIAgentFramework.Orchestration;

/// <summary>
/// 오케스트레이션 서비스 확장 메서드
/// </summary>
public static class OrchestrationServiceExtensions
{
    /// <summary>
    /// 오케스트레이션 시스템을 DI 컨테이너에 등록
    /// </summary>
    public static IServiceCollection AddOrchestration(this IServiceCollection services)
    {
        services.AddSingleton<IOrchestrationEngine, OrchestrationEngine>();
        
        return services;
    }
}