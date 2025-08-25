using System;
using System.Collections.Generic;
using System.Text.Json;
using Agent.Abstractions.Core.Common.Identifiers;
using Agent.Abstractions.Orchestration.Configuration;
using Agent.Abstractions.Orchestration.Execution;
using Agent.Abstractions.Orchestration.Plans;

namespace Agent.Abstractions.Orchestration.Core;

/// <summary>
/// 계획 생성 인터페이스
/// </summary>
public interface IPlanBuilder
{
    /// <summary>
    /// 계획 ID 설정
    /// </summary>
    IPlanBuilder WithId(string id);
    
    /// <summary>
    /// 계획 이름 설정
    /// </summary>
    IPlanBuilder WithName(string name);
    
    /// <summary>
    /// 계획 설명 설정
    /// </summary>
    IPlanBuilder WithDescription(string description);
    
    /// <summary>
    /// 오케스트레이션 타입 설정
    /// </summary>
    IPlanBuilder WithType(OrchestrationType type);
    
    /// <summary>
    /// 실행 단계 추가
    /// </summary>
    IPlanBuilder AddStep(ExecutionStep executionStep);
    
    /// <summary>
    /// LLM 호출 단계 추가
    /// </summary>
    IPlanBuilder AddLlmStep(string prompt, StepId? dependsOn = null);
    
    /// <summary>
    /// 도구 실행 단계 추가
    /// </summary>
    IPlanBuilder AddToolStep(string toolName, JsonDocument arguments, StepId? dependsOn = null);
    
    /// <summary>
    /// 병렬 실행 단계 추가
    /// </summary>
    IPlanBuilder AddParallelSteps(params Action<IPlanBuilder>[] stepBuilders);
    
    /// <summary>
    /// 컨텍스트 변수 추가
    /// </summary>
    IPlanBuilder WithContext(string key, object value);
    
    /// <summary>
    /// 설정 구성
    /// </summary>
    IPlanBuilder WithSettings(Action<PlanSettings> configure);
    
    /// <summary>
    /// 계획 생성
    /// </summary>
    Plan Build();
}