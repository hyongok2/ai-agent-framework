using Microsoft.Extensions.DependencyInjection;

namespace AIAgentFramework.Tools.Execution;

/// <summary>
/// 도구 실행 서비스 확장 메서드
/// </summary>
public static class ToolExecutionServiceExtensions
{
    /// <summary>
    /// 도구 실행 시스템을 DI 컨테이너에 등록
    /// </summary>
    public static IServiceCollection AddToolExecution(this IServiceCollection services)
    {
        services.AddSingleton<IToolExecutor, ToolExecutor>();
        services.AddSingleton<ToolResultStandardizer>();
        services.AddSingleton<ParameterProcessor>();
        
        return services;
    }
}