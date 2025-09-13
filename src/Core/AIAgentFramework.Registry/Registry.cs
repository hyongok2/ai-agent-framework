
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Attributes;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Attributes;
using AIAgentFramework.Registry.Models;
using AIAgentFramework.Registry.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AIAgentFramework.Registry;

/// <summary>
/// 컴포넌트 레지스트리 구현
/// </summary>
public class Registry : IAdvancedRegistry
{
    private readonly ConcurrentDictionary<string, ComponentRegistration> _registrations;
    private readonly ConcurrentDictionary<string, ILLMFunction> _llmFunctions;
    private readonly ConcurrentDictionary<string, ITool> _tools;
    private readonly ILogger<Registry> _logger;
    private readonly object _lockObject = new();

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public Registry(ILogger<Registry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registrations = new ConcurrentDictionary<string, ComponentRegistration>();
        _llmFunctions = new ConcurrentDictionary<string, ILLMFunction>();
        _tools = new ConcurrentDictionary<string, ITool>();
    }

    #region IRegistry 구현

    /// <inheritdoc />
    public void RegisterLLMFunction(ILLMFunction function)
    {
        if (function == null)
            throw new ArgumentNullException(nameof(function));

        var metadata = CreateDefaultLLMFunctionMetadata(function);
        RegisterLLMFunction(function, metadata);
    }

    /// <inheritdoc />
    public void RegisterTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        var metadata = CreateDefaultToolMetadata(tool);
        RegisterTool(tool, metadata);
    }

    /// <inheritdoc />
    public ILLMFunction? GetLLMFunction(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        _llmFunctions.TryGetValue(role, out var function);
        
        if (function != null)
        {
            UpdateUsageStatistics(role);
        }

        return function;
    }

    /// <inheritdoc />
    public ITool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        _tools.TryGetValue(name, out var tool);
        
        if (tool != null)
        {
            UpdateUsageStatistics(name);
        }

        return tool;
    }

    /// <inheritdoc />
    public List<ILLMFunction> GetAllLLMFunctions()
    {
        return _llmFunctions.Values
            .Where(f => IsComponentEnabled(GetComponentName(f)))
            .ToList();
    }

    /// <inheritdoc />
    public List<ITool> GetAllTools()
    {
        return _tools.Values
            .Where(t => IsComponentEnabled(GetComponentName(t)))
            .ToList();
    }

    /// <inheritdoc />
    public void AutoRegisterFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            // LLM 기능 자동 등록
            registeredCount += AutoRegisterFromAssembly<ILLMFunction>(assembly);
            
            // 도구 자동 등록
            registeredCount += AutoRegisterFromAssembly<ITool>(assembly);

            _logger.LogInformation("Auto-registered {Count} components from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-register from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }
    }

    #endregion

    #region IAdvancedRegistry 구현

    /// <inheritdoc />
    public string RegisterLLMFunction(ILLMFunction function, LLMFunctionMetadata metadata)
    {
        if (function == null)
            throw new ArgumentNullException(nameof(function));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        lock (_lockObject)
        {
            var registration = new ComponentRegistration
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = metadata,
                Instance = function,
                RegistrationSource = "Manual"
            };

            _registrations[registration.Id] = registration;
            _llmFunctions[metadata.Name] = function;

            // Role로도 조회 가능하도록 이중 등록 (Name과 Role이 다른 경우)
            if (!string.IsNullOrEmpty(metadata.Role) && metadata.Role != metadata.Name)
            {
                _llmFunctions[metadata.Role] = function;
            }

            _logger.LogInformation("LLM Function registered: {Name} (Role: {Role})",
                metadata.Name, metadata.Role);

            return registration.Id;
        }
    }

    /// <inheritdoc />
    public string RegisterTool(ITool tool, ToolMetadata metadata)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        lock (_lockObject)
        {
            var registration = new ComponentRegistration
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = metadata,
                Instance = tool,
                RegistrationSource = "Manual"
            };

            _registrations[registration.Id] = registration;
            _tools[metadata.Name] = tool;

            _logger.LogInformation("Tool registered: {Name} (Type: {ToolType})", 
                metadata.Name, metadata.ToolType);

            return registration.Id;
        }
    }

    /// <inheritdoc />
    public bool UnregisterComponent(string registrationId)
    {
        if (string.IsNullOrWhiteSpace(registrationId))
            return false;

        lock (_lockObject)
        {
            if (!_registrations.TryRemove(registrationId, out var registration))
                return false;

            // 해당 컴포넌트를 관련 컬렉션에서도 제거
            if (registration.Instance is ILLMFunction)
            {
                _llmFunctions.TryRemove(registration.Metadata.Name, out _);
            }
            else if (registration.Instance is ITool)
            {
                _tools.TryRemove(registration.Metadata.Name, out _);
            }

            _logger.LogInformation("Component unregistered: {Name}", registration.Metadata.Name);
            return true;
        }
    }

    /// <inheritdoc />
    public ComponentMetadata? GetComponentMetadata(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var registration = _registrations.Values
            .FirstOrDefault(r => r.Metadata.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return registration?.Metadata;
    }

    /// <inheritdoc />
    public List<ComponentRegistration> FindComponentsByTags(params string[] tags)
    {
        if (tags == null || tags.Length == 0)
            return new List<ComponentRegistration>();

        return _registrations.Values
            .Where(r => tags.Any(tag => r.Metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <inheritdoc />
    public List<ComponentRegistration> FindComponentsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new List<ComponentRegistration>();

        return _registrations.Values
            .Where(r => r.Metadata.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public List<T> FindComponentsByType<T>() where T : class
    {
        return _registrations.Values
            .Where(r => r.Instance is T && r.Metadata.IsEnabled)
            .Select(r => (T)r.Instance)
            .ToList();
    }

    /// <inheritdoc />
    public List<ComponentRegistration> GetAllRegistrations()
    {
        return _registrations.Values.ToList();
    }

    /// <inheritdoc />
    public void UpdateUsageStatistics(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var registration = _registrations.Values
            .FirstOrDefault(r => r.Metadata.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (registration != null)
        {
            registration.LastUsedAt = DateTime.UtcNow;
            registration.UsageCount++;
        }
    }

    /// <inheritdoc />
    public void SetComponentEnabled(string name, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var registration = _registrations.Values
            .FirstOrDefault(r => r.Metadata.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (registration != null)
        {
            registration.Metadata.IsEnabled = enabled;
            _logger.LogInformation("Component {Name} {Status}", name, enabled ? "enabled" : "disabled");
        }
    }

    /// <inheritdoc />
    public RegistryStatus GetRegistryStatus()
    {
        var registrations = _registrations.Values.ToList();
        
        var status = new RegistryStatus
        {
            TotalComponents = registrations.Count,
            LLMFunctionCount = registrations.Count(r => r.Instance is ILLMFunction),
            ToolCount = registrations.Count(r => r.Instance is ITool),
            EnabledComponents = registrations.Count(r => r.Metadata.IsEnabled),
            DisabledComponents = registrations.Count(r => !r.Metadata.IsEnabled),
            LastUpdated = DateTime.UtcNow
        };

        // 카테고리별 통계
        status.CategoryStatistics = registrations
            .Where(r => !string.IsNullOrEmpty(r.Metadata.Category))
            .GroupBy(r => r.Metadata.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        // 태그별 통계
        status.TagStatistics = registrations
            .SelectMany(r => r.Metadata.Tags)
            .GroupBy(tag => tag)
            .ToDictionary(g => g.Key, g => g.Count());

        return status;
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lockObject)
        {
            _registrations.Clear();
            _llmFunctions.Clear();
            _tools.Clear();
            
            _logger.LogInformation("Registry cleared");
        }
    }

    /// <inheritdoc />
    public int AutoRegisterFromAssembly<T>(Assembly assembly) where T : class
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t) && 
                           !t.IsInterface && 
                           !t.IsAbstract &&
                           t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as T;
                    if (instance != null)
                    {
                        if (instance is ILLMFunction llmFunction)
                        {
                            var metadata = CreateLLMFunctionMetadataFromType(type, llmFunction);
                            RegisterLLMFunction(llmFunction, metadata);
                            registeredCount++;
                        }
                        else if (instance is ITool tool)
                        {
                            var metadata = CreateToolMetadataFromType(type, tool);
                            RegisterTool(tool, metadata);
                            registeredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register component: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Auto-registered {Count} components of type {TypeName} from assembly: {AssemblyName}", 
                registeredCount, typeof(T).Name, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-register components from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int AutoRegisterFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
            return 0;

        var totalRegistered = 0;

        foreach (var assembly in assemblies)
        {
            try
            {
                AutoRegisterFromAssembly(assembly);
                totalRegistered++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-register from assembly: {AssemblyName}", 
                    assembly.GetName().Name);
            }
        }

        return totalRegistered;
    }

    /// <inheritdoc />
    public int AutoRegisterFromNamespace(Assembly assembly, string namespacePattern)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        if (string.IsNullOrWhiteSpace(namespacePattern))
            throw new ArgumentException("Namespace pattern cannot be null or empty", nameof(namespacePattern));

        var registeredCount = 0;
        var regex = new Regex(namespacePattern, RegexOptions.IgnoreCase);

        try
        {
            var types = assembly.GetTypes()
                .Where(t => t.Namespace != null && regex.IsMatch(t.Namespace))
                .Where(t => (typeof(ILLMFunction).IsAssignableFrom(t) || typeof(ITool).IsAssignableFrom(t)) &&
                           !t.IsInterface && 
                           !t.IsAbstract &&
                           t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    
                    if (instance is ILLMFunction llmFunction)
                    {
                        var metadata = CreateLLMFunctionMetadataFromType(type, llmFunction);
                        RegisterLLMFunction(llmFunction, metadata);
                        registeredCount++;
                    }
                    else if (instance is ITool tool)
                    {
                        var metadata = CreateToolMetadataFromType(type, tool);
                        RegisterTool(tool, metadata);
                        registeredCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register component: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Auto-registered {Count} components from namespace pattern '{Pattern}' in assembly: {AssemblyName}", 
                registeredCount, namespacePattern, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-register from namespace pattern: {Pattern}", namespacePattern);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int AutoRegisterWithAttributes(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Where(t => t.GetCustomAttributes().Any(attr => 
                    attr.GetType().Name.EndsWith("Attribute") &&
                    (attr.GetType().Name.Contains("LLMFunction") ||
                     attr.GetType().Name.Contains("Tool"))))
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    if (typeof(ILLMFunction).IsAssignableFrom(type))
                    {
                        var instance = Activator.CreateInstance(type) as ILLMFunction;
                        if (instance != null)
                        {
                            var metadata = CreateLLMFunctionMetadataFromType(type, instance);
                            RegisterLLMFunction(instance, metadata);
                            registeredCount++;
                        }
                    }
                    else if (typeof(ITool).IsAssignableFrom(type))
                    {
                        var instance = Activator.CreateInstance(type) as ITool;
                        if (instance != null)
                        {
                            var metadata = CreateToolMetadataFromType(type, instance);
                            RegisterTool(instance, metadata);
                            registeredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register component with attributes: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Auto-registered {Count} components with attributes from assembly: {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-register with attributes from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public int AutoRegisterWithAttributesFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
            return 0;

        var totalRegistered = 0;

        foreach (var assembly in assemblies)
        {
            try
            {
                totalRegistered += AutoRegisterWithAttributes(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-register with attributes from assembly: {AssemblyName}", 
                    assembly.GetName().Name);
            }
        }

        return totalRegistered;
    }

    /// <inheritdoc />
    public int AutoRegisterByAttribute<TAttribute>(Assembly assembly) where TAttribute : Attribute
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;

        try
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<TAttribute>() != null)
                .ToList();

            foreach (var type in types)
            {
                try
                {
                    if (typeof(ILLMFunction).IsAssignableFrom(type))
                    {
                        var instance = Activator.CreateInstance(type) as ILLMFunction;
                        if (instance != null)
                        {
                            var metadata = CreateLLMFunctionMetadataFromType(type, instance);
                            RegisterLLMFunction(instance, metadata);
                            registeredCount++;
                        }
                    }
                    else if (typeof(ITool).IsAssignableFrom(type))
                    {
                        var instance = Activator.CreateInstance(type) as ITool;
                        if (instance != null)
                        {
                            var metadata = CreateToolMetadataFromType(type, instance);
                            RegisterTool(instance, metadata);
                            registeredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register component by attribute: {TypeName}", type.Name);
                }
            }

            _logger.LogInformation("Auto-registered {Count} components by attribute {AttributeName} from assembly: {AssemblyName}", 
                registeredCount, typeof(TAttribute).Name, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-register by attribute from assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return registeredCount;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// 기본 LLM 기능 메타데이터 생성
    /// </summary>
    private LLMFunctionMetadata CreateDefaultLLMFunctionMetadata(ILLMFunction function)
    {
        var type = function.GetType();
        return new LLMFunctionMetadata
        {
            Name = GetComponentName(function),
            Description = $"LLM Function: {type.Name}",
            ComponentType = type,
            Role = GetLLMFunctionRole(function),
            Category = "LLM"
        };
    }

    /// <summary>
    /// 기본 도구 메타데이터 생성
    /// </summary>
    private ToolMetadata CreateDefaultToolMetadata(ITool tool)
    {
        var type = tool.GetType();
        return new ToolMetadata
        {
            Name = GetComponentName(tool),
            Description = $"Tool: {type.Name}",
            ComponentType = type,
            ToolType = ToolType.BuiltIn,
            Category = "Tool"
        };
    }

    /// <summary>
    /// 타입에서 LLM 기능 메타데이터 생성
    /// </summary>
    private LLMFunctionMetadata CreateLLMFunctionMetadataFromType(Type type, ILLMFunction function)
    {
        var metadata = CreateDefaultLLMFunctionMetadata(function);
        
        // Attribute에서 추가 정보 추출
        var attribute = type.GetCustomAttribute<AIAgentFramework.Core.LLM.Attributes.LLMFunctionAttribute>();
        if (attribute != null)
        {
            metadata.Name = !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : metadata.Name;
            metadata.Role = !string.IsNullOrEmpty(attribute.Role) ? attribute.Role : metadata.Role;
            metadata.Description = !string.IsNullOrEmpty(attribute.Description) ? attribute.Description : metadata.Description;
            metadata.Version = attribute.Version;
            metadata.Author = attribute.Author;
            metadata.Category = attribute.Category;
            metadata.Tags = attribute.GetTags();
            metadata.SupportedModels = attribute.GetSupportedModels();
            metadata.RequiredParameters = attribute.GetRequiredParameters();
            metadata.OptionalParameters = attribute.GetOptionalParameters();
            metadata.ResponseSchema = !string.IsNullOrEmpty(attribute.ResponseSchema) ? attribute.ResponseSchema : null;
            metadata.Priority = attribute.Priority;
            metadata.IsEnabled = attribute.IsEnabled;
        }
        
        return metadata;
    }

    /// <summary>
    /// 타입에서 도구 메타데이터 생성
    /// </summary>
    private ToolMetadata CreateToolMetadataFromType(Type type, ITool tool)
    {
        var metadata = CreateDefaultToolMetadata(tool);
        
        // Built-In Tool Attribute 확인
        var builtInAttribute = type.GetCustomAttribute<BuiltInToolAttribute>();
        if (builtInAttribute != null)
        {
            metadata.Name = !string.IsNullOrEmpty(builtInAttribute.Name) ? builtInAttribute.Name : metadata.Name;
            metadata.Description = !string.IsNullOrEmpty(builtInAttribute.Description) ? builtInAttribute.Description : metadata.Description;
            metadata.Version = builtInAttribute.Version;
            metadata.Author = builtInAttribute.Author;
            metadata.Category = builtInAttribute.Category;
            metadata.Tags = builtInAttribute.GetTags();
            metadata.ToolType = ToolType.BuiltIn;
            metadata.InputSchema = !string.IsNullOrEmpty(builtInAttribute.InputSchema) ? builtInAttribute.InputSchema : null;
            metadata.OutputSchema = !string.IsNullOrEmpty(builtInAttribute.OutputSchema) ? builtInAttribute.OutputSchema : null;
            metadata.Dependencies = builtInAttribute.GetDependencies();
            metadata.RequiredPermissions = builtInAttribute.GetRequiredPermissions();
            metadata.TimeoutSeconds = builtInAttribute.TimeoutSeconds;
            metadata.IsCacheable = builtInAttribute.IsCacheable;
            metadata.CacheTTLMinutes = builtInAttribute.CacheTTLMinutes;
            metadata.IsEnabled = builtInAttribute.IsEnabled;
            return metadata;
        }

        // Plugin Tool Attribute 확인
        var pluginAttribute = type.GetCustomAttribute<PluginToolAttribute>();
        if (pluginAttribute != null)
        {
            metadata.Name = !string.IsNullOrEmpty(pluginAttribute.Name) ? pluginAttribute.Name : metadata.Name;
            metadata.Description = !string.IsNullOrEmpty(pluginAttribute.Description) ? pluginAttribute.Description : metadata.Description;
            metadata.Version = pluginAttribute.Version;
            metadata.Author = pluginAttribute.Author;
            metadata.Category = pluginAttribute.Category;
            metadata.Tags = pluginAttribute.GetTags();
            metadata.ToolType = ToolType.Plugin;
            metadata.InputSchema = !string.IsNullOrEmpty(pluginAttribute.InputSchema) ? pluginAttribute.InputSchema : null;
            metadata.OutputSchema = !string.IsNullOrEmpty(pluginAttribute.OutputSchema) ? pluginAttribute.OutputSchema : null;
            metadata.Dependencies = pluginAttribute.GetDependencies();
            metadata.RequiredPermissions = pluginAttribute.GetRequiredPermissions();
            metadata.TimeoutSeconds = pluginAttribute.TimeoutSeconds;
            metadata.IsCacheable = pluginAttribute.IsCacheable;
            metadata.CacheTTLMinutes = pluginAttribute.CacheTTLMinutes;
            metadata.IsEnabled = pluginAttribute.IsEnabled;
            
            // 플러그인 특화 속성
            metadata.Properties["PluginId"] = pluginAttribute.PluginId;
            metadata.Properties["PackageName"] = pluginAttribute.PackageName;
            metadata.Properties["License"] = pluginAttribute.License;
            return metadata;
        }

        // MCP Tool Attribute 확인
        var mcpAttribute = type.GetCustomAttribute<MCPToolAttribute>();
        if (mcpAttribute != null)
        {
            metadata.Name = !string.IsNullOrEmpty(mcpAttribute.Name) ? mcpAttribute.Name : metadata.Name;
            metadata.Description = !string.IsNullOrEmpty(mcpAttribute.Description) ? mcpAttribute.Description : metadata.Description;
            metadata.Version = mcpAttribute.Version;
            metadata.Author = mcpAttribute.Author;
            metadata.Category = mcpAttribute.Category;
            metadata.Tags = mcpAttribute.GetTags();
            metadata.ToolType = ToolType.MCP;
            metadata.InputSchema = !string.IsNullOrEmpty(mcpAttribute.InputSchema) ? mcpAttribute.InputSchema : null;
            metadata.OutputSchema = !string.IsNullOrEmpty(mcpAttribute.OutputSchema) ? mcpAttribute.OutputSchema : null;
            metadata.RequiredPermissions = mcpAttribute.GetRequiredPermissions();
            metadata.TimeoutSeconds = mcpAttribute.RequestTimeoutSeconds;
            metadata.IsCacheable = mcpAttribute.IsCacheable;
            metadata.CacheTTLMinutes = mcpAttribute.CacheTTLMinutes;
            metadata.IsEnabled = mcpAttribute.IsEnabled;
            
            // MCP 특화 속성
            metadata.Properties["Endpoint"] = mcpAttribute.Endpoint;
            metadata.Properties["ProtocolVersion"] = mcpAttribute.ProtocolVersion;
            return metadata;
        }
        
        return metadata;
    }

    /// <summary>
    /// 컴포넌트 이름 추출
    /// </summary>
    private string GetComponentName(object component)
    {
        // 실제 구현에서는 인터페이스에서 Name 속성을 가져오거나
        // Attribute에서 이름을 추출할 수 있음
        return component.GetType().Name;
    }

    /// <summary>
    /// LLM 기능 역할 추출
    /// </summary>
    private string GetLLMFunctionRole(ILLMFunction function)
    {
        // 실제 구현에서는 인터페이스에서 Role 속성을 가져오거나
        // Attribute에서 역할을 추출할 수 있음
        var typeName = function.GetType().Name;
        return typeName.Replace("Function", "").ToLowerInvariant();
    }

    /// <summary>
    /// 컴포넌트 활성화 상태 확인
    /// </summary>
    private bool IsComponentEnabled(string name)
    {
        var registration = _registrations.Values
            .FirstOrDefault(r => r.Metadata.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return registration?.Metadata.IsEnabled ?? true;
    }

    #endregion
}