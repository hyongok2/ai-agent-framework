using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Agent.Core.Abstractions.Common.Identifiers;
using Agent.Core.Abstractions.Orchestration.Configuration;
using Agent.Core.Abstractions.Orchestration.Execution;

namespace Agent.Core.Abstractions.Orchestration.Plans;

/// <summary>
/// 실행 계획 (ExecutionStep들의 집합)
/// </summary>
public sealed record Plan
{
    public required string Id { get; init; }
    public required OrchestrationType Type { get; init; }
    public required IReadOnlyList<ExecutionStep> Steps { get; init; }
    
    /// <summary>
    /// 계획 이름
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// 계획 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 계획 버전
    /// </summary>
    public string Version { get; init; } = "1.0.0";
    
    /// <summary>
    /// 실행 컨텍스트
    /// </summary>
    public IDictionary<string, object> Context { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 전역 설정
    /// </summary>
    public PlanSettings Settings { get; init; } = new();
    
    /// <summary>
    /// 생성 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 예상 실행 시간
    /// </summary>
    public TimeSpan? EstimatedDuration { get; init; }
    
    /// <summary>
    /// 계획 상태
    /// </summary>
    public PlanStatus Status { get; init; } = PlanStatus.Ready;
    
    /// <summary>
    /// 실행 결과 요약
    /// </summary>
    public PlanResult? Result { get; init; }
    
    /// <summary>
    /// ExecutionStep 실행 순서 검증
    /// </summary>
    public bool ValidateExecutionOrder()
    {
        var stepIds = new HashSet<StepId>(Steps.Select(s => s.Id));
        
        foreach (var step in Steps)
        {
            foreach (var dependency in step.Dependencies)
            {
                if (!stepIds.Contains(dependency))
                    return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 다음 실행 가능한 ExecutionStep 찾기
    /// </summary>
    public IEnumerable<ExecutionStep> GetExecutableSteps(ISet<StepId> completedSteps)
    {
        return Steps.Where(step =>
            step.Status == StepStatus.Pending &&
            step.Dependencies.All(dep => completedSteps.Contains(dep))
        );
    }
}


