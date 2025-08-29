using AIAgentFramework.Configuration.Models;
using AIAgentFramework.Configuration.Validation;
using AIAgentFramework.Configuration.Templates;
using AIAgentFramework.Configuration.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Configuration.Extensions;

/// <summary>
/// 서비스 컬렉션 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// AI 에이전트 설정 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">설정</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddAIAgentConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 설정 모델 등록
        services.Configure<AIAgentConfiguration>(configuration);
        services.Configure<LLMConfiguration>(configuration.GetSection("LLM"));
        services.Configure<ToolConfiguration>(configuration.GetSection("Tools"));
        services.Configure<PromptConfiguration>(configuration.GetSection("Prompts"));
        services.Configure<UIConfiguration>(configuration.GetSection("UI"));
        services.Configure<OrchestrationConfiguration>(configuration.GetSection("Orchestration"));
        services.Configure<LoggingConfiguration>(configuration.GetSection("Logging"));
        services.Configure<SecurityConfiguration>(configuration.GetSection("Security"));
        
        // 설정 관리자 등록
        services.AddSingleton<IAIAgentConfigurationManager, AIAgentConfigurationManager>();
        
        // 설정 검증기 등록
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<IAdvancedConfigurationValidator, AdvancedConfigurationValidator>();
        
        // 템플릿 생성기 등록
        services.AddSingleton<IConfigurationTemplateGenerator, ConfigurationTemplateGenerator>();
        
        // 설정 도구 등록
        services.AddSingleton<IConfigurationTool, ConfigurationTool>();
        
        // 메모리 캐시 등록 (설정 캐싱용)
        services.AddMemoryCache();
        
        return services;
    }
    
    /// <summary>
    /// YAML 설정 파일을 추가합니다.
    /// </summary>
    /// <param name="builder">설정 빌더</param>
    /// <param name="path">YAML 파일 경로</param>
    /// <param name="optional">선택적 파일 여부</param>
    /// <param name="reloadOnChange">변경 시 다시 로드 여부</param>
    /// <returns>설정 빌더</returns>
    public static IConfigurationBuilder AddYamlFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false)
    {
        return builder.AddYamlFile(path, optional, reloadOnChange);
    }
    
    /// <summary>
    /// 환경별 YAML 설정 파일을 추가합니다.
    /// </summary>
    /// <param name="builder">설정 빌더</param>
    /// <param name="basePath">기본 파일 경로 (확장자 제외)</param>
    /// <param name="environmentName">환경 이름</param>
    /// <param name="optional">선택적 파일 여부</param>
    /// <param name="reloadOnChange">변경 시 다시 로드 여부</param>
    /// <returns>설정 빌더</returns>
    public static IConfigurationBuilder AddEnvironmentYamlFile(
        this IConfigurationBuilder builder,
        string basePath,
        string? environmentName = null,
        bool optional = true,
        bool reloadOnChange = false)
    {
        environmentName ??= System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var environmentFile = $"{basePath}.{environmentName.ToLowerInvariant()}.yaml";
        
        return builder.AddYamlFile(environmentFile, optional, reloadOnChange);
    }
    
    /// <summary>
    /// AI 에이전트 설정을 검증합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection ValidateAIAgentConfiguration(this IServiceCollection services)
    {
        // 서비스 프로바이더 빌드
        using var serviceProvider = services.BuildServiceProvider();
        
        // 설정 관리자 및 검증기 가져오기
        var configManager = serviceProvider.GetRequiredService<IAIAgentConfigurationManager>();
        var validator = serviceProvider.GetRequiredService<IConfigurationValidator>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ConfigurationValidation");
        
        try
        {
            // 설정 로드 및 검증
            var config = configManager.GetConfiguration();
            var validationResult = validator.ValidateConfiguration(config);
            
            if (!validationResult.IsValid)
            {
                var errorMessage = $"Configuration validation failed:\n{validationResult.GetFormattedMessages()}";
                logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            if (validationResult.Warnings.Any())
            {
                logger.LogWarning("Configuration validation completed with warnings:\n{Warnings}", 
                    string.Join("\n", validationResult.Warnings.Select(w => $"  - {w}")));
            }
            
            logger.LogInformation("Configuration validation passed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate AI Agent configuration");
            throw;
        }
        
        return services;
    }
}