using AIAgentFramework.Core.LLM.Abstractions;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// 단순한 LLM Provider 기본 클래스
/// </summary>
public abstract class LLMProviderBase : ILLMProvider
{
    protected readonly ILogger _logger;
    protected readonly HttpClient _httpClient;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="httpClient">HTTP 클라이언트</param>
    /// <param name="logger">로거</param>
    protected LLMProviderBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedModels { get; }

    /// <inheritdoc />
    public abstract string DefaultModel { get; }

    /// <inheritdoc />
    public virtual Task<bool> IsAvailableAsync()
    {
        // 기본 구현: 항상 사용 가능
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public abstract Task<string> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<int> CountTokensAsync(string text, string? model = null);

    /// <summary>
    /// HTTP 클라이언트 설정 (하위 클래스에서 구현)
    /// </summary>
    protected virtual void ConfigureHttpClient()
    {
        // 기본 구현: 아무것도 하지 않음
    }
}