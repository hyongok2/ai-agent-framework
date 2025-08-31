
using AIAgentFramework.Core.Actions.Factories;
using AIAgentFramework.Core.Infrastructure;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Registry;
using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.Tools.Registry;
using AIAgentFramework.Registry.AttributeBasedRegistration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AIAgentFramework.Registry.Extensions;

/// <summary>
/// 서비스 컬렉션 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registry 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddRegistry(this IServiceCollection services)
    {
        // Attribute 메타데이터 추출기 등록
        services.AddSingleton<AIAgentFramework.Registry.Utils.IAttributeMetadataExtractor, AIAgentFramework.Registry.Utils.AttributeMetadataExtractor>();
        
        // 컴포넌트 발견 유틸리티 등록
        services.AddSingleton<AIAgentFramework.Registry.Utils.IComponentDiscovery, AIAgentFramework.Registry.Utils.ComponentDiscovery>();
        
        // 기존 Registry 등록 (호환성 유지)
        services.AddSingleton<IRegistry, Registry>();
        services.AddSingleton<IAdvancedRegistry, Registry>();
        services.AddSingleton<IAttributeBasedComponentRegistrar, AttributeBasedComponentRegistrar>();
        
        // 새로운 타입 안전한 Registry 등록
        services.AddSingleton<ILLMFunctionRegistry, TypedLLMFunctionRegistry>();
        services.AddSingleton<IToolRegistry, TypedToolRegistry>();
        
        // 타입 안전한 컨텍스트 및 팩토리 서비스 등록 (Core 레벨)
        services.AddScoped<IExecutionContextFactory, ExecutionContextFactory>();
        services.AddScoped<IActionFactory, ActionFactory>();
        
        return services;
    }

    /// <summary>
    /// Registry 서비스를 등록하고 자동 등록을 수행합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assemblies">스캔할 어셈블리 목록</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddRegistryWithAutoRegistration(
        this IServiceCollection services, 
        params Assembly[] assemblies)
    {
        services.AddRegistry();

        // 서비스 프로바이더 빌드 후 자동 등록 수행
        services.AddSingleton<IRegistryInitializer>(provider =>
        {
            var registry = provider.GetRequiredService<IAdvancedRegistry>();
            var logger = provider.GetRequiredService<ILogger<RegistryInitializer>>();
            return new RegistryInitializer(registry, logger, assemblies);
        });

        return services;
    }

    /// <summary>
    /// 현재 어셈블리에서 자동 등록을 수행합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddRegistryWithCurrentAssembly(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        return services.AddRegistryWithAutoRegistration(callingAssembly);
    }

    /// <summary>
    /// 네임스페이스 패턴으로 자동 등록을 수행합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assembly">스캔할 어셈블리</param>
    /// <param name="namespacePattern">네임스페이스 패턴</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddRegistryWithNamespacePattern(
        this IServiceCollection services,
        Assembly assembly,
        string namespacePattern)
    {
        services.AddRegistry();

        services.AddSingleton<IRegistryInitializer>(provider =>
        {
            var registry = provider.GetRequiredService<IAdvancedRegistry>();
            var logger = provider.GetRequiredService<ILogger<RegistryInitializer>>();
            return new NamespacePatternRegistryInitializer(registry, logger, assembly, namespacePattern);
        });

        return services;
    }
}

/// <summary>
/// Registry 초기화 인터페이스
/// </summary>
public interface IRegistryInitializer
{
    /// <summary>
    /// Registry를 초기화합니다.
    /// </summary>
    /// <returns>등록된 컴포넌트 수</returns>
    Task<int> InitializeAsync();
}

/// <summary>
/// Registry 초기화 구현
/// </summary>
public class RegistryInitializer : IRegistryInitializer
{
    private readonly IAdvancedRegistry _registry;
    private readonly ILogger<RegistryInitializer> _logger;
    private readonly Assembly[] _assemblies;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="registry">Registry</param>
    /// <param name="logger">로거</param>
    /// <param name="assemblies">어셈블리 목록</param>
    public RegistryInitializer(
        IAdvancedRegistry registry, 
        ILogger<RegistryInitializer> logger, 
        Assembly[] assemblies)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
    }

    /// <inheritdoc />
    public Task<int> InitializeAsync()
    {
        var totalRegistered = 0;

        try
        {
            _logger.LogInformation("Starting registry initialization with {AssemblyCount} assemblies", _assemblies.Length);

            foreach (var assembly in _assemblies)
            {
                try
                {
                    _registry.AutoRegisterFromAssembly(assembly);
                    totalRegistered++;
                    
                    _logger.LogDebug("Processed assembly: {AssemblyName}", assembly.GetName().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register components from assembly: {AssemblyName}", 
                        assembly.GetName().Name);
                }
            }

            var status = _registry.GetRegistryStatus();
            _logger.LogInformation("Registry initialization completed. Total components: {TotalComponents}, " +
                                 "LLM Functions: {LLMFunctions}, Tools: {Tools}", 
                                 status.TotalComponents, status.LLMFunctionCount, status.ToolCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registry initialization failed");
            throw;
        }

        return Task.FromResult(totalRegistered);
    }
}

/// <summary>
/// 네임스페이스 패턴 기반 Registry 초기화
/// </summary>
public class NamespacePatternRegistryInitializer : IRegistryInitializer
{
    private readonly IAdvancedRegistry _registry;
    private readonly ILogger<RegistryInitializer> _logger;
    private readonly Assembly _assembly;
    private readonly string _namespacePattern;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="registry">Registry</param>
    /// <param name="logger">로거</param>
    /// <param name="assembly">어셈블리</param>
    /// <param name="namespacePattern">네임스페이스 패턴</param>
    public NamespacePatternRegistryInitializer(
        IAdvancedRegistry registry, 
        ILogger<RegistryInitializer> logger, 
        Assembly assembly,
        string namespacePattern)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _namespacePattern = namespacePattern ?? throw new ArgumentNullException(nameof(namespacePattern));
    }

    /// <inheritdoc />
    public Task<int> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting registry initialization with namespace pattern: {Pattern} in assembly: {AssemblyName}", 
                _namespacePattern, _assembly.GetName().Name);

            var registeredCount = _registry.AutoRegisterFromNamespace(_assembly, _namespacePattern);

            var status = _registry.GetRegistryStatus();
            _logger.LogInformation("Registry initialization completed. Registered: {RegisteredCount}, " +
                                 "Total components: {TotalComponents}", 
                                 registeredCount, status.TotalComponents);

            return Task.FromResult(registeredCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registry initialization failed");
            throw;
        }
    }
}