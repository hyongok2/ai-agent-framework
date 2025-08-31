
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.LLM.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Factories;

/// <summary>
/// LLM Provider Factory 구현
/// </summary>
public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Func<ILLMProvider>> _providerFactories;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="configuration">설정</param>
    /// <param name="httpClientFactory">HTTP 클라이언트 팩토리</param>
    /// <param name="loggerFactory">로거 팩토리</param>
    /// <param name="serviceProvider">서비스 프로바이더</param>
    public LLMProviderFactory(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _providerFactories = new Dictionary<string, Func<ILLMProvider>>(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = CreateOpenAIProvider,
            ["claude"] = CreateClaudeProvider,
            ["local"] = CreateLocalLLMProvider,
            ["ollama"] = CreateLocalLLMProvider
        };
    }

    /// <inheritdoc />
    public ILLMProvider CreateProvider(string providerType)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be null or empty", nameof(providerType));

        if (!_providerFactories.TryGetValue(providerType, out var factory))
        {
            throw new NotSupportedException($"Provider type '{providerType}' is not supported. " +
                $"Supported providers: {string.Join(", ", GetAvailableProviders())}");
        }

        try
        {
            return factory();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create provider '{providerType}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ILLMProvider CreateProviderForRole(string role)
    {
        // 역할에 따른 Provider 매핑 (설정에서 가져올 수 있음)
        var providerType = _configuration[$"LLM:Roles:{role}:Provider"] ?? 
                          _configuration["LLM:DefaultProvider"] ?? 
                          "openai";
        
        return CreateProvider(providerType);
    }

    /// <inheritdoc />
    public List<string> GetSupportedProviderTypes()
    {
        return _providerFactories.Keys.ToList();
    }

    /// <summary>
    /// 기본 Provider 생성
    /// </summary>
    /// <returns>기본 LLM Provider</returns>
    public ILLMProvider CreateDefaultProvider()
    {
        var defaultProviderType = _configuration["LLM:DefaultProvider"] ?? "openai";
        return CreateProvider(defaultProviderType);
    }

    /// <summary>
    /// 사용 가능한 Provider 목록
    /// </summary>
    /// <returns>Provider 타입 목록</returns>
    public IReadOnlyList<string> GetAvailableProviders()
    {
        return _providerFactories.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Provider가 지원되는지 확인
    /// </summary>
    /// <param name="providerType">Provider 타입</param>
    /// <returns>지원 여부</returns>
    public bool IsProviderSupported(string providerType)
    {
        return !string.IsNullOrWhiteSpace(providerType) && 
               _providerFactories.ContainsKey(providerType);
    }

    /// <summary>
    /// OpenAI Provider 생성
    /// </summary>
    /// <returns>OpenAI Provider</returns>
    private ILLMProvider CreateOpenAIProvider()
    {
        var apiKey = _configuration["LLM:OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please set 'LLM:OpenAI:ApiKey' in configuration.");
        }

        var baseUrl = _configuration["LLM:OpenAI:BaseUrl"];
        var httpClient = _httpClientFactory.CreateClient("OpenAI");
        var logger = _loggerFactory.CreateLogger<OpenAIProvider>();

        return new OpenAIProvider(httpClient, logger, apiKey, baseUrl);
    }

    /// <summary>
    /// Claude Provider 생성
    /// </summary>
    /// <returns>Claude Provider</returns>
    private ILLMProvider CreateClaudeProvider()
    {
        var apiKey = _configuration["LLM:Claude:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Claude API key is not configured. Please set 'LLM:Claude:ApiKey' in configuration.");
        }

        var baseUrl = _configuration["LLM:Claude:BaseUrl"];
        var httpClient = _httpClientFactory.CreateClient("Claude");
        var logger = _loggerFactory.CreateLogger<ClaudeProvider>();
        var tokenCounter = _serviceProvider.GetRequiredService<ITokenCounter>();

        return new ClaudeProvider(httpClient, logger, tokenCounter, apiKey, baseUrl);
    }

    /// <summary>
    /// Local LLM Provider 생성
    /// </summary>
    /// <returns>Local LLM Provider</returns>
    private ILLMProvider CreateLocalLLMProvider()
    {
        var baseUrl = _configuration["LLM:Local:BaseUrl"] ?? "http://localhost:11434";
        var defaultModel = _configuration["LLM:Local:DefaultModel"];
        
        var httpClient = _httpClientFactory.CreateClient("LocalLLM");
        var logger = _loggerFactory.CreateLogger<LocalLLMProvider>();

        return new LocalLLMProvider(httpClient, logger, baseUrl, defaultModel);
    }
}

/// <summary>
/// LLM Provider Factory 확장 메서드
/// </summary>
public static class LLMProviderFactoryExtensions
{
    /// <summary>
    /// 모델별 Provider 생성
    /// </summary>
    /// <param name="factory">Factory</param>
    /// <param name="model">모델명</param>
    /// <returns>해당 모델을 지원하는 Provider</returns>
    public static ILLMProvider CreateProviderForModel(this ILLMProviderFactory factory, string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be null or empty", nameof(model));

        // 모델명에 따른 Provider 매핑
        var providerType = model.ToLowerInvariant() switch
        {
            var m when m.StartsWith("gpt") => "openai",
            var m when m.StartsWith("claude") => "claude",
            var m when m.StartsWith("llama") => "local",
            var m when m.StartsWith("mistral") => "local",
            var m when m.StartsWith("mixtral") => "local",
            var m when m.StartsWith("phi") => "local",
            var m when m.StartsWith("gemma") => "local",
            _ => throw new NotSupportedException($"Model '{model}' is not supported")
        };

        return factory.CreateProvider(providerType);
    }

    /// <summary>
    /// 사용 가능한 모든 Provider의 모델 목록 조회
    /// </summary>
    /// <param name="factory">Factory</param>
    /// <returns>Provider별 모델 목록</returns>
    public static Task<Dictionary<string, IReadOnlyList<string>>> GetAllSupportedModelsAsync(this ILLMProviderFactory factory)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var providerType in factory.GetSupportedProviderTypes())
        {
            try
            {
                var provider = factory.CreateProvider(providerType);
                result[providerType] = provider.SupportedModels;
            }
            catch (Exception)
            {
                // Provider 생성 실패 시 무시
                result[providerType] = new List<string>().AsReadOnly();
            }
        }

        return Task.FromResult(result);
    }
}