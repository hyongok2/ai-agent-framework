using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agent.Abstractions.Core.Common.Identifiers;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 응답 모델
/// </summary>
public sealed record AgentResponse
{
    /// <summary>
    /// 응답 ID
    /// </summary>
    public string ResponseId { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// 요청 ID (추적용)
    /// </summary>
    public required string RequestId { get; init; }
    
    /// <summary>
    /// 응답 내용
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// 대화 ID
    /// </summary>
    public string? ConversationId { get; init; }
    
    /// <summary>
    /// 응답 형식
    /// </summary>
    public ResponseFormat Format { get; init; } = ResponseFormat.Text;
    
    /// <summary>
    /// 응답 메타데이터
    /// </summary>
    public ResponseMetadata Metadata { get; init; } = new();
    
    /// <summary>
    /// 실행된 도구 호출
    /// </summary>
    public List<ToolCallResult> ToolCalls { get; init; } = new();
    
    /// <summary>
    /// 실행 단계
    /// </summary>
    public List<ExecutionStepResult> ExecutionSteps { get; init; } = new();
    
    /// <summary>
    /// 응답 컨텍스트
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
    
    /// <summary>
    /// 사용량 정보
    /// </summary>
    public UsageInfo? Usage { get; init; }
    
    /// <summary>
    /// 성능 메트릭
    /// </summary>
    public PerformanceMetrics? Performance { get; init; }
    
    /// <summary>
    /// 에러 정보 (실패 시)
    /// </summary>
    public ErrorInfo? Error { get; init; }
    
    /// <summary>
    /// 경고 메시지
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// 성공 여부
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Error == null;
    
    /// <summary>
    /// 응답 생성 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 응답 완료 시간
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 총 소요 시간
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => CompletedAt - CreatedAt;
    
    /// <summary>
    /// 성공 응답 생성
    /// </summary>
    public static AgentResponse CreateSuccess(
        string requestId,
        string content,
        ResponseMetadata? metadata = null)
        => new()
        {
            RequestId = requestId,
            Content = content,
            Metadata = metadata ?? new ResponseMetadata()
        };
    
    /// <summary>
    /// 에러 응답 생성
    /// </summary>
    public static AgentResponse CreateError(
        string requestId,
        string errorMessage,
        string? errorCode = null)
        => new()
        {
            RequestId = requestId,
            Content = string.Empty,
            Error = new ErrorInfo
            {
                Message = errorMessage,
                Code = errorCode ?? "UNKNOWN_ERROR"
            }
        };
    
    /// <summary>
    /// JSON 응답 생성
    /// </summary>
    public static AgentResponse CreateJson<T>(
        string requestId,
        T data,
        ResponseMetadata? metadata = null)
        => new()
        {
            RequestId = requestId,
            Content = JsonSerializer.Serialize(data, JsonOptions.Default),
            Format = ResponseFormat.Json,
            Metadata = metadata ?? new ResponseMetadata()
        };
    
    /// <summary>
    /// 도구 실행 결과를 포함한 응답 생성
    /// </summary>
    public static AgentResponse CreateWithTools(
        string requestId,
        string content,
        params ToolCallResult[] toolCalls)
        => new()
        {
            RequestId = requestId,
            Content = content,
            ToolCalls = new List<ToolCallResult>(toolCalls)
        };
    
    /// <summary>
    /// JSON으로 직렬화
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(this, options ?? JsonOptions.Default);
    
    /// <summary>
    /// JSON에서 역직렬화
    /// </summary>
    public static AgentResponse FromJson(string json, JsonSerializerOptions? options = null)
        => JsonSerializer.Deserialize<AgentResponse>(json, options ?? JsonOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize AgentResponse");
    
    /// <summary>
    /// 구조화된 데이터 추출 (JSON 형식인 경우)
    /// </summary>
    public T? ExtractData<T>() where T : class
    {
        if (Format != ResponseFormat.Json || string.IsNullOrWhiteSpace(Content))
            return null;
        
        try
        {
            return JsonSerializer.Deserialize<T>(Content, JsonOptions.Default);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 요약 생성
    /// </summary>
    public string GetSummary(int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(Content))
            return Error?.Message ?? "Empty response";
        
        if (Content.Length <= maxLength)
            return Content;
        
        return string.Concat(Content.AsSpan(0, maxLength - 3), "...");
    }
    
    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}