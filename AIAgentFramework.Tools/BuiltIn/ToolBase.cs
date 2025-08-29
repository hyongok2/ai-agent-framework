using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Tools.BuiltIn;

/// <summary>
/// 도구 기본 추상 클래스
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
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract IToolContract Contract { get; }

    /// <inheritdoc />
    public virtual async Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting tool execution: {ToolName}", Name);

            // 입력 검증
            if (!await ValidateAsync(input, cancellationToken))
            {
                return ToolResult.CreateFailure($"Validation failed for tool: {Name}");
            }

            // 전처리
            await PreProcessAsync(input, cancellationToken);

            // 실제 실행
            var result = await ExecuteInternalAsync(input, cancellationToken);

            // 후처리
            await PostProcessAsync(result, input, cancellationToken);

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogDebug("Completed tool execution: {ToolName} in {ElapsedMs}ms", 
                Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing tool: {ToolName}", Name);
            
            return ToolResult.CreateFailure($"Execution failed: {ex.Message}", stopwatch.Elapsed)
                .WithMetadata("exception", ex.GetType().Name)
                .WithMetadata("tool_name", Name);
        }
    }

    /// <inheritdoc />
    public virtual Task<bool> ValidateAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        // 기본 검증: 필수 파라미터 확인
        foreach (var parameter in Contract.RequiredParameters)
        {
            if (!input.Parameters.ContainsKey(parameter) || 
                input.Parameters[parameter] == null)
            {
                _logger.LogWarning("Required parameter missing: {Parameter} for tool: {ToolName}", 
                    parameter, Name);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
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
        // 기본 메타데이터 추가
        result.WithMetadata("tool_name", Name)
              .WithMetadata("executed_at", DateTime.UtcNow);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 파라미터 값 가져오기
    /// </summary>
    /// <typeparam name="T">파라미터 타입</typeparam>
    /// <param name="input">입력</param>
    /// <param name="parameterName">파라미터 이름</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>파라미터 값</returns>
    protected T GetParameter<T>(IToolInput input, string parameterName, T defaultValue = default!)
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

            // 타입 변환 시도
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
    /// <typeparam name="T">파라미터 타입</typeparam>
    /// <param name="input">입력</param>
    /// <param name="parameterName">파라미터 이름</param>
    /// <returns>파라미터 값</returns>
    /// <exception cref="ArgumentException">필수 파라미터가 없는 경우</exception>
    protected T GetRequiredParameter<T>(IToolInput input, string parameterName)
    {
        if (!input.Parameters.TryGetValue(parameterName, out var value) || value == null)
        {
            throw new ArgumentException($"Required parameter '{parameterName}' is missing");
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
            throw new ArgumentException($"Failed to convert parameter '{parameterName}' to type {typeof(T).Name}", ex);
        }
    }
}