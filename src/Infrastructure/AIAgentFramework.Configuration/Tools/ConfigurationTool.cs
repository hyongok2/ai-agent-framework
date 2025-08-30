using AIAgentFramework.Configuration.Models;
using AIAgentFramework.Configuration.Templates;
using AIAgentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Configuration.Tools;

/// <summary>
/// 설정 관리 도구
/// </summary>
public class ConfigurationTool : IConfigurationTool
{
    private readonly IConfigurationTemplateGenerator _templateGenerator;
    private readonly IConfigurationValidator _validator;
    private readonly IAdvancedConfigurationValidator _advancedValidator;
    private readonly ILogger<ConfigurationTool> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="templateGenerator">템플릿 생성기</param>
    /// <param name="validator">기본 검증기</param>
    /// <param name="advancedValidator">고급 검증기</param>
    /// <param name="logger">로거</param>
    public ConfigurationTool(
        IConfigurationTemplateGenerator templateGenerator,
        IConfigurationValidator validator,
        IAdvancedConfigurationValidator advancedValidator,
        ILogger<ConfigurationTool> logger)
    {
        _templateGenerator = templateGenerator ?? throw new ArgumentNullException(nameof(templateGenerator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _advancedValidator = advancedValidator ?? throw new ArgumentNullException(nameof(advancedValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void GenerateConfigurationFiles(string outputDirectory, string[]? environments = null)
    {
        environments ??= new[] { "Development", "Testing", "Production" };
        
        try
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // 기본 설정 파일 생성
            var baseTemplate = _templateGenerator.GenerateYamlTemplate("Development");
            var baseFilePath = Path.Combine(outputDirectory, "config.yaml");
            _templateGenerator.SaveTemplate(baseFilePath, baseTemplate);

            // 환경별 설정 파일 생성
            foreach (var environment in environments)
            {
                var template = _templateGenerator.GenerateYamlTemplate(environment);
                var fileName = $"config.{environment.ToLowerInvariant()}.yaml";
                var filePath = Path.Combine(outputDirectory, fileName);
                _templateGenerator.SaveTemplate(filePath, template);
            }

            // JSON Schema 파일 복사
            var schemaSourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "aiagent-config-schema.json");
            var schemaTargetPath = Path.Combine(outputDirectory, "aiagent-config-schema.json");
            
            if (File.Exists(schemaSourcePath))
            {
                File.Copy(schemaSourcePath, schemaTargetPath, true);
            }

            _logger.LogInformation("Configuration files generated successfully in: {OutputDirectory}", outputDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate configuration files");
            throw;
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfigurationFile(string filePath, bool includeAdvancedValidation = true)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var result = new ValidationResult();
                result.Errors.Add($"Configuration file not found: {filePath}");
                result.IsValid = false;
                return result;
            }

            // 파일 내용 로드 및 파싱
            var content = File.ReadAllText(filePath);
            var config = ParseConfigurationFile(content, Path.GetExtension(filePath));

            if (config == null)
            {
                var result = new ValidationResult();
                result.Errors.Add("Failed to parse configuration file");
                result.IsValid = false;
                return result;
            }

            // 기본 검증
            var validationResult = _validator.ValidateConfiguration(config);

            if (includeAdvancedValidation)
            {
                // 고급 검증 수행
                var envValidation = _advancedValidator.ValidateEnvironmentSpecific(config);
                var perfValidation = _advancedValidator.ValidatePerformanceSettings(config);
                var secValidation = _advancedValidator.ValidateSecuritySettings(config);

                // 결과 병합
                validationResult.Errors.AddRange(envValidation.Errors);
                validationResult.Errors.AddRange(perfValidation.Errors);
                validationResult.Errors.AddRange(secValidation.Errors);

                validationResult.Warnings.AddRange(envValidation.Warnings);
                validationResult.Warnings.AddRange(perfValidation.Warnings);
                validationResult.Warnings.AddRange(secValidation.Warnings);

                validationResult.IsValid = validationResult.IsValid && 
                                         envValidation.IsValid && 
                                         perfValidation.IsValid && 
                                         secValidation.IsValid;
            }

            _logger.LogInformation("Configuration validation completed for: {FilePath}", filePath);
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed for: {FilePath}", filePath);
            var result = new ValidationResult();
            result.Errors.Add($"Validation exception: {ex.Message}");
            result.IsValid = false;
            return result;
        }
    }

    /// <inheritdoc />
    public void MigrateConfiguration(string sourceFilePath, string targetFilePath, string targetVersion = "1.0.0")
    {
        try
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException($"Source configuration file not found: {sourceFilePath}");
            }

            var sourceContent = File.ReadAllText(sourceFilePath);
            var sourceConfig = ParseConfigurationFile(sourceContent, Path.GetExtension(sourceFilePath));

            if (sourceConfig == null)
            {
                throw new InvalidOperationException("Failed to parse source configuration file");
            }

            // 버전별 마이그레이션 로직
            var migratedConfig = PerformMigration(sourceConfig, targetVersion);

            // 대상 파일 형식에 따라 저장
            var targetExtension = Path.GetExtension(targetFilePath).ToLowerInvariant();
            string targetContent;

            if (targetExtension == ".json")
            {
                targetContent = _templateGenerator.GenerateJsonTemplate(migratedConfig.Application.Environment);
            }
            else
            {
                targetContent = _templateGenerator.GenerateYamlTemplate(migratedConfig.Application.Environment);
            }

            _templateGenerator.SaveTemplate(targetFilePath, targetContent);

            _logger.LogInformation("Configuration migrated from {SourcePath} to {TargetPath}", sourceFilePath, targetFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration migration failed");
            throw;
        }
    }

    /// <inheritdoc />
    public ConfigurationComparisonResult CompareConfigurations(string filePath1, string filePath2)
    {
        try
        {
            var config1 = LoadConfigurationFromFile(filePath1);
            var config2 = LoadConfigurationFromFile(filePath2);

            var result = new ConfigurationComparisonResult
            {
                File1Path = filePath1,
                File2Path = filePath2,
                Differences = new List<ConfigurationDifference>()
            };

            // 설정 비교 로직
            CompareApplicationSettings(config1.Application, config2.Application, result);
            CompareLLMSettings(config1.LLM, config2.LLM, result);
            CompareToolSettings(config1.Tools, config2.Tools, result);
            ComparePromptSettings(config1.Prompts, config2.Prompts, result);
            CompareUISettings(config1.UI, config2.UI, result);
            CompareOrchestrationSettings(config1.Orchestration, config2.Orchestration, result);
            CompareLoggingSettings(config1.Logging, config2.Logging, result);
            CompareSecuritySettings(config1.Security, config2.Security, result);

            result.HasDifferences = result.Differences.Any();

            _logger.LogInformation("Configuration comparison completed between {File1} and {File2}", filePath1, filePath2);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration comparison failed");
            throw;
        }
    }

    /// <inheritdoc />
    public void ExportConfigurationDocumentation(AIAgentConfiguration configuration, string outputPath, string format = "markdown")
    {
        try
        {
            string content;

            switch (format.ToLowerInvariant())
            {
                case "markdown":
                case "md":
                    content = GenerateMarkdownDocumentation(configuration);
                    break;
                case "html":
                    content = GenerateHtmlDocumentation(configuration);
                    break;
                case "json":
                    content = _templateGenerator.GenerateJsonTemplate(configuration.Application.Environment);
                    break;
                default:
                    throw new ArgumentException($"Unsupported documentation format: {format}");
            }

            File.WriteAllText(outputPath, content, System.Text.Encoding.UTF8);

            _logger.LogInformation("Configuration documentation exported to: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration documentation");
            throw;
        }
    }

    /// <summary>
    /// 설정 파일 파싱
    /// </summary>
    private AIAgentConfiguration? ParseConfigurationFile(string content, string extension)
    {
        // 실제 구현에서는 YAML/JSON 파서를 사용
        // 여기서는 기본 구현만 제공
        return new AIAgentConfiguration();
    }

    /// <summary>
    /// 파일에서 설정 로드
    /// </summary>
    private AIAgentConfiguration LoadConfigurationFromFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var extension = Path.GetExtension(filePath);
        return ParseConfigurationFile(content, extension) ?? new AIAgentConfiguration();
    }

    /// <summary>
    /// 설정 마이그레이션 수행
    /// </summary>
    private AIAgentConfiguration PerformMigration(AIAgentConfiguration sourceConfig, string targetVersion)
    {
        // 버전별 마이그레이션 로직 구현
        var migratedConfig = sourceConfig;
        migratedConfig.Application.Version = targetVersion;
        return migratedConfig;
    }

    /// <summary>
    /// 애플리케이션 설정 비교
    /// </summary>
    private void CompareApplicationSettings(ApplicationInfo app1, ApplicationInfo app2, ConfigurationComparisonResult result)
    {
        if (app1.Name != app2.Name)
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Section = "Application",
                Property = "Name",
                Value1 = app1.Name,
                Value2 = app2.Name,
                DifferenceType = "Value"
            });
        }

        if (app1.Version != app2.Version)
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Section = "Application",
                Property = "Version",
                Value1 = app1.Version,
                Value2 = app2.Version,
                DifferenceType = "Value"
            });
        }

        if (app1.Environment != app2.Environment)
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Section = "Application",
                Property = "Environment",
                Value1 = app1.Environment,
                Value2 = app2.Environment,
                DifferenceType = "Value"
            });
        }
    }

    /// <summary>
    /// LLM 설정 비교
    /// </summary>
    private void CompareLLMSettings(LLMConfiguration llm1, LLMConfiguration llm2, ConfigurationComparisonResult result)
    {
        if (llm1.DefaultProvider != llm2.DefaultProvider)
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Section = "LLM",
                Property = "DefaultProvider",
                Value1 = llm1.DefaultProvider,
                Value2 = llm2.DefaultProvider,
                DifferenceType = "Value"
            });
        }

        // 모델 매핑 비교
        var allModelKeys = llm1.Models.Keys.Union(llm2.Models.Keys).ToList();
        foreach (var key in allModelKeys)
        {
            var value1 = llm1.Models.GetValueOrDefault(key, "");
            var value2 = llm2.Models.GetValueOrDefault(key, "");
            
            if (value1 != value2)
            {
                result.Differences.Add(new ConfigurationDifference
                {
                    Section = "LLM.Models",
                    Property = key,
                    Value1 = value1,
                    Value2 = value2,
                    DifferenceType = string.IsNullOrEmpty(value1) ? "Added" : string.IsNullOrEmpty(value2) ? "Removed" : "Value"
                });
            }
        }
    }

    // 다른 설정 섹션들의 비교 메서드들도 유사하게 구현...
    private void CompareToolSettings(ToolConfiguration tools1, ToolConfiguration tools2, ConfigurationComparisonResult result) { }
    private void ComparePromptSettings(PromptConfiguration prompts1, PromptConfiguration prompts2, ConfigurationComparisonResult result) { }
    private void CompareUISettings(UIConfiguration ui1, UIConfiguration ui2, ConfigurationComparisonResult result) { }
    private void CompareOrchestrationSettings(OrchestrationConfiguration orch1, OrchestrationConfiguration orch2, ConfigurationComparisonResult result) { }
    private void CompareLoggingSettings(LoggingConfiguration log1, LoggingConfiguration log2, ConfigurationComparisonResult result) { }
    private void CompareSecuritySettings(SecurityConfiguration sec1, SecurityConfiguration sec2, ConfigurationComparisonResult result) { }

    /// <summary>
    /// Markdown 문서 생성
    /// </summary>
    private string GenerateMarkdownDocumentation(AIAgentConfiguration config)
    {
        var md = new System.Text.StringBuilder();
        
        md.AppendLine("# AI Agent Framework Configuration Documentation");
        md.AppendLine();
        md.AppendLine($"**Environment:** {config.Application.Environment}");
        md.AppendLine($"**Version:** {config.Application.Version}");
        md.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        md.AppendLine();
        
        md.AppendLine("## Application Settings");
        md.AppendLine($"- **Name:** {config.Application.Name}");
        md.AppendLine($"- **Instance ID:** {config.Application.InstanceId}");
        md.AppendLine();
        
        md.AppendLine("## LLM Configuration");
        md.AppendLine($"- **Default Provider:** {config.LLM.DefaultProvider}");
        md.AppendLine("- **Model Mappings:**");
        foreach (var model in config.LLM.Models)
        {
            md.AppendLine($"  - {model.Key}: {model.Value}");
        }
        md.AppendLine();
        
        // 다른 섹션들도 유사하게 추가...
        
        return md.ToString();
    }

    /// <summary>
    /// HTML 문서 생성
    /// </summary>
    private string GenerateHtmlDocumentation(AIAgentConfiguration config)
    {
        // HTML 문서 생성 로직
        return $"<html><head><title>AI Agent Configuration</title></head><body><h1>{config.Application.Name}</h1></body></html>";
    }
}

/// <summary>
/// 설정 비교 결과
/// </summary>
public class ConfigurationComparisonResult
{
    public string File1Path { get; set; } = string.Empty;
    public string File2Path { get; set; } = string.Empty;
    public bool HasDifferences { get; set; }
    public List<ConfigurationDifference> Differences { get; set; } = new();
}

/// <summary>
/// 설정 차이점
/// </summary>
public class ConfigurationDifference
{
    public string Section { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
    public string DifferenceType { get; set; } = string.Empty; // Added, Removed, Value
}