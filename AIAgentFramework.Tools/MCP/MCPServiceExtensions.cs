using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Tools.MCP;

/// <summary>
/// MCP 서비스 확장 메서드
/// </summary>
public static class MCPServiceExtensions
{
    /// <summary>
    /// MCP 도구 시스템을 DI 컨테이너에 등록
    /// </summary>
    public static IServiceCollection AddMCPTools(this IServiceCollection services)
    {
        services.AddSingleton<MCPToolLoader>();
        services.AddSingleton<MCPProtocolValidator>();
        
        return services;
    }

    /// <summary>
    /// MCP 도구 로더를 사용하여 설정 파일에서 도구 로드
    /// </summary>
    public static async Task<IServiceCollection> LoadMCPToolsFromConfigAsync(
        this IServiceCollection services, 
        string configPath,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = services.BuildServiceProvider();
        var loader = serviceProvider.GetRequiredService<MCPToolLoader>();
        
        var tools = await loader.LoadFromConfigAsync(configPath, cancellationToken);
        
        // 로드된 도구들을 DI 컨테이너에 등록
        foreach (var tool in tools)
        {
            services.AddSingleton<IMCPTool>(tool);
        }

        return services;
    }
}