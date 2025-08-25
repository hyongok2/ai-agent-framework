using System.Collections.Generic;
using Agent.Abstractions.Core.Common.Identifiers;
using Agent.Abstractions.LLM.Core;
using Agent.Abstractions.Tools.Registry;
using Agent.Abstractions.Orchestration.Configuration;

namespace Agent.Abstractions.Orchestration.Execution;

/// <summary>
/// 실행 전역 상태 저장소
/// </summary>
public sealed record RunContext
{
    /// <summary>
    /// 실행 ID
    /// </summary>
    public required RunId RunId { get; init; }
    
    /// <summary>
    /// 오케스트레이션 타입
    /// </summary>
    public required OrchestrationType Type { get; init; }
    
    /// <summary>
    /// 입력 데이터
    /// </summary>
    public required IDictionary<string, object> Inputs { get; init; }
    
    /// <summary>
    /// 도구 레지스트리
    /// </summary>
    public required IToolRegistry ToolRegistry { get; init; }
    
    /// <summary>
    /// LLM 클라이언트 레지스트리
    /// </summary>
    public required ILlmRegistry LlmRegistry { get; init; }
    
    /// <summary>
    /// 메모리/상태 저장소
    /// </summary>
    public IDictionary<string, object> Memory { get; init; } = new Dictionary<string, object>();
}