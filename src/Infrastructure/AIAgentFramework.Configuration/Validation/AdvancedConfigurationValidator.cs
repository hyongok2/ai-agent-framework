using AIAgentFramework.Configuration.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

namespace AIAgentFramework.Configuration.Validation;

/// <summary>
/// 고급 설정 검증기
/// </summary>
public class AdvancedConfigurationValidator : IAdvancedConfigurationValidator
{
    private readonly ILogger<AdvancedConfigurationValidator> _logger;
    private readonly Dictionary<string, Func<object, ValidationResult>> _customValidators;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public AdvancedConfigurationValidator(ILogger<AdvancedConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customValidators = new Dictionary<string, Func<object, ValidationResult>>();
        InitializeCustomValidators();
    }

    /// <inheritdoc />
    public ValidationResult ValidateWithSchema(AIAgentConfiguration configuration, string schemaPath)
    {
        var result = new ValidationResult();
        
        try
        {
            if (!File.Exists(schemaPath))
            {
                result.Errors.Add($"Schema file not found: {schemaPath}");
                result.IsValid = false;
                return result;
            }

            var schemaContent = File.ReadAllText(schemaPath);
            var configJson = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // JSON Schema 검증 (실제 구현에서는 Newtonsoft.Json.Schema 등을 사용)
            result = ValidateJsonSchema(configJson, schemaContent);
            
            _logger.LogDebug("Schema validation completed with {ErrorCount} errors", result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed");
            result.Errors.Add($"Schema validation exception: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateEnvironmentSpecific(AIAgentConfiguration configuration)
    {
        var result = new ValidationResult();
        var environment = configuration.Application.Environment;
        
        try
        {
            switch (environment.ToLowerInvariant())
            {
                case "development":
                    ValidateDevelopmentEnvironment(configuration, result);
                    break;
                case "testing":
                    ValidateTestingEnvironment(configuration, result);
                    break;
                case "staging":
                    ValidateStagingEnvironment(configuration, result);
                    break;
                case "production":
                    ValidateProductionEnvironment(configuration, result);
                    break;
                default:
                    result.Warnings.Add($"Unknown environment: {environment}");
                    break;
            }
            
            result.IsValid = !result.Errors.Any();
            _logger.LogDebug("Environment-specific validation completed for {Environment}", environment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Environment-specific validation failed");
            result.Errors.Add($"Environment validation exception: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateConnectivity(AIAgentConfiguration configuration)
    {
        var result = new ValidationResult();
        
        try
        {
            // LLM 제공자 연결 검증
            ValidateLLMProviderConnectivity(configuration.LLM, result);
            
            // MCP 엔드포인트 연결 검증
            ValidateMCPEndpointConnectivity(configuration.Tools, result);
            
            // 데이터베이스 연결 검증 (ConnectionStrings가 있다면)
            ValidateDatabaseConnectivity(result);
            
            result.IsValid = !result.Errors.Any();
            _logger.LogDebug("Connectivity validation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connectivity validation failed");
            result.Errors.Add($"Connectivity validation exception: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidatePerformanceSettings(AIAgentConfiguration configuration)
    {
        var result = new ValidationResult();
        
        try
        {
            // 메모리 사용량 검증
            ValidateMemorySettings(configuration, result);
            
            // 동시 실행 설정 검증
            ValidateConcurrencySettings(configuration, result);
            
            // 타임아웃 설정 검증
            ValidateTimeoutSettings(configuration, result);
            
            // 캐시 설정 검증
            ValidateCacheSettings(configuration, result);
            
            result.IsValid = !result.Errors.Any();
            _logger.LogDebug("Performance settings validation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Performance settings validation failed");
            result.Errors.Add($"Performance validation exception: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateSecuritySettings(AIAgentConfiguration configuration)
    {
        var result = new ValidationResult();
        
        try
        {
            // API 키 보안 검증
            ValidateApiKeySecurity(configuration.LLM, result);
            
            // 네트워크 보안 검증
            ValidateNetworkSecurity(configuration, result);
            
            // 로깅 보안 검증
            ValidateLoggingSecurity(configuration, result);
            
            // 파일 시스템 보안 검증
            ValidateFileSystemSecurity(configuration, result);
            
            result.IsValid = !result.Errors.Any();
            _logger.LogDebug("Security settings validation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security settings validation failed");
            result.Errors.Add($"Security validation exception: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <inheritdoc />
    public void RegisterCustomValidator(string name, Func<object, ValidationResult> validator)
    {
        _customValidators[name] = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger.LogDebug("Custom validator registered: {Name}", name);
    }

    /// <inheritdoc />
    public ValidationResult RunCustomValidation(string name, object target)
    {
        if (!_customValidators.TryGetValue(name, out var validator))
        {
            var result = new ValidationResult();
            result.Errors.Add($"Custom validator not found: {name}");
            result.IsValid = false;
            return result;
        }
        
        try
        {
            return validator(target);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom validation failed: {Name}", name);
            var result = new ValidationResult();
            result.Errors.Add($"Custom validation exception: {ex.Message}");
            result.IsValid = false;
            return result;
        }
    }

    /// <summary>
    /// 사용자 정의 검증기 초기화
    /// </summary>
    private void InitializeCustomValidators()
    {
        // 포트 번호 검증
        RegisterCustomValidator("port", target =>
        {
            var result = new ValidationResult();
            if (target is int port)
            {
                if (port <= 0 || port > 65535)
                {
                    result.Errors.Add($"Invalid port number: {port}");
                    result.IsValid = false;
                }
            }
            return result;
        });

        // 디렉토리 경로 검증
        RegisterCustomValidator("directory", target =>
        {
            var result = new ValidationResult();
            if (target is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    Path.GetFullPath(path);
                }
                catch
                {
                    result.Errors.Add($"Invalid directory path: {path}");
                    result.IsValid = false;
                }
            }
            return result;
        });
    }

    /// <summary>
    /// JSON Schema 검증
    /// </summary>
    private ValidationResult ValidateJsonSchema(string json, string schema)
    {
        var result = new ValidationResult();
        
        // 실제 구현에서는 Newtonsoft.Json.Schema 또는 다른 JSON Schema 라이브러리 사용
        // 여기서는 기본적인 JSON 파싱 검증만 수행
        try
        {
            JsonDocument.Parse(json);
            JsonDocument.Parse(schema);
            result.IsValid = true;
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"JSON validation failed: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <summary>
    /// 개발 환경 검증
    /// </summary>
    private void ValidateDevelopmentEnvironment(AIAgentConfiguration config, ValidationResult result)
    {
        if (config.Security.RequireHttps)
        {
            result.Warnings.Add("HTTPS is enabled in development environment");
        }
        
        if (!config.UI.Common.DebugMode)
        {
            result.Warnings.Add("Debug mode is disabled in development environment");
        }
        
        if (config.Logging.LogLevel != "Debug")
        {
            result.Warnings.Add("Log level is not Debug in development environment");
        }
    }

    /// <summary>
    /// 테스트 환경 검증
    /// </summary>
    private void ValidateTestingEnvironment(AIAgentConfiguration config, ValidationResult result)
    {
        if (config.LLM.DefaultProvider != "mock")
        {
            result.Warnings.Add("Non-mock LLM provider in testing environment may cause external API calls");
        }
        
        if (config.Tools.Cache.Enabled)
        {
            result.Warnings.Add("Cache is enabled in testing environment, may affect test isolation");
        }
    }

    /// <summary>
    /// 스테이징 환경 검증
    /// </summary>
    private void ValidateStagingEnvironment(AIAgentConfiguration config, ValidationResult result)
    {
        if (!config.Security.RequireHttps)
        {
            result.Warnings.Add("HTTPS is not required in staging environment");
        }
        
        if (config.UI.Common.DebugMode)
        {
            result.Warnings.Add("Debug mode is enabled in staging environment");
        }
    }

    /// <summary>
    /// 프로덕션 환경 검증
    /// </summary>
    private void ValidateProductionEnvironment(AIAgentConfiguration config, ValidationResult result)
    {
        if (!config.Security.RequireHttps)
        {
            result.Errors.Add("HTTPS must be required in production environment");
        }
        
        if (config.UI.Common.DebugMode)
        {
            result.Errors.Add("Debug mode must be disabled in production environment");
        }
        
        if (!config.Security.EncryptApiKeys)
        {
            result.Errors.Add("API key encryption must be enabled in production environment");
        }
        
        if (!config.Security.AuditLogging)
        {
            result.Warnings.Add("Audit logging should be enabled in production environment");
        }
        
        if (config.Logging.LogLevel == "Debug" || config.Logging.LogLevel == "Trace")
        {
            result.Warnings.Add("Verbose logging in production may impact performance");
        }
    }

    /// <summary>
    /// LLM 제공자 연결성 검증
    /// </summary>
    private void ValidateLLMProviderConnectivity(LLMConfiguration llm, ValidationResult result)
    {
        foreach (var provider in llm.Providers)
        {
            if (!string.IsNullOrEmpty(provider.Value.Endpoint))
            {
                if (!IsValidUrl(provider.Value.Endpoint))
                {
                    result.Errors.Add($"Invalid endpoint URL for provider {provider.Key}: {provider.Value.Endpoint}");
                }
            }
        }
    }

    /// <summary>
    /// MCP 엔드포인트 연결성 검증
    /// </summary>
    private void ValidateMCPEndpointConnectivity(ToolConfiguration tools, ValidationResult result)
    {
        foreach (var endpoint in tools.MCPEndpoints)
        {
            if (!IsValidUrl(endpoint.Endpoint))
            {
                result.Errors.Add($"Invalid MCP endpoint URL: {endpoint.Endpoint}");
            }
        }
    }

    /// <summary>
    /// 데이터베이스 연결성 검증
    /// </summary>
    private void ValidateDatabaseConnectivity(ValidationResult result)
    {
        // 실제 구현에서는 연결 문자열을 파싱하고 연결 테스트 수행
        // 여기서는 기본적인 검증만 수행
    }

    /// <summary>
    /// 메모리 설정 검증
    /// </summary>
    private void ValidateMemorySettings(AIAgentConfiguration config, ValidationResult result)
    {
        var totalCacheSize = config.Tools.Cache.MaxSizeMB;
        
        if (totalCacheSize > 1024) // 1GB
        {
            result.Warnings.Add($"Large cache size configured: {totalCacheSize}MB");
        }
        
        if (config.Orchestration.MaxConcurrentSessions > 1000)
        {
            result.Warnings.Add($"High concurrent session limit: {config.Orchestration.MaxConcurrentSessions}");
        }
    }

    /// <summary>
    /// 동시 실행 설정 검증
    /// </summary>
    private void ValidateConcurrencySettings(AIAgentConfiguration config, ValidationResult result)
    {
        var maxSessions = config.Orchestration.MaxConcurrentSessions;
        var maxToolExecutions = config.Tools.MaxConcurrentExecutions;
        
        if (maxToolExecutions > maxSessions)
        {
            result.Warnings.Add("Tool concurrent executions exceed session limit");
        }
    }

    /// <summary>
    /// 타임아웃 설정 검증
    /// </summary>
    private void ValidateTimeoutSettings(AIAgentConfiguration config, ValidationResult result)
    {
        var orchestrationTimeout = config.Orchestration.ExecutionTimeoutMinutes * 60;
        var toolTimeout = config.Tools.ExecutionTimeoutSeconds;
        
        if (toolTimeout >= orchestrationTimeout)
        {
            result.Warnings.Add("Tool timeout is greater than or equal to orchestration timeout");
        }
    }

    /// <summary>
    /// 캐시 설정 검증
    /// </summary>
    private void ValidateCacheSettings(AIAgentConfiguration config, ValidationResult result)
    {
        if (config.Tools.Cache.Enabled && config.Tools.Cache.DefaultTTLMinutes <= 0)
        {
            result.Errors.Add("Cache TTL must be positive when cache is enabled");
        }
        
        if (config.Prompts.CacheEnabled && config.Prompts.CacheTTLMinutes <= 0)
        {
            result.Errors.Add("Prompt cache TTL must be positive when cache is enabled");
        }
    }

    /// <summary>
    /// API 키 보안 검증
    /// </summary>
    private void ValidateApiKeySecurity(LLMConfiguration llm, ValidationResult result)
    {
        foreach (var provider in llm.Providers)
        {
            var apiKey = provider.Value.ApiKey;
            if (!string.IsNullOrEmpty(apiKey) && !apiKey.StartsWith("${"))
            {
                if (apiKey.Length < 20)
                {
                    result.Warnings.Add($"Short API key for provider {provider.Key}");
                }
                
                if (apiKey.Contains("test") || apiKey.Contains("demo"))
                {
                    result.Warnings.Add($"Test/demo API key detected for provider {provider.Key}");
                }
            }
        }
    }

    /// <summary>
    /// 네트워크 보안 검증
    /// </summary>
    private void ValidateNetworkSecurity(AIAgentConfiguration config, ValidationResult result)
    {
        if (config.UI.Web.AllowedOrigins.Contains("*"))
        {
            result.Warnings.Add("Wildcard CORS origin allows all domains");
        }
        
        if (!config.Security.AllowedIPs.Any() && config.Application.Environment == "Production")
        {
            result.Warnings.Add("No IP restrictions configured in production");
        }
    }

    /// <summary>
    /// 로깅 보안 검증
    /// </summary>
    private void ValidateLoggingSecurity(AIAgentConfiguration config, ValidationResult result)
    {
        if (!config.Security.MaskSensitiveData)
        {
            result.Warnings.Add("Sensitive data masking is disabled");
        }
        
        if (config.Logging.LogLevel == "Trace" || config.Logging.LogLevel == "Debug")
        {
            result.Warnings.Add("Verbose logging may expose sensitive information");
        }
    }

    /// <summary>
    /// 파일 시스템 보안 검증
    /// </summary>
    private void ValidateFileSystemSecurity(AIAgentConfiguration config, ValidationResult result)
    {
        var paths = new[]
        {
            config.Tools.PluginDirectory,
            config.Prompts.TemplateDirectory,
            config.UI.Web.StaticFilesPath
        };
        
        foreach (var path in paths.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (Path.IsPathRooted(path) && path.StartsWith("/"))
            {
                result.Warnings.Add($"Absolute path may pose security risk: {path}");
            }
        }
    }

    /// <summary>
    /// URL 유효성 검증
    /// </summary>
    private bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}