# AI Agent Framework - Configuration Guide

## Overview

The AI Agent Framework provides a comprehensive configuration management system that supports multiple environments, validation, and flexible deployment scenarios.

## Configuration Structure

### Main Configuration Sections

1. **Application**: Basic application information
2. **LLM**: Language model providers and settings
3. **Tools**: Built-in tools, plugins, and MCP endpoints
4. **Prompts**: Prompt templates and caching
5. **UI**: User interface settings (Web, Console, API)
6. **Orchestration**: Execution flow and session management
7. **Logging**: Logging configuration and retention
8. **Security**: Security policies and restrictions

## Environment-Specific Configuration

The framework supports multiple environments with automatic configuration overrides:

- **Development**: Debug logging, relaxed security, local endpoints
- **Testing**: Mock providers, minimal caching, isolated execution
- **Staging**: Production-like settings with debug capabilities
- **Production**: Optimized performance, enhanced security, audit logging

## Configuration Files

### Base Configuration
```yaml
# config.yaml - Base configuration template
Application:
  Name: "AI Agent Framework"
  Version: "1.0.0"
  Environment: "Development"

LLM:
  DefaultProvider: "openai"
  Models:
    planner: "gpt-4"
    interpreter: "gpt-3.5-turbo"
    # ... other model mappings
  Providers:
    openai:
      ApiKey: "${OPENAI_API_KEY}"
      MaxRetries: 3
      TimeoutSeconds: 30
```

### Environment Overrides
```yaml
# config.production.yaml - Production overrides
Application:
  Environment: "Production"

LLM:
  Providers:
    openai:
      MaxRetries: 5
      TimeoutSeconds: 60

Security:
  EncryptApiKeys: true
  RequireHttps: true
  AuditLogging: true
```

## Configuration Validation

### Basic Validation
- Required field validation
- Type checking
- Format validation (URLs, IP addresses, etc.)
- Range validation for numeric values

### Advanced Validation
- Environment-specific rules
- Security policy enforcement
- Performance setting optimization
- Connectivity testing

### JSON Schema Support
The framework includes a comprehensive JSON Schema for configuration validation:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "AI Agent Framework Configuration Schema",
  "type": "object",
  "properties": {
    "Application": { ... },
    "LLM": { ... },
    // ... other sections
  }
}
```

## Configuration Management Tools

### Template Generation
```csharp
// Generate configuration templates
var generator = serviceProvider.GetService<IConfigurationTemplateGenerator>();
var yamlTemplate = generator.GenerateYamlTemplate("Production");
var jsonTemplate = generator.GenerateJsonTemplate("Development");
```

### Validation
```csharp
// Validate configuration
var validator = serviceProvider.GetService<IConfigurationValidator>();
var result = validator.ValidateConfiguration(configuration);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Advanced Validation
```csharp
// Advanced validation with environment-specific rules
var advancedValidator = serviceProvider.GetService<IAdvancedConfigurationValidator>();
var envResult = advancedValidator.ValidateEnvironmentSpecific(configuration);
var secResult = advancedValidator.ValidateSecuritySettings(configuration);
var perfResult = advancedValidator.ValidatePerformanceSettings(configuration);
```

## Configuration Loading

### Dependency Injection Setup
```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: true)
    .AddEnvironmentYamlFile("config", builder.Environment.EnvironmentName)
    .AddEnvironmentVariables();

// Register configuration services
builder.Services.AddAIAgentConfiguration(builder.Configuration);

// Validate configuration at startup
builder.Services.ValidateAIAgentConfiguration();

var app = builder.Build();
```

### Runtime Configuration Access
```csharp
public class MyService
{
    private readonly IAIAgentConfigurationManager _configManager;
    
    public MyService(IAIAgentConfigurationManager configManager)
    {
        _configManager = configManager;
    }
    
    public void DoSomething()
    {
        var config = _configManager.GetConfiguration();
        var llmConfig = _configManager.GetSection<LLMConfiguration>("LLM");
        
        // Use configuration...
    }
}
```

## Environment Variables

The framework supports environment variable substitution in configuration files:

```yaml
LLM:
  Providers:
    openai:
      ApiKey: "${OPENAI_API_KEY}"           # Required
      OrganizationId: "${OPENAI_ORG_ID}"    # Optional
    claude:
      ApiKey: "${ANTHROPIC_API_KEY}"        # Required

ConnectionStrings:
  DefaultConnection: "${DATABASE_CONNECTION_STRING}"
  VectorDB: "${VECTOR_DB_CONNECTION_STRING}"
  Redis: "${REDIS_CONNECTION_STRING}"
```

## Security Considerations

### API Key Management
- Use environment variables for sensitive data
- Enable API key encryption in production
- Implement key rotation policies
- Monitor API key usage

### Network Security
- Configure allowed IP ranges
- Use HTTPS in production
- Implement proper CORS policies
- Enable audit logging

### Data Protection
- Enable sensitive data masking
- Configure appropriate log levels
- Implement data retention policies
- Use secure file permissions

## Performance Optimization

### Caching Configuration
```yaml
Tools:
  Cache:
    Enabled: true
    DefaultTTLMinutes: 30
    MaxSizeMB: 100
    ToolSpecificTTL:
      web_search: 15
      database_query: 60

Prompts:
  CacheEnabled: true
  CacheTTLMinutes: 60
```

### Concurrency Settings
```yaml
Orchestration:
  MaxConcurrentSessions: 100
  MaxExecutionSteps: 50
  ExecutionTimeoutMinutes: 10

Tools:
  MaxConcurrentExecutions: 5
  ExecutionTimeoutSeconds: 60
```

## Troubleshooting

### Common Issues

1. **Configuration Not Loading**
   - Check file paths and permissions
   - Verify YAML/JSON syntax
   - Ensure environment variables are set

2. **Validation Failures**
   - Review validation error messages
   - Check required fields
   - Verify data types and formats

3. **Performance Issues**
   - Review timeout settings
   - Check concurrency limits
   - Monitor cache usage

### Debug Configuration
```yaml
Logging:
  LogLevel: "Debug"
  StructuredLogging: true
  PerformanceLogging: true

UI:
  Common:
    DebugMode: true
```

## Migration Guide

When upgrading to new versions, use the configuration migration tool:

```csharp
var tool = serviceProvider.GetService<IConfigurationTool>();
tool.MigrateConfiguration(
    "old-config.yaml", 
    "new-config.yaml", 
    "2.0.0"
);
```

## Best Practices

1. **Version Control**: Store configuration templates in version control
2. **Environment Separation**: Use separate configuration files per environment
3. **Secret Management**: Never commit secrets to version control
4. **Validation**: Always validate configuration before deployment
5. **Documentation**: Document custom configuration options
6. **Monitoring**: Monitor configuration changes and their impact
7. **Backup**: Maintain backups of working configurations
8. **Testing**: Test configuration changes in non-production environments

## Examples

See the `config.yaml`, `config.development.yaml`, `config.testing.yaml`, and `config.production.yaml` files for complete configuration examples.