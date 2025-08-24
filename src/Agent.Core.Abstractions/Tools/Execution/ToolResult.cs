using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Agent.Core.Abstractions.Tools.Execution;

/// <summary>
/// 도구 실행 결과
/// </summary>
public sealed record ToolResult
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// 출력 데이터
    /// </summary>
    public JsonNode? Output { get; init; }
    
    /// <summary>
    /// 에러 메시지
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// 에러 코드
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// 실행 메트릭
    /// </summary>
    public ToolExecutionMetrics? Metrics { get; init; }
    
    /// <summary>
    /// 경고 메시지
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    public static ToolResult CreateSuccess(JsonNode? output = null, ToolExecutionMetrics? metrics = null)
        => new() { Success = true, Output = output, Metrics = metrics };
    
    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    public static ToolResult CreateFailure(string error, string? errorCode = null, ToolExecutionMetrics? metrics = null)
        => new() { Success = false, Error = error, ErrorCode = errorCode, Metrics = metrics };
}