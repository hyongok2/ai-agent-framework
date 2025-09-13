using AIAgentFramework.Core.Tools.Abstractions;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Tools.BuiltIn;

/// <summary>
/// 도구 기본 클래스 - 독립적 구현
/// </summary>
public abstract class ToolBase : ITool
{
    protected readonly ILogger _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    protected ToolBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Tool created: {Name}", Name);
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract string Category { get; }

    /// <inheritdoc />
    public abstract Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task<bool> ValidateAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        // 기본 구현: 항상 true
        return Task.FromResult(true);
    }

    /// <summary>
    /// 파라미터 값 가져오기
    /// </summary>
    protected T? GetParameter<T>(IToolInput input, string key, T? defaultValue = default)
    {
        if (input.Parameters.TryGetValue(key, out var value) && value != null)
        {
            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 필수 파라미터 값 가져오기
    /// </summary>
    protected T GetRequiredParameter<T>(IToolInput input, string key)
    {
        if (!input.Parameters.TryGetValue(key, out var value) || value == null)
        {
            throw new ArgumentException($"Required parameter '{key}' is missing or null");
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Parameter '{key}' cannot be converted to {typeof(T).Name}", ex);
        }
    }
}