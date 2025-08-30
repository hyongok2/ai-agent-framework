using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Tools.BuiltIn;
using AIAgentFramework.Tools.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace AIAgentFramework.Tools.Extensions;

/// <summary>
/// Tools 서비스 등록을 위한 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Built-In Tools를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddBuiltInTools(this IServiceCollection services)
    {
        // 메모리 캐시 등록 (아직 등록되지 않은 경우)
        services.AddMemoryCache();

        // Built-In Tools 등록
        services.AddScoped<ITool, EmbeddingCacheTool>();
        services.AddScoped<ITool, VectorDBTool>();

        // 개별 도구 등록 (특정 도구가 필요한 경우)
        services.AddScoped<EmbeddingCacheTool>();
        services.AddScoped<VectorDBTool>();

        return services;
    }

    /// <summary>
    /// 특정 Built-In Tool만 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="toolTypes">등록할 도구 타입들</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddBuiltInTools(this IServiceCollection services, params Type[] toolTypes)
    {
        services.AddMemoryCache();

        foreach (var toolType in toolTypes)
        {
            if (typeof(ITool).IsAssignableFrom(toolType))
            {
                services.AddScoped(typeof(ITool), toolType);
                services.AddScoped(toolType);
            }
        }

        return services;
    }

    /// <summary>
    /// EmbeddingCacheTool만 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddEmbeddingCacheTool(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ITool, EmbeddingCacheTool>();
        services.AddScoped<EmbeddingCacheTool>();
        return services;
    }

    /// <summary>
    /// VectorDBTool을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddVectorDBTool(this IServiceCollection services)
    {
        // VectorDB 구현체가 등록되어 있어야 함
        services.AddScoped<ITool, VectorDBTool>();
        services.AddScoped<VectorDBTool>();
        return services;
    }

    /// <summary>
    /// VectorDBTool을 특정 구현체와 함께 등록합니다.
    /// </summary>
    /// <typeparam name="TVectorDB">벡터 데이터베이스 구현체</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddVectorDBTool<TVectorDB>(this IServiceCollection services) 
        where TVectorDB : class, IVectorDatabase
    {
        services.AddScoped<IVectorDatabase, TVectorDB>();
        services.AddScoped<ITool, VectorDBTool>();
        services.AddScoped<VectorDBTool>();
        return services;
    }

    /// <summary>
    /// Plugin Tools 시스템을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddPluginTools(this IServiceCollection services)
    {
        // Plugin 시스템 등록
        services.AddSingleton<IPluginLoader, PluginLoader>();
        services.AddSingleton<IPluginManager, PluginManager>();
        
        return services;
    }

    /// <summary>
    /// 모든 도구 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddAllTools(this IServiceCollection services)
    {
        // Built-In Tools
        services.AddBuiltInTools();

        // Plugin Tools
        services.AddPluginTools();

        // 향후 MCP Tools 등도 여기에 추가

        return services;
    }
}