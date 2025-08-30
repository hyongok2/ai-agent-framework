using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Monitoring.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AIAgentFramework.Monitoring.Orchestration;

/// <summary>
/// 텔레메트리 통합 오케스트레이션 엔진
/// 기존 오케스트레이션 엔진을 래핑하여 텔레메트리 기능 추가
/// </summary>
public class TelemetryOrchestrationEngine : IOrchestrationEngine
{
    private readonly IOrchestrationEngine _innerEngine;
    private readonly TelemetryCollector _telemetryCollector;
    private readonly ActivitySourceManager _activitySourceManager;
    private readonly ILogger<TelemetryOrchestrationEngine> _logger;

    public TelemetryOrchestrationEngine(
        IOrchestrationEngine innerEngine,
        TelemetryCollector telemetryCollector,
        ActivitySourceManager activitySourceManager,
        ILogger<TelemetryOrchestrationEngine> logger)
    {
        _innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
        _telemetryCollector = telemetryCollector ?? throw new ArgumentNullException(nameof(telemetryCollector));
        _activitySourceManager = activitySourceManager ?? throw new ArgumentNullException(nameof(activitySourceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("TelemetryOrchestrationEngine initialized, wrapping: {InnerEngineType}",
            _innerEngine.GetType().Name);
    }

    /// <inheritdoc />
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString();
        var operationType = "execute_user_request";
        
        // Activity 시작
        using var activity = _activitySourceManager.StartOrchestrationActivity(sessionId, operationType);
        
        IOrchestrationResult? result = null;
        Exception? exception = null;
        
        try
        {
            // 요청 정보를 Activity에 추가
            if (activity != null)
            {
                activity.SetTag("request.type", request.GetType().Name);
                
                // 사용자 요청 내용 (보안상 요약)
                if (request is IUserRequest userRequest)
                {
                    var content = userRequest.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var contentSummary = content.Length > 200 ? content[..200] + "..." : content;
                        activity.SetTag("request.content_summary", contentSummary);
                        activity.SetTag("request.content_length", content.Length);
                    }
                }
            }

            _logger.LogInformation("Starting orchestration execution: {SessionId}", sessionId);
            
            // 내부 엔진 실행
            result = await _innerEngine.ExecuteAsync(request);
            
            stopwatch.Stop();
            
            if (result != null)
            {
                // 성공 상태 설정
                ActivitySourceManager.SetActivityResult(activity, result.IsCompleted);
                
                // 결과 정보를 Activity에 추가
                if (activity != null)
                {
                    activity.SetTag("result.is_completed", result.IsCompleted);
                    activity.SetTag("result.session_id", result.SessionId);
                    
                    if (result.ExecutionSteps != null)
                    {
                        activity.SetTag("result.step_count", result.ExecutionSteps.Count);
                    }
                    
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        activity.SetTag("result.error_message", result.ErrorMessage);
                    }
                }

                // 메트릭 기록
                _telemetryCollector.RecordRequest(
                    operationType, 
                    result.IsCompleted, 
                    stopwatch.Elapsed, 
                    new Dictionary<string, object>
                    {
                        ["session_id"] = result.SessionId,
                        ["step_count"] = result.ExecutionSteps?.Count ?? 0
                    });

                // 오케스트레이션 실행 메트릭
                _telemetryCollector.RecordOrchestrationExecution(
                    result.SessionId, 
                    result.IsCompleted, 
                    result.ExecutionSteps?.Count ?? 0);

                _logger.LogInformation("Orchestration execution completed: {SessionId}, Success: {Success}, Duration: {Duration}ms",
                    result.SessionId, result.IsCompleted, stopwatch.ElapsedMilliseconds);
            }
                
            return result!;
        }
        catch (Exception ex)
        {
            exception = ex;
            stopwatch.Stop();
            
            // 실패 상태 설정
            ActivitySourceManager.SetActivityResult(activity, false, ex);
            
            // 에러 메트릭 기록
            _telemetryCollector.RecordRequest(
                operationType, 
                false, 
                stopwatch.Elapsed, 
                new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message
                });

            _logger.LogError(ex, "Orchestration execution failed: {SessionId}, Duration: {Duration}ms",
                sessionId, stopwatch.ElapsedMilliseconds);
                
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = context.SessionId ?? Guid.NewGuid().ToString();
        var operationType = "continue_orchestration";
        
        // Activity 시작
        using var activity = _activitySourceManager.StartOrchestrationActivity(sessionId, operationType);
        
        IOrchestrationResult? result = null;
        Exception? exception = null;
        
        try
        {
            // 컨텍스트 정보를 Activity에 추가
            if (activity != null)
            {
                activity.SetTag("context.session_id", context.SessionId);
                activity.SetTag("context.type", context.GetType().Name);
                
                // 기존 실행 단계 수
                if (context.ExecutionHistory != null)
                {
                    activity.SetTag("context.existing_steps", context.ExecutionHistory.Count);
                }
            }

            _logger.LogInformation("Continuing orchestration execution: {SessionId}", sessionId);
            
            // 내부 엔진에서 계속 실행
            result = await _innerEngine.ContinueAsync(context);
            
            stopwatch.Stop();
            
            if (result != null)
            {
                // 성공 상태 설정
                ActivitySourceManager.SetActivityResult(activity, result.IsCompleted);
                
                // 결과 정보를 Activity에 추가
                if (activity != null)
                {
                    activity.SetTag("result.is_completed", result.IsCompleted);
                    activity.SetTag("result.session_id", result.SessionId);
                    
                    if (result.ExecutionSteps != null)
                    {
                        activity.SetTag("result.total_steps", result.ExecutionSteps.Count);
                    }
                }

                // 메트릭 기록
                _telemetryCollector.RecordRequest(
                    operationType, 
                    result.IsCompleted, 
                    stopwatch.Elapsed, 
                    new Dictionary<string, object>
                    {
                        ["session_id"] = result.SessionId,
                        ["total_steps"] = result.ExecutionSteps?.Count ?? 0
                    });

                _logger.LogInformation("Orchestration continuation completed: {SessionId}, Success: {Success}, Duration: {Duration}ms",
                    result.SessionId, result.IsCompleted, stopwatch.ElapsedMilliseconds);
            }
                
            return result!;
        }
        catch (Exception ex)
        {
            exception = ex;
            stopwatch.Stop();
            
            // 실패 상태 설정
            ActivitySourceManager.SetActivityResult(activity, false, ex);
            
            // 에러 메트릭 기록
            _telemetryCollector.RecordRequest(
                operationType, 
                false, 
                stopwatch.Elapsed, 
                new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message
                });

            _logger.LogError(ex, "Orchestration continuation failed: {SessionId}, Duration: {Duration}ms",
                sessionId, stopwatch.ElapsedMilliseconds);
                
            throw;
        }
    }
}