using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Tools.Plugin;

/// <summary>
/// 플러그인 도구 기본 추상 클래스
/// </summary>
public abstract class PluginToolBase : IPluginTool
{
    protected readonly ILogger _logger;
    protected Dictionary<string, object> _configuration = new();
    protected bool _initialized;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    protected PluginToolBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }
    
    /// <inheritdoc />
    public virtual string Category { get; } = "Plugin";

    /// <inheritdoc />
    public abstract string Version { get; }

    /// <inheritdoc />
    public abstract string Author { get; }

    /// <inheritdoc />
    public abstract IEnumerable<string> Dependencies { get; }

    /// <inheritdoc />
    public abstract IToolContract Contract { get; }

    /// <inheritdoc />
    public virtual async Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Initializing plugin tool: {ToolName}", Name);

            _configuration = configuration ?? new Dictionary<string, object>();

            // 사용자 정의 초기화 로직 실행
            await InitializeInternalAsync(_configuration, cancellationToken);

            _initialized = true;

            _logger.LogDebug("Successfully initialized plugin tool: {ToolName}", Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize plugin tool: {ToolName}", Name);
            return false;
        }
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Disposing plugin tool: {ToolName}", Name);

            // 사용자 정의 정리 로직 실행
            await DisposeInternalAsync(cancellationToken);

            _initialized = false;

            _logger.LogDebug("Successfully disposed plugin tool: {ToolName}", Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing plugin tool: {ToolName}", Name);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Dictionary<string, object>> GetStatusAsync()
    {
        var status = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["version"] = Version,
            ["author"] = Author,
            ["initialized"] = _initialized,
            ["configuration_count"] = _configuration.Count,
            ["dependencies"] = Dependencies.ToList()
        };

        try
        {
            // 사용자 정의 상태 정보 추가
            var customStatus = await GetCustomStatusAsync();
            foreach (var kvp in customStatus)
            {
                status[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom status for plugin tool: {ToolName}", Name);
            status["status_error"] = ex.Message;
        }

        return status;
    }

    /// <inheritdoc />
    public virtual async Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_initialized)
            {
                return ToolResult.CreateFailure($"Plugin tool '{Name}' is not initialized");
            }

            _logger.LogDebug("Executing plugin tool: {ToolName}", Name);

            // 입력 검증
            if (!await ValidateAsync(input, cancellationToken))
            {
                return ToolResult.CreateFailure($"Input validation failed for plugin tool '{Name}'");
            }

            // 전처리
            await PreProcessAsync(input, cancellationToken);

            // 실제 실행
            var result = await ExecuteInternalAsync(input, cancellationToken);

            // 후처리
            await PostProcessAsync(result, input, cancellationToken);

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogDebug("Completed plugin tool execution: {ToolName} in {ElapsedMs}ms", 
                Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing plugin tool: {ToolName}", Name);

            return ToolResult.CreateFailure($"Execution failed: {ex.Message}", stopwatch.Elapsed)
                .WithMetadata("exception", ex.GetType().Name)
                .WithMetadata("tool_name", Name)
                .WithMetadata("plugin_tool", true);
        }
    }

    /// <inheritdoc />
    public virtual Task<bool> ValidateAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        if (input == null)
        {
            _logger.LogWarning("Input is null for plugin tool: {ToolName}", Name);
            return Task.FromResult(false);
        }

        // 필수 파라미터 확인
        foreach (var parameter in Contract.RequiredParameters)
        {
            if (!input.Parameters.ContainsKey(parameter) ||
                input.Parameters[parameter] == null ||
                string.IsNullOrWhiteSpace(input.Parameters[parameter].ToString()))
            {
                _logger.LogWarning("Required parameter missing: {Parameter} for plugin tool: {ToolName}",
                    parameter, Name);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// 사용자 정의 초기화 로직 (하위 클래스에서 구현)
    /// </summary>
    /// <param name="configuration">설정</param>
    /// <param name="cancellationToken">취소 토큰</param>
    protected virtual Task InitializeInternalAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 사용자 정의 정리 로직 (하위 클래스에서 구현)
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    protected virtual Task DisposeInternalAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 사용자 정의 상태 정보 (하위 클래스에서 구현)
    /// </summary>
    /// <returns>상태 정보</returns>
    protected virtual Task<Dictionary<string, object>> GetCustomStatusAsync()
    {
        return Task.FromResult(new Dictionary<string, object>());
    }

    /// <summary>
    /// 전처리 (하위 클래스에서 오버라이드 가능)
    /// </summary>
    /// <param name="input">도구 입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    protected virtual Task PreProcessAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 실제 실행 (하위 클래스에서 구현)
    /// </summary>
    /// <param name="input">도구 입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    protected abstract Task<ToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 후처리 (하위 클래스에서 오버라이드 가능)
    /// </summary>
    /// <param name="result">실행 결과</param>
    /// <param name="input">도구 입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    protected virtual Task PostProcessAsync(ToolResult result, IToolInput input, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 설정 값 가져오기
    /// </summary>
    /// <typeparam name="T">타입</typeparam>
    /// <param name="key">설정 키</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>설정 값</returns>
    protected T? GetConfigurationValue<T>(string key, T? defaultValue = default)
    {
        if (!_configuration.TryGetValue(key, out var value) || value == null)
        {
            return defaultValue;
        }

        try
        {
            if (value is T directValue)
            {
                return directValue;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert configuration value {Key} to type {Type}",
                key, typeof(T).Name);
            return defaultValue;
        }
    }

    /// <summary>
    /// 파라미터 값 가져오기
    /// </summary>
    /// <typeparam name="T">타입</typeparam>
    /// <param name="input">입력</param>
    /// <param name="parameterName">파라미터명</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>파라미터 값</returns>
    protected T? GetParameter<T>(IToolInput input, string parameterName, T? defaultValue = default)
    {
        if (!input.Parameters.TryGetValue(parameterName, out var value) || value == null)
        {
            return defaultValue;
        }

        try
        {
            if (value is T directValue)
            {
                return directValue;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert parameter {Parameter} to type {Type}",
                parameterName, typeof(T).Name);
            return defaultValue;
        }
    }

    /// <summary>
    /// 필수 파라미터 값 가져오기
    /// </summary>
    /// <typeparam name="T">타입</typeparam>
    /// <param name="input">입력</param>
    /// <param name="parameterName">파라미터명</param>
    /// <returns>파라미터 값</returns>
    /// <exception cref="ArgumentException">필수 파라미터가 없는 경우</exception>
    protected T GetRequiredParameter<T>(IToolInput input, string parameterName)
    {
        var value = GetParameter<T>(input, parameterName);
        if (value == null)
        {
            throw new ArgumentException($"Required parameter '{parameterName}' is missing or null");
        }
        return value;
    }
}