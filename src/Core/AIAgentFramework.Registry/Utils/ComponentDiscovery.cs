
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Registry.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AIAgentFramework.Registry.Utils;

/// <summary>
/// 컴포넌트 발견 유틸리티
/// </summary>
public class ComponentDiscovery : IComponentDiscovery
{
    private readonly ILogger<ComponentDiscovery> _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public ComponentDiscovery(ILogger<ComponentDiscovery> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public List<Type> DiscoverLLMFunctions(Assembly assembly)
    {
        return DiscoverTypes<ILLMFunction>(assembly);
    }

    /// <inheritdoc />
    public List<Type> DiscoverTools(Assembly assembly)
    {
        return DiscoverTypes<ITool>(assembly);
    }

    /// <inheritdoc />
    public List<Type> DiscoverTypes<T>(Assembly assembly) where T : class
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var discoveredTypes = new List<Type>();

        try
        {
            var types = assembly.GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t) && 
                           !t.IsInterface && 
                           !t.IsAbstract)
                .ToList();

            foreach (var type in types)
            {
                if (IsValidComponent(type))
                {
                    discoveredTypes.Add(type);
                    _logger.LogDebug("Discovered component: {TypeName} implementing {InterfaceName}", 
                        type.Name, typeof(T).Name);
                }
            }

            _logger.LogInformation("Discovered {Count} components of type {TypeName} in assembly: {AssemblyName}", 
                discoveredTypes.Count, typeof(T).Name, assembly.GetName().Name);
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning("Failed to load some types from assembly {AssemblyName}: {Errors}", 
                assembly.GetName().Name, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message)));
            
            // 로드 가능한 타입들만 처리
            var loadableTypes = ex.Types.Where(t => t != null).Cast<Type>();
            discoveredTypes.AddRange(loadableTypes.Where(t => typeof(T).IsAssignableFrom(t) && 
                                                             !t.IsInterface && 
                                                             !t.IsAbstract &&
                                                             IsValidComponent(t)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover components in assembly: {AssemblyName}", 
                assembly.GetName().Name);
            throw;
        }

        return discoveredTypes;
    }

    /// <inheritdoc />
    public List<Type> DiscoverTypesByNamespace(Assembly assembly, string namespacePattern)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        if (string.IsNullOrWhiteSpace(namespacePattern))
            throw new ArgumentException("Namespace pattern cannot be null or empty", nameof(namespacePattern));

        var discoveredTypes = new List<Type>();
        var regex = new Regex(namespacePattern, RegexOptions.IgnoreCase);

        try
        {
            var types = assembly.GetTypes()
                .Where(t => t.Namespace != null && regex.IsMatch(t.Namespace))
                .Where(t => (typeof(ILLMFunction).IsAssignableFrom(t) || typeof(ITool).IsAssignableFrom(t)) &&
                           !t.IsInterface && 
                           !t.IsAbstract)
                .ToList();

            foreach (var type in types)
            {
                if (IsValidComponent(type))
                {
                    discoveredTypes.Add(type);
                    _logger.LogDebug("Discovered component: {TypeName} in namespace: {Namespace}", 
                        type.Name, type.Namespace);
                }
            }

            _logger.LogInformation("Discovered {Count} components matching namespace pattern '{Pattern}' in assembly: {AssemblyName}", 
                discoveredTypes.Count, namespacePattern, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover components by namespace pattern: {Pattern}", namespacePattern);
            throw;
        }

        return discoveredTypes;
    }

    /// <inheritdoc />
    public List<Assembly> DiscoverAssemblies(string directoryPath, string searchPattern = "*.dll")
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

        var assemblies = new List<Assembly>();

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
                return assemblies;
            }

            var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    assemblies.Add(assembly);
                    
                    _logger.LogDebug("Loaded assembly: {AssemblyName} from {FilePath}", 
                        assembly.GetName().Name, file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load assembly from file: {FilePath}", file);
                }
            }

            _logger.LogInformation("Discovered {Count} assemblies in directory: {DirectoryPath}", 
                assemblies.Count, directoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover assemblies in directory: {DirectoryPath}", directoryPath);
            throw;
        }

        return assemblies;
    }

    /// <inheritdoc />
    public ComponentMetadata ExtractMetadata(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        ComponentMetadata metadata;

        if (typeof(ILLMFunction).IsAssignableFrom(type))
        {
            metadata = new LLMFunctionMetadata
            {
                Role = ExtractLLMFunctionRole(type)
            };
        }
        else if (typeof(ITool).IsAssignableFrom(type))
        {
            metadata = new ToolMetadata
            {
                ToolType = ExtractToolType(type)
            };
        }
        else
        {
            metadata = new LLMFunctionMetadata(); // 기본값
        }

        // 공통 메타데이터 설정
        metadata.Name = ExtractComponentName(type);
        metadata.Description = ExtractDescription(type);
        metadata.Version = ExtractVersion(type);
        metadata.Author = ExtractAuthor(type);
        metadata.Category = ExtractCategory(type);
        metadata.Tags = ExtractTags(type);
        metadata.ComponentType = type;

        return metadata;
    }

    /// <inheritdoc />
    public bool ValidateComponent(Type type)
    {
        if (type == null)
            return false;

        try
        {
            // 기본 검증
            if (!IsValidComponent(type))
                return false;

            // 생성자 검증
            if (!HasValidConstructor(type))
            {
                _logger.LogWarning("Component {TypeName} does not have a valid constructor", type.Name);
                return false;
            }

            // 인터페이스 구현 검증
            if (!HasValidInterfaceImplementation(type))
            {
                _logger.LogWarning("Component {TypeName} does not properly implement required interfaces", type.Name);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate component: {TypeName}", type.Name);
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// 유효한 컴포넌트인지 확인
    /// </summary>
    private bool IsValidComponent(Type type)
    {
        return !type.IsInterface && 
               !type.IsAbstract && 
               (typeof(ILLMFunction).IsAssignableFrom(type) || typeof(ITool).IsAssignableFrom(type));
    }

    /// <summary>
    /// 유효한 생성자가 있는지 확인
    /// </summary>
    private bool HasValidConstructor(Type type)
    {
        // 기본 생성자 또는 DI 가능한 생성자가 있는지 확인
        var constructors = type.GetConstructors();
        return constructors.Any(c => c.GetParameters().Length == 0) || 
               constructors.Any(c => c.GetParameters().All(p => IsInjectableParameter(p)));
    }

    /// <summary>
    /// 주입 가능한 매개변수인지 확인
    /// </summary>
    private bool IsInjectableParameter(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        return type.IsInterface || 
               type.IsClass && !type.IsSealed ||
               type.Namespace?.StartsWith("Microsoft.Extensions") == true;
    }

    /// <summary>
    /// 유효한 인터페이스 구현인지 확인
    /// </summary>
    private bool HasValidInterfaceImplementation(Type type)
    {
        if (typeof(ILLMFunction).IsAssignableFrom(type))
        {
            // ILLMFunction의 필수 멤버들이 구현되어 있는지 확인
            return type.GetInterfaces().Contains(typeof(ILLMFunction));
        }
        
        if (typeof(ITool).IsAssignableFrom(type))
        {
            // ITool의 필수 멤버들이 구현되어 있는지 확인
            return type.GetInterfaces().Contains(typeof(ITool));
        }

        return false;
    }

    /// <summary>
    /// 컴포넌트 이름 추출
    /// </summary>
    private string ExtractComponentName(Type type)
    {
        // Attribute에서 이름을 추출하거나 타입 이름 사용
        return type.Name;
    }

    /// <summary>
    /// 설명 추출
    /// </summary>
    private string ExtractDescription(Type type)
    {
        // Attribute나 XML 문서에서 설명 추출
        return $"Component: {type.Name}";
    }

    /// <summary>
    /// 버전 추출
    /// </summary>
    private string ExtractVersion(Type type)
    {
        // Assembly 버전 또는 Attribute에서 버전 추출
        return type.Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// 작성자 추출
    /// </summary>
    private string ExtractAuthor(Type type)
    {
        // Attribute에서 작성자 정보 추출
        return "Unknown";
    }

    /// <summary>
    /// 카테고리 추출
    /// </summary>
    private string ExtractCategory(Type type)
    {
        if (typeof(ILLMFunction).IsAssignableFrom(type))
            return "LLM";
        if (typeof(ITool).IsAssignableFrom(type))
            return "Tool";
        return "Unknown";
    }

    /// <summary>
    /// 태그 추출
    /// </summary>
    private List<string> ExtractTags(Type type)
    {
        var tags = new List<string>();
        
        // 네임스페이스 기반 태그
        if (!string.IsNullOrEmpty(type.Namespace))
        {
            var namespaceParts = type.Namespace.Split('.');
            tags.AddRange(namespaceParts.Skip(1)); // 첫 번째 부분(보통 회사명) 제외
        }

        return tags;
    }

    /// <summary>
    /// LLM 기능 역할 추출
    /// </summary>
    private string ExtractLLMFunctionRole(Type type)
    {
        var typeName = type.Name;
        if (typeName.EndsWith("Function"))
        {
            return typeName.Substring(0, typeName.Length - 8).ToLowerInvariant();
        }
        return typeName.ToLowerInvariant();
    }

    /// <summary>
    /// 도구 타입 추출
    /// </summary>
    private ToolType ExtractToolType(Type type)
    {
        var namespaceName = type.Namespace ?? "";
        
        if (namespaceName.Contains("Plugin"))
            return ToolType.Plugin;
        if (namespaceName.Contains("MCP"))
            return ToolType.MCP;
        
        return ToolType.BuiltIn;
    }

    #endregion
}

/// <summary>
/// 컴포넌트 발견 인터페이스
/// </summary>
public interface IComponentDiscovery
{
    /// <summary>
    /// 어셈블리에서 LLM 기능을 발견합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>LLM 기능 타입 목록</returns>
    List<Type> DiscoverLLMFunctions(Assembly assembly);
    
    /// <summary>
    /// 어셈블리에서 도구를 발견합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <returns>도구 타입 목록</returns>
    List<Type> DiscoverTools(Assembly assembly);
    
    /// <summary>
    /// 어셈블리에서 특정 타입의 컴포넌트를 발견합니다.
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <param name="assembly">어셈블리</param>
    /// <returns>컴포넌트 타입 목록</returns>
    List<Type> DiscoverTypes<T>(Assembly assembly) where T : class;
    
    /// <summary>
    /// 네임스페이스 패턴으로 컴포넌트를 발견합니다.
    /// </summary>
    /// <param name="assembly">어셈블리</param>
    /// <param name="namespacePattern">네임스페이스 패턴</param>
    /// <returns>컴포넌트 타입 목록</returns>
    List<Type> DiscoverTypesByNamespace(Assembly assembly, string namespacePattern);
    
    /// <summary>
    /// 디렉토리에서 어셈블리를 발견합니다.
    /// </summary>
    /// <param name="directoryPath">디렉토리 경로</param>
    /// <param name="searchPattern">검색 패턴</param>
    /// <returns>어셈블리 목록</returns>
    List<Assembly> DiscoverAssemblies(string directoryPath, string searchPattern = "*.dll");
    
    /// <summary>
    /// 타입에서 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="type">타입</param>
    /// <returns>메타데이터</returns>
    ComponentMetadata ExtractMetadata(Type type);
    
    /// <summary>
    /// 컴포넌트의 유효성을 검증합니다.
    /// </summary>
    /// <param name="type">컴포넌트 타입</param>
    /// <returns>유효성 검증 결과</returns>
    bool ValidateComponent(Type type);
}