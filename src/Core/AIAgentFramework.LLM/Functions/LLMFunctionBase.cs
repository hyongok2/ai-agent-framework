using AIAgentFramework.Core.LLM.Abstractions;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Functions;

/// <summary>
/// LLM 기능 기본 클래스 - 독립적 구현
/// </summary>
public abstract class LLMFunctionBase : ILLMFunction
{
    protected readonly ILLMProvider _llmProvider;
    protected readonly ILogger _logger;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="llmProvider">LLM Provider</param>
    /// <param name="logger">로거</param>
    protected LLMFunctionBase(ILLMProvider llmProvider, ILogger logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("LLM Function created: {Name}", GetType().Name);
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Role { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        // 기본 구현: 항상 true
        return Task.FromResult(true);
    }
}