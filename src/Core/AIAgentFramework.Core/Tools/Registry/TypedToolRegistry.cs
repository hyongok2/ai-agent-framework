using System.Collections.Concurrent;
using AIAgentFramework.Core.Tools.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Tools.Registry;

/// <summary>
/// 타입 안전한 도구 레지스트리 구현
/// </summary>
public class TypedToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _toolsByName;
    private readonly ConcurrentDictionary<Type, ITool> _toolsByType;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TypedToolRegistry> _logger;

    public TypedToolRegistry(
        IServiceProvider serviceProvider,
        ILogger<TypedToolRegistry> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolsByName = new ConcurrentDictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
        _toolsByType = new ConcurrentDictionary<Type, ITool>();
    }

    /// <inheritdoc />
    public void Register<T>() where T : class, ITool
    {
        var tool = _serviceProvider.GetService<T>()
            ?? ActivatorUtilities.CreateInstance<T>(_serviceProvider);

        Register(tool);
    }

    /// <inheritdoc />
    public void Register<T>(T instance) where T : class, ITool
    {
        ArgumentNullException.ThrowIfNull(instance);

        var type = typeof(T);
        var name = instance.Name;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Tool {type.Name} must have a valid name");
        }

        if (_toolsByName.TryAdd(name, instance))
        {
            _toolsByType.TryAdd(type, instance);
            _logger.LogInformation("Registered tool: {ToolName} (Type: {ToolType}, Category: {Category})",
                name, type.Name, instance.Category);
        }
        else
        {
            throw new InvalidOperationException($"Tool with name '{name}' is already registered");
        }
    }

    /// <inheritdoc />
    public void Register(string name, ITool tool)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(tool);

        if (_toolsByName.TryAdd(name, tool))
        {
            _toolsByType.TryAdd(tool.GetType(), tool);
            _logger.LogInformation("Registered tool: {ToolName} (Category: {Category})",
                name, tool.Category);
        }
        else
        {
            throw new InvalidOperationException($"Tool with name '{name}' is already registered");
        }
    }

    /// <inheritdoc />
    public T Resolve<T>() where T : class, ITool
    {
        var type = typeof(T);

        if (_toolsByType.TryGetValue(type, out var tool) && tool is T typedTool)
        {
            return typedTool;
        }

        // 인터페이스나 기본 클래스로 검색
        var matchingTool = _toolsByType.Values
            .OfType<T>()
            .FirstOrDefault();

        if (matchingTool != null)
        {
            return matchingTool;
        }

        throw new InvalidOperationException($"Tool of type '{type.Name}' is not registered");
    }

    /// <inheritdoc />
    public ITool Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_toolsByName.TryGetValue(name, out var tool))
        {
            return tool;
        }

        throw new InvalidOperationException($"Tool with name '{name}' is not registered");
    }

    /// <inheritdoc />
    public bool IsRegistered(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return _toolsByName.ContainsKey(name);
    }

    /// <inheritdoc />
    public bool IsRegistered<T>() where T : class, ITool
    {
        var type = typeof(T);
        return _toolsByType.ContainsKey(type) ||
               _toolsByType.Values.Any(t => t is T);
    }

    /// <inheritdoc />
    public IEnumerable<ITool> GetAll()
    {
        return _toolsByName.Values.ToList();
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllNames()
    {
        return _toolsByName.Keys.ToList();
    }

    /// <inheritdoc />
    public IEnumerable<ITool> GetByCategory(string category)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);

        return _toolsByName.Values
            .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public void Clear()
    {
        _toolsByName.Clear();
        _toolsByType.Clear();
        _logger.LogInformation("Tool registry cleared");
    }
}