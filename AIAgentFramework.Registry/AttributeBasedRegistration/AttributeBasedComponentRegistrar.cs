using AIAgentFramework.Core.Attributes;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Registry.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AIAgentFramework.Registry.AttributeBasedRegistration;

/// <summary>
/// Attribute 기반 컴포넌트 등록기
/// </summary>
public class AttributeBasedComponentRegistrar : IAttributeBasedComponentRegistrar
{
    private readonly IAdvancedRegistry _registry;
    private readonly ILogger<AttributeBasedComponentRegistrar> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="registry">Registry</param>
    /// <param name="logger">로거</param>
    public AttributeBasedComponentRegistrar(
        IAdvancedRegistry registry,
        ILogger<AttributeBasedComponentRegistrar> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public int RegisterFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            _logger.LogInformation("Starting attribute-based registration from assembly: {AssemblyName}", 
                assembly.GetName().Name);

            // LLM 기능 등록
            registeredCount += RegisterLLMFunctionsFromAssembly(assembly);

            // Built-In 도구 등록
            registeredCount += RegisterBuiltInToolsFromAssembly(assembly);

            // 플러그인 도구 등록
            registeredCount += RegisterPluginToolsFromAssembly(assembly);

            // MCP 도구 등록
            registeredCount += RegisterMCPToolsFromAssembly(assembly);

            _logger.LogInformation("Attribute-based registration completed. Registered {Count} components from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register components from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int RegisterLLMFunctionsFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(ILLMFunction).IsAssignableFrom(t) &&
                           !t.IsInterface &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<LLMFunctionAttribute>() != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var attribute = type.GetCustomAttribute<LLMFunctionAttribute>();
                    if (attribute == null) continue;

                    var instance = CreateInstance<ILLMFunction>(type);
                    if (instance == null) continue;

                    var metadata = CreateLLMFunctionMetadata(type, attribute, instance);
                    var registrationId = _registry.RegisterLLMFunction(instance, metadata);

                    _logger.LogDebug("Registered LLM Function: {Name} (ID: {RegistrationId})", 
                        metadata.Name, registrationId);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register LLM Function: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Registered {Count} LLM Functions from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register LLM Functions from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int RegisterBuiltInToolsFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(ITool).IsAssignableFrom(t) &&
                           !t.IsInterface &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<BuiltInToolAttribute>() != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var attribute = type.GetCustomAttribute<BuiltInToolAttribute>();
                    if (attribute == null) continue;

                    var instance = CreateInstance<ITool>(type);
                    if (instance == null) continue;

                    var metadata = CreateBuiltInToolMetadata(type, attribute, instance);
                    var registrationId = _registry.RegisterTool(instance, metadata);

                    _logger.LogDebug("Registered Built-In Tool: {Name} (ID: {RegistrationId})", 
                        metadata.Name, registrationId);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register Built-In Tool: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Registered {Count} Built-In Tools from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Built-In Tools from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int RegisterPluginToolsFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(ITool).IsAssignableFrom(t) &&
                           !t.IsInterface &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<PluginToolAttribute>() != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var attribute = type.GetCustomAttribute<PluginToolAttribute>();
                    if (attribute == null) continue;

                    // 프레임워크 버전 호환성 확인
                    var frameworkVersion = GetFrameworkVersion();
                    if (!attribute.IsCompatibleWith(frameworkVersion))
                    {
                        _logger.LogWarning("Plugin Tool {TypeName} is not compatible with framework version {Version}", 
                            type.Name, frameworkVersion);
                        continue;
                    }

                    var instance = CreateInstance<ITool>(type);
                    if (instance == null) continue;

                    var metadata = CreatePluginToolMetadata(type, attribute, instance);
                    var registrationId = _registry.RegisterTool(instance, metadata);

                    _logger.LogDebug("Registered Plugin Tool: {Name} (ID: {RegistrationId})", 
                        metadata.Name, registrationId);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register Plugin Tool: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Registered {Count} Plugin Tools from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Plugin Tools from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int RegisterMCPToolsFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(ITool).IsAssignableFrom(t) &&
                           !t.IsInterface &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<MCPToolAttribute>() != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var attribute = type.GetCustomAttribute<MCPToolAttribute>();
                    if (attribute == null) continue;

                    var instance = CreateInstance<ITool>(type);
                    if (instance == null) continue;

                    var metadata = CreateMCPToolMetadata(type, attribute, instance);
                    var registrationId = _registry.RegisterTool(instance, metadata);

                    _logger.LogDebug("Registered MCP Tool: {Name} (ID: {RegistrationId})", 
                        metadata.Name, registrationId);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register MCP Tool: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Registered {Count} MCP Tools from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register MCP Tools from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public ComponentRegistrationInfo GetRegistrationInfo(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var info = new ComponentRegistrationInfo
        {
            Type = type,
            IsRegistrable = false
        };

        // LLM Function 확인
        var llmAttribute = type.GetCustomAttribute<LLMFunctionAttribute>();
        if (llmAttribute != null && typeof(ILLMFunction).IsAssignableFrom(type))
        {
            info.IsRegistrable = true;
            info.ComponentType = ComponentType.LLMFunction;
            info.Attribute = llmAttribute;
            info.Name = !string.IsNullOrEmpty(llmAttribute.Name) ? llmAttribute.Name : type.Name;
            info.IsEnabled = llmAttribute.IsEnabled;
            return info;
        }

        // Built-In Tool 확인
        var builtInAttribute = type.GetCustomAttribute<BuiltInToolAttribute>();
        if (builtInAttribute != null && typeof(ITool).IsAssignableFrom(type))
        {
            info.IsRegistrable = true;
            info.ComponentType = ComponentType.BuiltInTool;
            info.Attribute = builtInAttribute;
            info.Name = !string.IsNullOrEmpty(builtInAttribute.Name) ? builtInAttribute.Name : type.Name;
            info.IsEnabled = builtInAttribute.IsEnabled;
            return info;
        }

        // Plugin Tool 확인
        var pluginAttribute = type.GetCustomAttribute<PluginToolAttribute>();
        if (pluginAttribute != null && typeof(ITool).IsAssignableFrom(type))
        {
            info.IsRegistrable = true;
            info.ComponentType = ComponentType.PluginTool;
            info.Attribute = pluginAttribute;
            info.Name = !string.IsNullOrEmpty(pluginAttribute.Name) ? pluginAttribute.Name : type.Name;
            info.IsEnabled = pluginAttribute.IsEnabled;
            return info;
        }

        // MCP Tool 확인
        var mcpAttribute = type.GetCustomAttribute<MCPToolAttribute>();
        if (mcpAttribute != null && typeof(ITool).IsAssignableFrom(type))
        {
            info.IsRegistrable = true;
            info.ComponentType = ComponentType.MCPTool;
            info.Attribute = mcpAttribute;
            info.Name = !string.IsNullOrEmpty(mcpAttribute.Name) ? mcpAttribute.Name : type.Name;
            info.IsEnabled = mcpAttribute.IsEnabled;
            return info;
        }

        return info;
    }

    #region Private Helper Methods

    /// <summary>
    /// 인스턴스 생성
    /// </summary>
    private T? CreateInstance<T>(Type type) where T : class
    {
        try
        {
            // 기본 생성자 시도
            var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor != null)
            {
                return Activator.CreateInstance(type) as T;
            }

            // DI 생성자 시도 (향후 DI 컨테이너 통합 시 구현)
            _logger.LogWarning("Type {TypeName} does not have a parameterless constructor", type.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create instance of type: {TypeName}", type.Name);
            return null;
        }
    }

    /// <summary>
    /// LLM 기능 메타데이터 생성
    /// </summary>
    private LLMFunctionMetadata CreateLLMFunctionMetadata(Type type, LLMFunctionAttribute attribute, ILLMFunction instance)
    {
        return new LLMFunctionMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Role = !string.IsNullOrEmpty(attribute.Role) ? attribute.Role : ExtractRoleFromTypeName(type.Name),
            Description = !string.IsNullOrEmpty(attribute.Description) ? attribute.Description : $"LLM Function: {type.Name}",
            Version = attribute.Version,
            Author = attribute.Author,
            Category = attribute.Category,
            Tags = attribute.GetTags(),
            ComponentType = type,
            SupportedModels = attribute.GetSupportedModels(),
            RequiredParameters = attribute.GetRequiredParameters(),
            OptionalParameters = attribute.GetOptionalParameters(),
            ResponseSchema = !string.IsNullOrEmpty(attribute.ResponseSchema) ? attribute.ResponseSchema : null,
            Priority = attribute.Priority,
            IsEnabled = attribute.IsEnabled
        };
    }

    /// <summary>
    /// Built-In 도구 메타데이터 생성
    /// </summary>
    private ToolMetadata CreateBuiltInToolMetadata(Type type, BuiltInToolAttribute attribute, ITool instance)
    {
        return new ToolMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Description = !string.IsNullOrEmpty(attribute.Description) ? attribute.Description : $"Built-In Tool: {type.Name}",
            Version = attribute.Version,
            Author = attribute.Author,
            Category = attribute.Category,
            Tags = attribute.GetTags(),
            ComponentType = type,
            ToolType = ToolType.BuiltIn,
            InputSchema = !string.IsNullOrEmpty(attribute.InputSchema) ? attribute.InputSchema : null,
            OutputSchema = !string.IsNullOrEmpty(attribute.OutputSchema) ? attribute.OutputSchema : null,
            Dependencies = attribute.GetDependencies(),
            RequiredPermissions = attribute.GetRequiredPermissions(),
            TimeoutSeconds = attribute.TimeoutSeconds,
            IsCacheable = attribute.IsCacheable,
            CacheTTLMinutes = attribute.CacheTTLMinutes,
            IsEnabled = attribute.IsEnabled
        };
    }

    /// <summary>
    /// 플러그인 도구 메타데이터 생성
    /// </summary>
    private ToolMetadata CreatePluginToolMetadata(Type type, PluginToolAttribute attribute, ITool instance)
    {
        var metadata = new ToolMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Description = !string.IsNullOrEmpty(attribute.Description) ? attribute.Description : $"Plugin Tool: {type.Name}",
            Version = attribute.Version,
            Author = attribute.Author,
            Category = attribute.Category,
            Tags = attribute.GetTags(),
            ComponentType = type,
            ToolType = ToolType.Plugin,
            InputSchema = !string.IsNullOrEmpty(attribute.InputSchema) ? attribute.InputSchema : null,
            OutputSchema = !string.IsNullOrEmpty(attribute.OutputSchema) ? attribute.OutputSchema : null,
            Dependencies = attribute.GetDependencies(),
            RequiredPermissions = attribute.GetRequiredPermissions(),
            TimeoutSeconds = attribute.TimeoutSeconds,
            IsCacheable = attribute.IsCacheable,
            CacheTTLMinutes = attribute.CacheTTLMinutes,
            IsEnabled = attribute.IsEnabled
        };

        // 플러그인 특화 속성 추가
        metadata.Properties["PluginId"] = attribute.PluginId;
        metadata.Properties["PackageName"] = attribute.PackageName;
        metadata.Properties["License"] = attribute.License;
        metadata.Properties["Homepage"] = attribute.Homepage ?? string.Empty;
        metadata.Properties["Repository"] = attribute.Repository;
        metadata.Properties["MinFrameworkVersion"] = attribute.MinFrameworkVersion;
        metadata.Properties["MaxFrameworkVersion"] = attribute.MaxFrameworkVersion;
        metadata.Properties["RequiresSandbox"] = attribute.RequiresSandbox;
        metadata.Properties["RequiresNetworkAccess"] = attribute.RequiresNetworkAccess;
        metadata.Properties["RequiresFileSystemAccess"] = attribute.RequiresFileSystemAccess;

        return metadata;
    }

    /// <summary>
    /// MCP 도구 메타데이터 생성
    /// </summary>
    private ToolMetadata CreateMCPToolMetadata(Type type, MCPToolAttribute attribute, ITool instance)
    {
        var metadata = new ToolMetadata
        {
            Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : type.Name,
            Description = !string.IsNullOrEmpty(attribute.Description) ? attribute.Description : $"MCP Tool: {type.Name}",
            Version = attribute.Version,
            Author = attribute.Author,
            Category = attribute.Category,
            Tags = attribute.GetTags(),
            ComponentType = type,
            ToolType = ToolType.MCP,
            InputSchema = !string.IsNullOrEmpty(attribute.InputSchema) ? attribute.InputSchema : null,
            OutputSchema = !string.IsNullOrEmpty(attribute.OutputSchema) ? attribute.OutputSchema : null,
            RequiredPermissions = attribute.GetRequiredPermissions(),
            TimeoutSeconds = attribute.RequestTimeoutSeconds,
            IsCacheable = attribute.IsCacheable,
            CacheTTLMinutes = attribute.CacheTTLMinutes,
            IsEnabled = attribute.IsEnabled
        };

        // MCP 특화 속성 추가
        metadata.Properties["Endpoint"] = attribute.Endpoint;
        metadata.Properties["ProtocolVersion"] = attribute.ProtocolVersion;
        metadata.Properties["AuthTokenEnvVar"] = attribute.AuthTokenEnvVar;
        metadata.Properties["ConnectionTimeoutSeconds"] = attribute.ConnectionTimeoutSeconds;
        metadata.Properties["MaxRetries"] = attribute.MaxRetries;
        metadata.Properties["SupportedTransports"] = attribute.GetSupportedTransports();
        metadata.Properties["UseConnectionPooling"] = attribute.UseConnectionPooling;
        metadata.Properties["MaxConcurrentConnections"] = attribute.MaxConcurrentConnections;
        metadata.Properties["HeartbeatIntervalSeconds"] = attribute.HeartbeatIntervalSeconds;

        return metadata;
    }

    /// <summary>
    /// 타입 이름에서 역할 추출
    /// </summary>
    private string ExtractRoleFromTypeName(string typeName)
    {
        if (typeName.EndsWith("Function"))
        {
            return typeName.Substring(0, typeName.Length - 8).ToLowerInvariant();
        }
        return typeName.ToLowerInvariant();
    }

    /// <summary>
    /// 프레임워크 버전 가져오기
    /// </summary>
    private string GetFrameworkVersion()
    {
        // 실제 구현에서는 프레임워크 버전을 가져오는 로직 구현
        return "1.0.0";
    }

    #endregion
}