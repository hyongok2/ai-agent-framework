using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.Monitoring.Logging;

/// <summary>
/// ILogger 확장 메서드
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// 성능 측정과 함께 로그 기록
    /// </summary>
    public static IDisposable BeginTimedScope(this ILogger logger, string operationName, LogLevel logLevel = LogLevel.Information)
    {
        return new TimedLogScope(logger, operationName, logLevel);
    }

    /// <summary>
    /// 오케스트레이션 컨텍스트와 함께 로그 기록
    /// </summary>
    public static void LogWithOrchestrationContext(this ILogger logger, LogLevel logLevel, string sessionId, string message, params object[] args)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["SessionId"] = sessionId,
            ["Component"] = "Orchestration"
        });

        logger.Log(logLevel, message, args);
    }

    /// <summary>
    /// LLM 컨텍스트와 함께 로그 기록
    /// </summary>
    public static void LogWithLLMContext(this ILogger logger, LogLevel logLevel, string provider, string model, string message, params object[] args)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Provider"] = provider,
            ["Model"] = model,
            ["Component"] = "LLM"
        });

        logger.Log(logLevel, message, args);
    }

    /// <summary>
    /// 도구 실행 컨텍스트와 함께 로그 기록
    /// </summary>
    public static void LogWithToolContext(this ILogger logger, LogLevel logLevel, string toolName, string message, params object[] args)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ToolName"] = toolName,
            ["Component"] = "Tool"
        });

        logger.Log(logLevel, message, args);
    }

    /// <summary>
    /// 상태 저장소 컨텍스트와 함께 로그 기록
    /// </summary>
    public static void LogWithStateContext(this ILogger logger, LogLevel logLevel, string provider, string operation, string message, params object[] args)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["StateProvider"] = provider,
            ["Operation"] = operation,
            ["Component"] = "State"
        });

        logger.Log(logLevel, message, args);
    }

    /// <summary>
    /// 호출자 정보와 함께 디버그 로그 기록
    /// </summary>
    public static void LogDebugWithCaller(this ILogger logger, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Caller"] = $"{fileName}.{memberName}:{lineNumber}"
        });

        logger.LogDebug(message);
    }

    /// <summary>
    /// 성능 임계값과 함께 경고 로그
    /// </summary>
    public static void LogPerformanceWarning(this ILogger logger, string operation, TimeSpan actualTime, TimeSpan threshold)
    {
        if (actualTime > threshold)
        {
            logger.LogWarning("Performance warning: {Operation} took {ActualTime}ms (threshold: {Threshold}ms)",
                operation, actualTime.TotalMilliseconds, threshold.TotalMilliseconds);
        }
    }

    /// <summary>
    /// 구조화된 예외 로그
    /// </summary>
    public static void LogStructuredException(this ILogger logger, Exception exception, string context, object? additionalData = null)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Context"] = context,
            ["ExceptionType"] = exception.GetType().Name,
            ["AdditionalData"] = additionalData ?? new { }
        });

        logger.LogError(exception, "Exception in {Context}: {Message}", context, exception.Message);
    }

    /// <summary>
    /// 메트릭과 함께 로그
    /// </summary>
    public static void LogWithMetrics(this ILogger logger, LogLevel logLevel, string message, Dictionary<string, object> metrics)
    {
        using var scope = logger.BeginScope(metrics);
        logger.Log(logLevel, message);
    }

    /// <summary>
    /// 조건부 로그 (성능을 위해)
    /// </summary>
    public static void LogIf(this ILogger logger, bool condition, LogLevel logLevel, string message, params object[] args)
    {
        if (condition && logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, message, args);
        }
    }
}

/// <summary>
/// 시간 측정을 위한 로그 스코프
/// </summary>
internal class TimedLogScope : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly LogLevel _logLevel;
    private readonly Stopwatch _stopwatch;
    private readonly IDisposable? _scope;

    public TimedLogScope(ILogger logger, string operationName, LogLevel logLevel)
    {
        _logger = logger;
        _operationName = operationName;
        _logLevel = logLevel;
        _stopwatch = Stopwatch.StartNew();

        _scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["StartTime"] = DateTimeOffset.UtcNow
        });

        if (_logger.IsEnabled(_logLevel))
        {
            _logger.Log(_logLevel, "Started operation: {OperationName}", operationName);
        }
    }

    public void Dispose()
    {
        _stopwatch.Stop();

        if (_logger.IsEnabled(_logLevel))
        {
            _logger.Log(_logLevel, "Completed operation: {OperationName} in {Duration}ms",
                _operationName, _stopwatch.ElapsedMilliseconds);
        }

        _scope?.Dispose();
    }
}