
using AIAgentFramework.Core.Common.Registry;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Registry.Extensions;
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
        // 기존 오케스트레이션 엔진
        services.AddSingleton<IOrchestrationEngine, OrchestrationEngine>();
        
        // 타입 안전한 오케스트레이션 엔진 추가
        services.AddScoped<TypeSafeOrchestrationEngine>();
        
        // Registry 시스템 등록 (타입 안전한 Registry 포함)
        services.AddRegistry();
        
        // 의존성 추가
        services.AddSingleton<IRegistry, Registry.Registry>();
        services.AddSingleton<ILLMProviderFactory, LLM.Factories.LLMProviderFactory>();
        services.AddHttpClient();
        
        return services;
    }
}