using AIAgentFramework.Configuration.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIAgentFramework.Configuration.Templates;

/// <summary>
/// 설정 템플릿 생성기
/// </summary>
public class ConfigurationTemplateGenerator : IConfigurationTemplateGenerator
{
    private readonly ILogger<ConfigurationTemplateGenerator> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public ConfigurationTemplateGenerator(ILogger<ConfigurationTemplateGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GenerateYamlTemplate(string environment = "Development")
    {
        var config = CreateDefaultConfiguration(environment);
        return ConvertToYaml(config);
    }

    /// <inheritdoc />
    public string GenerateJsonTemplate(string environment = "Development")
    {
        var config = CreateDefaultConfiguration(environment);
        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <inheritdoc />
    public AIAgentConfiguration CreateDefaultConfiguration(string environment = "Development")
    {
        var config = new AIAgentConfiguration
        {
            Application = new ApplicationInfo
            {
                Name = "AI Agent Framework",
                Version = "1.0.0",
                Environment = environment,
                InstanceId = System.Environment.MachineName
            },
            LLM = CreateDefaultLLMConfiguration(environment),
            Tools = CreateDefaultToolConfiguration(environment),
            Prompts = CreateDefaultPromptConfiguration(environment),
            UI = CreateDefaultUIConfiguration(environment),
            Orchestration = CreateDefaultOrchestrationConfiguration(environment),
            Logging = CreateDefaultLoggingConfiguration(environment),
            Security = CreateDefaultSecurityConfiguration(environment)
        };

        _logger.LogDebug("Default configuration created for environment: {Environment}", environment);
        return config;
    }

    /// <inheritdoc />
    public void SaveTemplate(string filePath, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
            _logger.LogInformation("Configuration template saved to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration template to: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public bool ValidateTemplate(string templateContent, string format = "yaml")
    {
        try
        {
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                JsonSerializer.Deserialize<AIAgentConfiguration>(templateContent);
            }
            else if (format.Equals("yaml", StringComparison.OrdinalIgnoreCase))
            {
                // YAML 검증은 NetEscapades.Configuration.Yaml을 사용하여 파싱 시도
                // 실제 구현에서는 YamlDotNet 등을 사용할 수 있음
                return !string.IsNullOrWhiteSpace(templateContent);
            }

            _logger.LogDebug("Template validation passed for format: {Format}", format);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template validation failed for format: {Format}", format);
            return false;
        }
    }

    /// <summary>
    /// 기본 LLM 설정 생성
    /// </summary>
    private LLMConfiguration CreateDefaultLLMConfiguration(string environment)
    {
        var config = new LLMConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.DefaultProvider = "mock";
            config.Providers["mock"] = new ProviderConfiguration
            {
                ApiKey = "test-api-key",
                MaxRetries = 1,
                TimeoutSeconds = 5
            };
            config.DefaultParameters.MaxTokens = 100;
            config.DefaultParameters.Temperature = 0.0;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.Providers["openai"] = new ProviderConfiguration
            {
                ApiKey = "${OPENAI_API_KEY}",
                MaxRetries = 3,
                TimeoutSeconds = 30
            };
            config.DefaultParameters.Temperature = 0.5;
        }
        else // Production
        {
            config.Providers["openai"] = new ProviderConfiguration
            {
                ApiKey = "${OPENAI_API_KEY}",
                OrganizationId = "${OPENAI_ORG_ID}",
                MaxRetries = 5,
                TimeoutSeconds = 60
            };
            config.DefaultParameters.Temperature = 0.3;
        }

        return config;
    }

    /// <summary>
    /// 기본 도구 설정 생성
    /// </summary>
    private ToolConfiguration CreateDefaultToolConfiguration(string environment)
    {
        var config = new ToolConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.PluginDirectory = "./test-plugins";
            config.ExecutionTimeoutSeconds = 10;
            config.MaxConcurrentExecutions = 1;
            config.Cache.Enabled = false;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.ExecutionTimeoutSeconds = 30;
            config.MaxConcurrentExecutions = 2;
            config.Cache.DefaultTTLMinutes = 5;
        }
        else // Production
        {
            config.ExecutionTimeoutSeconds = 120;
            config.MaxConcurrentExecutions = 10;
            config.Cache.DefaultTTLMinutes = 60;
            config.Cache.MaxSizeMB = 500;
        }

        return config;
    }

    /// <summary>
    /// 기본 프롬프트 설정 생성
    /// </summary>
    private PromptConfiguration CreateDefaultPromptConfiguration(string environment)
    {
        var config = new PromptConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.TemplateDirectory = "./test-prompts";
            config.CacheTTLMinutes = 1;
            config.CacheEnabled = false;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.CacheTTLMinutes = 5;
        }
        else // Production
        {
            config.CacheTTLMinutes = 120;
        }

        // 공통 변수 설정
        config.CommonVariables["system_name"] = "AI Agent Framework";
        config.CommonVariables["current_date"] = "{{current_date}}";
        config.CommonVariables["current_time"] = "{{current_time}}";

        return config;
    }

    /// <summary>
    /// 기본 UI 설정 생성
    /// </summary>
    private UIConfiguration CreateDefaultUIConfiguration(string environment)
    {
        var config = new UIConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.EnabledInterfaces = new List<string> { "api" };
            config.Web.Port = 5002;
            config.Console.LogLevel = "Debug";
            config.Console.OutputFormat = "json";
            config.Common.DebugMode = true;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.Web.Port = 5001;
            config.Console.LogLevel = "Debug";
            config.API.SwaggerEnabled = true;
            config.Common.DebugMode = true;
        }
        else // Production
        {
            config.Web.Port = 443;
            config.Web.UseHttps = true;
            config.Web.AllowedOrigins = new List<string> { "https://yourdomain.com" };
            config.Console.LogLevel = "Warning";
            config.API.AuthenticationEnabled = true;
            config.API.SwaggerEnabled = false;
            config.Common.DebugMode = false;
        }

        return config;
    }

    /// <summary>
    /// 기본 오케스트레이션 설정 생성
    /// </summary>
    private OrchestrationConfiguration CreateDefaultOrchestrationConfiguration(string environment)
    {
        var config = new OrchestrationConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.MaxExecutionSteps = 10;
            config.ExecutionTimeoutMinutes = 1;
            config.MaxConcurrentSessions = 5;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.MaxExecutionSteps = 20;
            config.ExecutionTimeoutMinutes = 5;
            config.MaxConcurrentSessions = 10;
        }
        else // Production
        {
            config.MaxExecutionSteps = 100;
            config.ExecutionTimeoutMinutes = 30;
            config.MaxConcurrentSessions = 1000;
            config.HistoryRetentionDays = 90;
        }

        return config;
    }

    /// <summary>
    /// 기본 로깅 설정 생성
    /// </summary>
    private LoggingConfiguration CreateDefaultLoggingConfiguration(string environment)
    {
        var config = new LoggingConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.LogLevel = "Debug";
            config.LogFilePath = "./logs/test/aiagent.log";
            config.PerformanceLogging = false;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.LogLevel = "Debug";
            config.LogFilePath = "./logs/dev/aiagent.log";
        }
        else // Production
        {
            config.LogLevel = "Warning";
            config.LogFilePath = "/var/log/aiagent/aiagent.log";
            config.RetentionDays = 90;
        }

        return config;
    }

    /// <summary>
    /// 기본 보안 설정 생성
    /// </summary>
    private SecurityConfiguration CreateDefaultSecurityConfiguration(string environment)
    {
        var config = new SecurityConfiguration();

        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            config.EncryptApiKeys = false;
            config.MaskSensitiveData = false;
            config.AuditLogging = false;
            config.RequireHttps = false;
        }
        else if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            config.RequireHttps = false;
        }
        else // Production
        {
            config.EncryptApiKeys = true;
            config.MaskSensitiveData = true;
            config.AuditLogging = true;
            config.RequireHttps = true;
            config.AllowedIPs = new List<string>
            {
                "10.0.0.0/8",
                "172.16.0.0/12",
                "192.168.0.0/16"
            };
        }

        return config;
    }

    /// <summary>
    /// 설정을 YAML 형식으로 변환
    /// </summary>
    private string ConvertToYaml(AIAgentConfiguration config)
    {
        var yaml = new StringBuilder();
        
        yaml.AppendLine("# AI Agent Framework Configuration");
        yaml.AppendLine();
        
        // Application 섹션
        yaml.AppendLine("Application:");
        yaml.AppendLine($"  Name: \"{config.Application.Name}\"");
        yaml.AppendLine($"  Version: \"{config.Application.Version}\"");
        yaml.AppendLine($"  Environment: \"{config.Application.Environment}\"");
        yaml.AppendLine($"  InstanceId: \"{config.Application.InstanceId}\"");
        yaml.AppendLine();
        
        // LLM 섹션
        yaml.AppendLine("LLM:");
        yaml.AppendLine($"  DefaultProvider: \"{config.LLM.DefaultProvider}\"");
        yaml.AppendLine("  Models:");
        foreach (var model in config.LLM.Models)
        {
            yaml.AppendLine($"    {model.Key}: \"{model.Value}\"");
        }
        yaml.AppendLine("  Providers:");
        foreach (var provider in config.LLM.Providers)
        {
            yaml.AppendLine($"    {provider.Key}:");
            yaml.AppendLine($"      ApiKey: \"{provider.Value.ApiKey}\"");
            if (!string.IsNullOrEmpty(provider.Value.Endpoint))
                yaml.AppendLine($"      Endpoint: \"{provider.Value.Endpoint}\"");
            if (!string.IsNullOrEmpty(provider.Value.OrganizationId))
                yaml.AppendLine($"      OrganizationId: \"{provider.Value.OrganizationId}\"");
            yaml.AppendLine($"      MaxRetries: {provider.Value.MaxRetries}");
            yaml.AppendLine($"      TimeoutSeconds: {provider.Value.TimeoutSeconds}");
        }
        yaml.AppendLine("  DefaultParameters:");
        yaml.AppendLine($"    MaxTokens: {config.LLM.DefaultParameters.MaxTokens}");
        yaml.AppendLine($"    Temperature: {config.LLM.DefaultParameters.Temperature}");
        yaml.AppendLine($"    TopP: {config.LLM.DefaultParameters.TopP}");
        yaml.AppendLine($"    FrequencyPenalty: {config.LLM.DefaultParameters.FrequencyPenalty}");
        yaml.AppendLine($"    PresencePenalty: {config.LLM.DefaultParameters.PresencePenalty}");
        yaml.AppendLine();
        
        // 간단한 YAML 생성 (실제로는 YamlDotNet 등을 사용하는 것이 좋음)
        // 여기서는 기본적인 구조만 생성
        
        return yaml.ToString();
    }
}