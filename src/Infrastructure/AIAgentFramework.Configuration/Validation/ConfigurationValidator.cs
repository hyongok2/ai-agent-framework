using AIAgentFramework.Configuration.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIAgentFramework.Configuration.Validation;

/// <summary>
/// 설정 검증기
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfiguration(AIAgentConfiguration configuration)
    {
        var result = new ValidationResult();
        
        try
        {
            // 애플리케이션 설정 검증
            ValidateApplication(configuration.Application, result);
            
            // LLM 설정 검증
            ValidateLLM(configuration.LLM, result);
            
            // 도구 설정 검증
            ValidateTools(configuration.Tools, result);
            
            // 프롬프트 설정 검증
            ValidatePrompts(configuration.Prompts, result);
            
            // UI 설정 검증
            ValidateUI(configuration.UI, result);
            
            // 오케스트레이션 설정 검증
            ValidateOrchestration(configuration.Orchestration, result);
            
            // 로깅 설정 검증
            ValidateLogging(configuration.Logging, result);
            
            // 보안 설정 검증
            ValidateSecurity(configuration.Security, result);
            
            result.IsValid = !result.Errors.Any();
            
            if (result.IsValid)
            {
                _logger.LogInformation("Configuration validation passed");
            }
            else
            {
                _logger.LogError("Configuration validation failed with {ErrorCount} errors", result.Errors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed with exception");
            result.Errors.Add($"Validation exception: {ex.Message}");
            result.IsValid = false;
        }
        
        return result;
    }

    /// <summary>
    /// 애플리케이션 설정 검증
    /// </summary>
    private void ValidateApplication(ApplicationInfo application, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(application.Name))
        {
            result.Errors.Add("Application.Name is required");
        }
        
        if (string.IsNullOrWhiteSpace(application.Version))
        {
            result.Errors.Add("Application.Version is required");
        }
        else if (!IsValidVersion(application.Version))
        {
            result.Warnings.Add($"Application.Version '{application.Version}' format may be invalid");
        }
        
        var validEnvironments = new[] { "Development", "Testing", "Staging", "Production" };
        if (!validEnvironments.Contains(application.Environment, StringComparer.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"Application.Environment '{application.Environment}' is not a standard environment");
        }
    }

    /// <summary>
    /// LLM 설정 검증
    /// </summary>
    private void ValidateLLM(LLMConfiguration llm, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(llm.DefaultProvider))
        {
            result.Errors.Add("LLM.DefaultProvider is required");
        }
        
        // 모델 매핑 검증
        var requiredRoles = new[]
        {
            "planner", "interpreter", "summarizer", "generator", "evaluator",
            "rewriter", "explainer", "reasoner", "converter", "visualizer",
            "tool_parameter_setter", "dialogue_manager", "knowledge_retriever", "meta_manager"
        };
        
        foreach (var role in requiredRoles)
        {
            if (!llm.Models.ContainsKey(role) || string.IsNullOrWhiteSpace(llm.Models[role]))
            {
                result.Errors.Add($"LLM.Models['{role}'] is required");
            }
        }
        
        // 제공자 설정 검증
        foreach (var provider in llm.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Value.ApiKey) && 
                string.IsNullOrWhiteSpace(provider.Value.Endpoint))
            {
                result.Errors.Add($"LLM.Providers['{provider.Key}'] must have either ApiKey or Endpoint");
            }
            
            if (provider.Value.MaxRetries < 0)
            {
                result.Errors.Add($"LLM.Providers['{provider.Key}'].MaxRetries must be non-negative");
            }
            
            if (provider.Value.TimeoutSeconds <= 0)
            {
                result.Errors.Add($"LLM.Providers['{provider.Key}'].TimeoutSeconds must be positive");
            }
        }
        
        // 기본 파라미터 검증
        if (llm.DefaultParameters.MaxTokens <= 0)
        {
            result.Errors.Add("LLM.DefaultParameters.MaxTokens must be positive");
        }
        
        if (llm.DefaultParameters.Temperature < 0 || llm.DefaultParameters.Temperature > 2)
        {
            result.Warnings.Add("LLM.DefaultParameters.Temperature should be between 0 and 2");
        }
    }

    /// <summary>
    /// 도구 설정 검증
    /// </summary>
    private void ValidateTools(ToolConfiguration tools, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(tools.PluginDirectory))
        {
            result.Errors.Add("Tools.PluginDirectory is required");
        }
        
        if (tools.ExecutionTimeoutSeconds <= 0)
        {
            result.Errors.Add("Tools.ExecutionTimeoutSeconds must be positive");
        }
        
        if (tools.MaxConcurrentExecutions <= 0)
        {
            result.Errors.Add("Tools.MaxConcurrentExecutions must be positive");
        }
        
        // MCP 엔드포인트 검증
        foreach (var endpoint in tools.MCPEndpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Name))
            {
                result.Errors.Add("MCP endpoint name is required");
            }
            
            if (string.IsNullOrWhiteSpace(endpoint.Endpoint))
            {
                result.Errors.Add($"MCP endpoint '{endpoint.Name}' URL is required");
            }
            else if (!IsValidUrl(endpoint.Endpoint))
            {
                result.Errors.Add($"MCP endpoint '{endpoint.Name}' URL is invalid");
            }
        }
    }

    /// <summary>
    /// 프롬프트 설정 검증
    /// </summary>
    private void ValidatePrompts(PromptConfiguration prompts, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(prompts.TemplateDirectory))
        {
            result.Errors.Add("Prompts.TemplateDirectory is required");
        }
        
        if (prompts.CacheTTLMinutes <= 0)
        {
            result.Errors.Add("Prompts.CacheTTLMinutes must be positive");
        }
        
        if (string.IsNullOrWhiteSpace(prompts.FileExtension))
        {
            result.Errors.Add("Prompts.FileExtension is required");
        }
        
        // 역할별 설정 검증
        foreach (var roleConfig in prompts.RoleConfigurations)
        {
            if (!string.IsNullOrWhiteSpace(roleConfig.Value.ExpectedResponseSchema))
            {
                try
                {
                    JsonDocument.Parse(roleConfig.Value.ExpectedResponseSchema);
                }
                catch (JsonException)
                {
                    result.Errors.Add($"Role '{roleConfig.Key}' has invalid JSON schema");
                }
            }
        }
    }

    /// <summary>
    /// UI 설정 검증
    /// </summary>
    private void ValidateUI(UIConfiguration ui, ValidationResult result)
    {
        var validInterfaces = new[] { "web", "console", "api", "application" };
        foreach (var interfaceType in ui.EnabledInterfaces)
        {
            if (!validInterfaces.Contains(interfaceType, StringComparer.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"Unknown interface type: {interfaceType}");
            }
        }
        
        // Web 설정 검증
        if (ui.Web.Port <= 0 || ui.Web.Port > 65535)
        {
            result.Errors.Add("UI.Web.Port must be between 1 and 65535");
        }
        
        // API 설정 검증
        if (ui.API.RateLimitPerMinute <= 0)
        {
            result.Errors.Add("UI.API.RateLimitPerMinute must be positive");
        }
    }

    /// <summary>
    /// 오케스트레이션 설정 검증
    /// </summary>
    private void ValidateOrchestration(OrchestrationConfiguration orchestration, ValidationResult result)
    {
        if (orchestration.MaxExecutionSteps <= 0)
        {
            result.Errors.Add("Orchestration.MaxExecutionSteps must be positive");
        }
        
        if (orchestration.ExecutionTimeoutMinutes <= 0)
        {
            result.Errors.Add("Orchestration.ExecutionTimeoutMinutes must be positive");
        }
        
        if (orchestration.MaxConcurrentSessions <= 0)
        {
            result.Errors.Add("Orchestration.MaxConcurrentSessions must be positive");
        }
        
        if (orchestration.SessionExpirationHours <= 0)
        {
            result.Errors.Add("Orchestration.SessionExpirationHours must be positive");
        }
    }

    /// <summary>
    /// 로깅 설정 검증
    /// </summary>
    private void ValidateLogging(LoggingConfiguration logging, ValidationResult result)
    {
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None" };
        if (!validLogLevels.Contains(logging.LogLevel, StringComparer.OrdinalIgnoreCase))
        {
            result.Errors.Add($"Logging.LogLevel '{logging.LogLevel}' is not valid");
        }
        
        if (logging.RetentionDays <= 0)
        {
            result.Errors.Add("Logging.RetentionDays must be positive");
        }
    }

    /// <summary>
    /// 보안 설정 검증
    /// </summary>
    private void ValidateSecurity(SecurityConfiguration security, ValidationResult result)
    {
        // IP 주소 형식 검증
        foreach (var ip in security.AllowedIPs)
        {
            if (!IsValidIPAddress(ip))
            {
                result.Errors.Add($"Invalid IP address or CIDR: {ip}");
            }
        }
    }

    /// <summary>
    /// 버전 형식 검증
    /// </summary>
    private bool IsValidVersion(string version)
    {
        return Regex.IsMatch(version, @"^\d+\.\d+\.\d+(-\w+)?$");
    }

    /// <summary>
    /// URL 형식 검증
    /// </summary>
    private bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// IP 주소 형식 검증
    /// </summary>
    private bool IsValidIPAddress(string ipAddress)
    {
        // CIDR 표기법 지원
        if (ipAddress.Contains('/'))
        {
            var parts = ipAddress.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var prefix) || prefix < 0 || prefix > 32)
            {
                return false;
            }
            ipAddress = parts[0];
        }
        
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}