using AIAgentFramework.Configuration.Models;
using AIAgentFramework.Configuration.Interfaces;
using AIAgentFramework.Configuration.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace AIAgentFramework.Configuration;

/// <summary>
/// AI 에이전트 설정 관리자
/// </summary>
public class AIAgentConfigurationManager : IAIAgentConfigurationManager, IConfigurationCache
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIAgentConfigurationManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConfigurationCache _configurationCache;
    private readonly string _environment;
    private AIAgentConfiguration? _cachedConfiguration;
    private DateTime _lastLoadTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="configuration">설정</param>
    /// <param name="cache">메모리 캐시</param>
    /// <param name="loggerFactory">로거 팩토리</param>
    public AIAgentConfigurationManager(
        IConfiguration configuration,
        IMemoryCache cache,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<AIAgentConfigurationManager>();
        
        // ConfigurationCache용 로거 생성
        var cacheLogger = loggerFactory.CreateLogger<ConfigurationCache>();
        _configurationCache = new ConfigurationCache(cache, cacheLogger);
        _environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    /// <inheritdoc />
    public AIAgentConfiguration GetConfiguration()
    {
        var cacheKey = $"configuration_{_environment}";
        
        if (_cache.TryGetValue(cacheKey, out AIAgentConfiguration? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        var config = LoadConfiguration();
        
        _cache.Set(cacheKey, config, _cacheExpiration);
        _configurationCache.TrackKey(cacheKey);
        _cachedConfiguration = config;
        _lastLoadTime = DateTime.UtcNow;
        
        _logger.LogInformation("Configuration loaded for environment: {Environment}", _environment);
        
        return config;
    }

    /// <inheritdoc />
    public T GetSection<T>(string sectionName) where T : class, new()
    {
        var cacheKey = $"section_{sectionName}_{_environment}";
        
        if (_cache.TryGetValue(cacheKey, out T? cachedSection) && cachedSection != null)
        {
            return cachedSection;
        }

        var section = _configuration.GetSection(sectionName).Get<T>() ?? new T();
        
        _cache.Set(cacheKey, section, _cacheExpiration);
        _configurationCache.TrackKey(cacheKey);
        
        _logger.LogDebug("Configuration section '{SectionName}' loaded", sectionName);
        
        return section;
    }

    /// <inheritdoc />
    public void ReloadConfiguration()
    {
        _logger.LogInformation("Configuration 다시 로딩 중...");
        
        // 타입 안전한 캐시 무효화 구현
        _configurationCache.InvalidateAll();
        
        // 설정 다시 로드
        if (_configuration is IConfigurationRoot configRoot)
        {
            configRoot.Reload();
        }
        
        _cachedConfiguration = null;
        
        _logger.LogInformation("Configuration 다시 로딩 완료");
    }

    /// <inheritdoc />
    public bool ValidateConfiguration()
    {
        try
        {
            var config = GetConfiguration();
            
            // 필수 설정 검증
            var validationErrors = new List<string>();
            
            // LLM 설정 검증
            if (string.IsNullOrWhiteSpace(config.LLM.DefaultProvider))
            {
                validationErrors.Add("LLM.DefaultProvider is required");
            }
            
            // 프롬프트 설정 검증
            if (string.IsNullOrWhiteSpace(config.Prompts.TemplateDirectory))
            {
                validationErrors.Add("Prompts.TemplateDirectory is required");
            }
            
            // 도구 설정 검증
            if (string.IsNullOrWhiteSpace(config.Tools.PluginDirectory))
            {
                validationErrors.Add("Tools.PluginDirectory is required");
            }
            
            if (validationErrors.Any())
            {
                _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", validationErrors));
                return false;
            }
            
            _logger.LogInformation("Configuration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed with exception");
            return false;
        }
    }

    /// <inheritdoc />
    public string GetConnectionString(string name)
    {
        var connectionString = _configuration.GetConnectionString(name);
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' not found");
        }
        
        return connectionString;
    }

    /// <inheritdoc />
    public void SetValue(string key, object value)
    {
        _configuration[key] = value?.ToString();
        
        // 관련 캐시 무효화 - 패턴 기반으로 관련된 모든 캐시 제거
        _configurationCache.Invalidate($"*{_environment}*");
        
        _logger.LogDebug("Configuration value set: {Key} = {Value}", key, value);
    }

    /// <summary>
    /// 설정을 로드합니다.
    /// </summary>
    /// <returns>AI 에이전트 설정</returns>
    private AIAgentConfiguration LoadConfiguration()
    {
        var config = new AIAgentConfiguration();
        
        try
        {
            // 각 섹션별로 바인딩
            _configuration.GetSection("Application").Bind(config.Application);
            _configuration.GetSection("LLM").Bind(config.LLM);
            _configuration.GetSection("Tools").Bind(config.Tools);
            _configuration.GetSection("Prompts").Bind(config.Prompts);
            _configuration.GetSection("UI").Bind(config.UI);
            _configuration.GetSection("Orchestration").Bind(config.Orchestration);
            _configuration.GetSection("Logging").Bind(config.Logging);
            _configuration.GetSection("Security").Bind(config.Security);
            
            // 환경별 설정 오버라이드
            ApplyEnvironmentOverrides(config);
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            throw new InvalidOperationException("Configuration loading failed", ex);
        }
    }

    /// <summary>
    /// 환경별 설정 오버라이드를 적용합니다.
    /// </summary>
    /// <param name="config">설정</param>
    private void ApplyEnvironmentOverrides(AIAgentConfiguration config)
    {
        config.Application.Environment = _environment;
        
        // 개발 환경 설정
        if (_environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.Logging.LogLevel = "Debug";
            config.UI.Common.DebugMode = true;
            config.Security.RequireHttps = false;
        }
        // 프로덕션 환경 설정
        else if (_environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            config.Logging.LogLevel = "Warning";
            config.UI.Common.DebugMode = false;
            config.Security.RequireHttps = true;
            config.Security.EncryptApiKeys = true;
        }
        
        _logger.LogDebug("Environment-specific overrides applied for: {Environment}", _environment);
    }

    #region IConfigurationCache Implementation

    /// <inheritdoc />
    public void Invalidate(string? keyPattern = null)
    {
        _configurationCache.Invalidate(keyPattern);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        _configurationCache.InvalidateAll();
    }

    /// <inheritdoc />
    public Task WarmupAsync(IEnumerable<string> keys)
    {
        return _configurationCache.WarmupAsync(keys);
    }

    /// <inheritdoc />
    public CacheStatistics GetStatistics()
    {
        return _configurationCache.GetStatistics();
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        return _configurationCache.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool RemoveKey(string key)
    {
        return _configurationCache.RemoveKey(key);
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetCachedKeys()
    {
        return _configurationCache.GetCachedKeys();
    }

    #endregion
}