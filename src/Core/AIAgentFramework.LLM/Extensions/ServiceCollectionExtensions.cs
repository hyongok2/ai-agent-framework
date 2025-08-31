
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.LLM.Factories;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Prompts;
using AIAgentFramework.LLM.Parsing;
using AIAgentFramework.LLM.TokenManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIAgentFramework.Core.Infrastructure;

namespace AIAgentFramework.LLM.Extensions;

/// <summary>
/// LLM 서비스 등록을 위한 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// LLM 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddLLMServices(this IServiceCollection services)
    {
        // HTTP 클라이언트 등록
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "AIAgentFramework/1.0");
        });

        services.AddHttpClient("Claude", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "AIAgentFramework/1.0");
        });

        services.AddHttpClient("LocalLLM", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Add("User-Agent", "AIAgentFramework/1.0");
        });

        // 토큰 카운터 등록
        services.AddSingleton<ITokenCounter, TiktokenCounter>();
        
        // LLM Provider Factory 등록
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();

        // 기본 LLM Provider 등록
        services.AddScoped<ILLMProvider>(provider =>
        {
            var factory = provider.GetRequiredService<ILLMProviderFactory>();
            return factory.CreateDefaultProvider();
        });

        // 프롬프트 관리자 등록
        services.AddSingleton<IPromptManager, PromptManager>();

        // 응답 파서 등록
        services.AddSingleton<ILLMResponseParser, LLMResponseParser>();
        services.AddSingleton<LLMResponseParserFactory>();

        // 메모리 캐시 등록 (아직 등록되지 않은 경우)
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// 특정 Provider로 LLM 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="providerType">Provider 타입</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddLLMServices(this IServiceCollection services, string providerType)
    {
        services.AddLLMServices();

        // 지정된 Provider로 기본 Provider 오버라이드
        services.AddScoped<ILLMProvider>(provider =>
        {
            var factory = provider.GetRequiredService<ILLMProviderFactory>();
            return factory.CreateProvider(providerType);
        });

        return services;
    }

    /// <summary>
    /// OpenAI Provider를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="apiKey">API 키</param>
    /// <param name="baseUrl">기본 URL (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddOpenAIProvider(this IServiceCollection services, string apiKey, string? baseUrl = null)
    {
        services.AddHttpClient("OpenAI");

        services.AddScoped<ILLMProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger<OpenAIProvider>>();
            var httpClient = httpClientFactory.CreateClient("OpenAI");

            return new OpenAIProvider(httpClient, logger, apiKey, baseUrl);
        });

        return services;
    }

    /// <summary>
    /// Claude Provider를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="apiKey">API 키</param>
    /// <param name="baseUrl">기본 URL (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddClaudeProvider(this IServiceCollection services, string apiKey, string? baseUrl = null)
    {
        services.AddHttpClient("Claude");

        services.AddScoped<ILLMProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger<ClaudeProvider>>();
            var tokenCounter = provider.GetRequiredService<ITokenCounter>();
            var httpClient = httpClientFactory.CreateClient("Claude");

            return new ClaudeProvider(httpClient, logger, tokenCounter, apiKey, baseUrl);
        });

        return services;
    }

    /// <summary>
    /// Local LLM Provider를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="baseUrl">로컬 LLM 서버 URL</param>
    /// <param name="defaultModel">기본 모델 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddLocalLLMProvider(this IServiceCollection services, string baseUrl, string? defaultModel = null)
    {
        services.AddHttpClient("LocalLLM");

        services.AddScoped<ILLMProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger<LocalLLMProvider>>();
            var httpClient = httpClientFactory.CreateClient("LocalLLM");

            return new LocalLLMProvider(httpClient, logger, baseUrl, defaultModel);
        });

        return services;
    }

    /// <summary>
    /// 설정에서 LLM 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">설정</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddLLMServicesFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLLMServices();

        // 설정에서 Provider별 설정 확인
        var llmSection = configuration.GetSection("LLM");
        var defaultProvider = llmSection["DefaultProvider"] ?? "openai";

        // Provider별 개별 등록도 지원
        var openAISection = llmSection.GetSection("OpenAI");
        if (!string.IsNullOrEmpty(openAISection["ApiKey"]))
        {
            services.AddScoped<OpenAIProvider>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var logger = provider.GetRequiredService<ILogger<OpenAIProvider>>();
                var httpClient = httpClientFactory.CreateClient("OpenAI");

                return new OpenAIProvider(
                    httpClient, 
                    logger, 
                    openAISection["ApiKey"]!, 
                    openAISection["BaseUrl"]);
            });
        }

        var claudeSection = llmSection.GetSection("Claude");
        if (!string.IsNullOrEmpty(claudeSection["ApiKey"]))
        {
            services.AddScoped<ClaudeProvider>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var logger = provider.GetRequiredService<ILogger<ClaudeProvider>>();
                var tokenCounter = provider.GetRequiredService<ITokenCounter>();
                var httpClient = httpClientFactory.CreateClient("Claude");

                return new ClaudeProvider(
                    httpClient, 
                    logger, 
                    tokenCounter,
                    claudeSection["ApiKey"]!, 
                    claudeSection["BaseUrl"]);
            });
        }

        var localSection = llmSection.GetSection("Local");
        if (!string.IsNullOrEmpty(localSection["BaseUrl"]))
        {
            services.AddScoped<LocalLLMProvider>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var logger = provider.GetRequiredService<ILogger<LocalLLMProvider>>();
                var httpClient = httpClientFactory.CreateClient("LocalLLM");

                return new LocalLLMProvider(
                    httpClient, 
                    logger, 
                    localSection["BaseUrl"]!, 
                    localSection["DefaultModel"]);
            });
        }

        return services;
    }

    /// <summary>
    /// 여러 Provider를 동시에 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="providerConfigurations">Provider 설정 목록</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddMultipleLLMProviders(this IServiceCollection services, 
        params (string providerType, Action<IServiceCollection> configure)[] providerConfigurations)
    {
        services.AddLLMServices();

        foreach (var (providerType, configure) in providerConfigurations)
        {
            configure(services);
        }

        // 여러 Provider를 사용할 수 있는 Factory 등록
        services.AddScoped<Func<string, ILLMProvider>>(provider =>
        {
            var factory = provider.GetRequiredService<ILLMProviderFactory>();
            return providerType => factory.CreateProvider(providerType);
        });

        return services;
    }
}