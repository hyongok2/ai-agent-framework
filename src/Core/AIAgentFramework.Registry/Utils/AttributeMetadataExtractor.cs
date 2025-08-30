using AIAgentFramework.Core.Attributes;
using AIAgentFramework.Registry.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AIAgentFramework.Registry.Utils;

/// <summary>
/// Attribute에서 메타데이터를 추출하는 유틸리티
/// </summary>
public class AttributeMetadataExtractor : IAttributeMetadataExtractor
{
    private readonly ILogger<AttributeMetadataExtractor> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public AttributeMetadataExtractor(ILogger<AttributeMetadataExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ComponentMetadata? ExtractMetadata(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        try
        {
            // LLM Function Attribute 확인
            var llmAttribute = type.GetCustomAttribute<LLMFunctionAttribute>();
            if (llmAttribute != null)
            {
                return ExtractLLMFunctionMetadata(type, llmAttribute);
            }

            // Built-in Tool Attribute 확인
            var builtInAttribute = type.GetCustomAttribute<BuiltInToolAttribute>();
            if (builtInAttribute != null)
            {
                return ExtractBuiltInToolMetadata(type, builtInAttribute);
            }

            // Plugin Tool Attribute 확인
            var pluginAttribute = type.GetCustomAttribute<PluginToolAttribute>();
            if (pluginAttribute != null)
            {
                return ExtractPluginToolMetadata(type, pluginAttribute);
            }

            // MCP Tool Attribute 확인
            var mcpAttribute = type.GetCustomAttribute<MCPToolAttribute>();
            if (mcpAttribute != null)
            {
                return ExtractMCPToolMetadata(type, mcpAttribute);
            }

            _logger.LogDebug("No recognized attributes found on type: {TypeName}", type.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from type: {TypeName}", type.Name);
            return null;
        }
    }

    /// <inheritdoc />
    public bool HasValidAttributes(Type type)
    {
        if (type == null)
            return false;

        return type.GetCustomAttribute<LLMFunctionAttribute>() != null ||
               type.GetCustomAttribute<BuiltInToolAttribute>() != null ||
               type.GetCustomAttribute<PluginToolAttribute>() != null ||
               type.GetCustomAttribute<MCPToolAttribute>() != null;
    }

    /// <inheritdoc />
    public List<Type> FilterTypesWithAttributes(IEnumerable<Type> types)
    {
        if (types == null)
            throw new ArgumentNullException(nameof(types));

        var filteredTypes = new List<Type>();

        foreach (var type in types)
        {
            if (HasValidAttributes(type))
            {
                filteredTypes.Add(type);
                _logger.LogDebug("Type {TypeName} has valid attributes", type.Name);
            }
        }

        return filteredTypes;
    }

    /// <inheritdoc />
    public Dictionary<Type, ComponentMetadata> ExtractAllMetadata(IEnumerable<Type> types)
    {
        if (types == null)
            throw new ArgumentNullException(nameof(types));

        var metadataMap = new Dictionary<Type, ComponentMetadata>();

        foreach (var type in types)
        {
            var metadata = ExtractMetadata(type);
            if (metadata != null)
            {
                metadataMap[type] = metadata;
            }
        }

        _logger.LogInformation("Extracted metadata for {Count} types", metadataMap.Count);
        return metadataMap;
    }

    /// <summary>
    /// LLM Function 메타데이터 추출
    /// </summary>
    private LLMFunctionMetadata ExtractLLMFunctionMetadata(Type type, LLMFunctionAttribute attribute)
    {
        var metadata = new LLMFunctionMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Role = attribute.Role,
            Description = attribute.Description,
            Category = attribute.Category,
            Priority = attribute.Priority,
            ResponseSchema = attribute.ResponseSchema,
            Author = attribute.Author,
            Version = attribute.Version,
            IsEnabled = attribute.IsEnabled,
            ComponentType = type,
            RegisteredAt = DateTime.UtcNow
        };

        // 지원 모델 파싱
        if (!string.IsNullOrEmpty(attribute.SupportedModels))
        {
            metadata.SupportedModels = ParseCommaSeparatedString(attribute.SupportedModels);
        }

        // 필수 파라미터 파싱
        if (!string.IsNullOrEmpty(attribute.RequiredParameters))
        {
            metadata.RequiredParameters = ParseCommaSeparatedString(attribute.RequiredParameters);
        }

        // 선택적 파라미터 파싱
        if (!string.IsNullOrEmpty(attribute.OptionalParameters))
        {
            metadata.OptionalParameters = ParseCommaSeparatedString(attribute.OptionalParameters);
        }

        // 태그 파싱
        if (!string.IsNullOrEmpty(attribute.Tags))
        {
            metadata.Tags = ParseCommaSeparatedString(attribute.Tags);
        }

        _logger.LogDebug("Extracted LLM Function metadata: {Name} (Role: {Role})", metadata.Name, metadata.Role);
        return metadata;
    }

    /// <summary>
    /// Built-in Tool 메타데이터 추출
    /// </summary>
    private ToolMetadata ExtractBuiltInToolMetadata(Type type, BuiltInToolAttribute attribute)
    {
        var metadata = new ToolMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Description = attribute.Description,
            Category = attribute.Category,
            ToolType = ToolType.BuiltIn,
            InputSchema = attribute.InputSchema,
            OutputSchema = attribute.OutputSchema,
            TimeoutSeconds = attribute.TimeoutSeconds,
            IsCacheable = attribute.IsCacheable,
            CacheTTLMinutes = attribute.CacheTTLMinutes,
            Author = attribute.Author,
            Version = attribute.Version,
            IsEnabled = attribute.IsEnabled,
            ComponentType = type,
            RegisteredAt = DateTime.UtcNow
        };

        // 의존성 파싱
        if (!string.IsNullOrEmpty(attribute.Dependencies))
        {
            metadata.Dependencies = ParseCommaSeparatedString(attribute.Dependencies);
        }

        // 권한 파싱
        if (!string.IsNullOrEmpty(attribute.RequiredPermissions))
        {
            metadata.RequiredPermissions = ParseCommaSeparatedString(attribute.RequiredPermissions);
        }

        // 태그 파싱
        if (!string.IsNullOrEmpty(attribute.Tags))
        {
            metadata.Tags = ParseCommaSeparatedString(attribute.Tags);
        }

        _logger.LogDebug("Extracted Built-in Tool metadata: {Name}", metadata.Name);
        return metadata;
    }

    /// <summary>
    /// Plugin Tool 메타데이터 추출
    /// </summary>
    private ToolMetadata ExtractPluginToolMetadata(Type type, PluginToolAttribute attribute)
    {
        var metadata = new ToolMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Description = attribute.Description,
            Category = attribute.Category,
            ToolType = ToolType.Plugin,
            InputSchema = attribute.InputSchema,
            OutputSchema = attribute.OutputSchema,
            TimeoutSeconds = attribute.TimeoutSeconds,
            IsCacheable = attribute.IsCacheable,
            CacheTTLMinutes = attribute.CacheTTLMinutes,
            Author = attribute.Author,
            Version = attribute.Version,
            IsEnabled = attribute.IsEnabled,
            ComponentType = type,
            RegisteredAt = DateTime.UtcNow
        };

        // 플러그인 특화 속성 추가
        metadata.Properties["PluginName"] = attribute.PluginName;
        metadata.Properties["PluginVersion"] = attribute.PluginVersion;
        metadata.Properties["MinFrameworkVersion"] = attribute.MinFrameworkVersion;
        metadata.Properties["License"] = attribute.License;
        
        if (!string.IsNullOrEmpty(attribute.Homepage))
        {
            metadata.Properties["Homepage"] = attribute.Homepage;
        }

        // 의존성 파싱
        if (!string.IsNullOrEmpty(attribute.Dependencies))
        {
            metadata.Dependencies = ParseCommaSeparatedString(attribute.Dependencies);
        }

        // 권한 파싱
        if (!string.IsNullOrEmpty(attribute.RequiredPermissions))
        {
            metadata.RequiredPermissions = ParseCommaSeparatedString(attribute.RequiredPermissions);
        }

        // 태그 파싱
        if (!string.IsNullOrEmpty(attribute.Tags))
        {
            metadata.Tags = ParseCommaSeparatedString(attribute.Tags);
        }

        _logger.LogDebug("Extracted Plugin Tool metadata: {Name} (Plugin: {PluginName})", 
            metadata.Name, attribute.PluginName);
        return metadata;
    }

    /// <summary>
    /// MCP Tool 메타데이터 추출
    /// </summary>
    private ToolMetadata ExtractMCPToolMetadata(Type type, MCPToolAttribute attribute)
    {
        var metadata = new ToolMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Description = attribute.Description,
            Category = attribute.Category,
            ToolType = ToolType.MCP,
            InputSchema = attribute.InputSchema,
            OutputSchema = attribute.OutputSchema,
            TimeoutSeconds = attribute.ExecutionTimeoutSeconds,
            IsCacheable = attribute.IsCacheable,
            CacheTTLMinutes = attribute.CacheTTLMinutes,
            Author = attribute.Author,
            Version = attribute.Version,
            IsEnabled = attribute.IsEnabled,
            ComponentType = type,
            RegisteredAt = DateTime.UtcNow
        };

        // MCP 특화 속성 추가
        metadata.Properties["Endpoint"] = attribute.Endpoint;
        metadata.Properties["ServerName"] = attribute.ServerName;
        metadata.Properties["ConnectionTimeoutSeconds"] = attribute.ConnectionTimeoutSeconds;
        metadata.Properties["MaxRetries"] = attribute.MaxRetries;
        metadata.Properties["RequiresAuth"] = attribute.RequiresAuth;
        metadata.Properties["MCPVersion"] = attribute.MCPVersion;
        metadata.Properties["UseSSL"] = attribute.UseSSL;

        // 태그 파싱
        if (!string.IsNullOrEmpty(attribute.Tags))
        {
            metadata.Tags = ParseCommaSeparatedString(attribute.Tags);
        }

        _logger.LogDebug("Extracted MCP Tool metadata: {Name} (Endpoint: {Endpoint})", 
            metadata.Name, attribute.Endpoint);
        return metadata;
    }

    /// <summary>
    /// 쉼표로 구분된 문자열을 파싱
    /// </summary>
    private List<string> ParseCommaSeparatedString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToList();
    }
}

/// <summary>
/// Attribute 메타데이터 추출기 인터페이스
/// </summary>
public interface IAttributeMetadataExtractor
{
    /// <summary>
    /// 타입에서 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="type">타입</param>
    /// <returns>메타데이터 (없으면 null)</returns>
    ComponentMetadata? ExtractMetadata(Type type);
    
    /// <summary>
    /// 타입이 유효한 Attribute를 가지고 있는지 확인합니다.
    /// </summary>
    /// <param name="type">타입</param>
    /// <returns>유효한 Attribute 보유 여부</returns>
    bool HasValidAttributes(Type type);
    
    /// <summary>
    /// 타입 목록에서 유효한 Attribute를 가진 타입들만 필터링합니다.
    /// </summary>
    /// <param name="types">타입 목록</param>
    /// <returns>필터링된 타입 목록</returns>
    List<Type> FilterTypesWithAttributes(IEnumerable<Type> types);
    
    /// <summary>
    /// 여러 타입에서 메타데이터를 일괄 추출합니다.
    /// </summary>
    /// <param name="types">타입 목록</param>
    /// <returns>타입별 메타데이터 맵</returns>
    Dictionary<Type, ComponentMetadata> ExtractAllMetadata(IEnumerable<Type> types);
}