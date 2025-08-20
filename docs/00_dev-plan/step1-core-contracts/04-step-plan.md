# Phase 4: Step & Plan 구조

## 목표
실행 단위(Step)와 실행 계획(Plan) 구조 정의

## 구현 파일

### 1. Orchestration/Step.cs

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using Agent.Core.Abstractions.Common;

namespace Agent.Core.Abstractions.Orchestration;

/// <summary>
/// 실행 가능한 최소 단위
/// </summary>
public sealed record Step
{
    public required StepId Id { get; init; }
    public required StepKind Kind { get; init; }
    public required JsonDocument Input { get; init; }
    public JsonDocument? Output { get; init; }
    public string? Error { get; init; }
    public StepStatus Status { get; init; } = StepStatus.Pending;
    
    /// <summary>
    /// 단계 이름 (사용자 친화적)
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// 단계 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 재시도 정책
    /// </summary>
    public RetryPolicy? RetryPolicy { get; init; }
    
    /// <summary>
    /// 타임아웃 설정
    /// </summary>
    public TimeSpan? Timeout { get; init; }
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 의존성 Step ID 목록
    /// </summary>
    public IReadOnlyList<StepId> Dependencies { get; init; } = Array.Empty<StepId>();
    
    /// <summary>
    /// 실행 시작 시간
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }
    
    /// <summary>
    /// 실행 완료 시간
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// Step 종류
/// </summary>
public enum StepKind
{
    /// <summary>LLM 호출</summary>
    LlmCall,
    
    /// <summary>도구 호출</summary>
    ToolCall,
    
    /// <summary>조건 분기</summary>
    Branch,
    
    /// <summary>병렬 실행</summary>
    Parallel,
    
    /// <summary>순차 실행</summary>
    Sequential,
    
    /// <summary>루프 실행</summary>
    Loop,
    
    /// <summary>대기</summary>
    Wait,
    
    /// <summary>사용자 입력</summary>
    UserInput
}

/// <summary>
/// Step 상태
/// </summary>
public enum StepStatus
{
    /// <summary>대기 중</summary>
    Pending,
    
    /// <summary>실행 중</summary>
    Running,
    
    /// <summary>완료</summary>
    Completed,
    
    /// <summary>실패</summary>
    Failed,
    
    /// <summary>건너뜀</summary>
    Skipped,
    
    /// <summary>취소됨</summary>
    Cancelled,
    
    /// <summary>재시도 중</summary>
    Retrying
}

/// <summary>
/// 재시도 정책
/// </summary>
public sealed record RetryPolicy
{
    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxAttempts { get; init; } = 3;
    
    /// <summary>
    /// 재시도 간격 전략
    /// </summary>
    public RetryStrategy Strategy { get; init; } = RetryStrategy.ExponentialBackoff;
    
    /// <summary>
    /// 초기 대기 시간
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// 최대 대기 시간
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// 재시도 가능한 에러 코드
    /// </summary>
    public IReadOnlyList<string> RetryableErrors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 재시도 전략
/// </summary>
public enum RetryStrategy
{
    /// <summary>고정 간격</summary>
    Fixed,
    
    /// <summary>선형 증가</summary>
    Linear,
    
    /// <summary>지수 백오프</summary>
    ExponentialBackoff
}
```

### 2. Orchestration/Plan.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Agent.Core.Abstractions.Common;

namespace Agent.Core.Abstractions.Orchestration;

/// <summary>
/// 실행 계획 (Step들의 집합)
/// </summary>
public sealed record Plan
{
    public required string Id { get; init; }
    public required OrchestrationType Type { get; init; }
    public required IReadOnlyList<Step> Steps { get; init; }
    
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
    /// Step 실행 순서 검증
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
    /// 다음 실행 가능한 Step 찾기
    /// </summary>
    public IEnumerable<Step> GetExecutableSteps(ISet<StepId> completedSteps)
    {
        return Steps.Where(step =>
            step.Status == StepStatus.Pending &&
            step.Dependencies.All(dep => completedSteps.Contains(dep))
        );
    }
}

/// <summary>
/// 오케스트레이션 타입
/// </summary>
public enum OrchestrationType
{
    /// <summary>단순 실행</summary>
    Simple,
    
    /// <summary>고정 워크플로우</summary>
    Fixed,
    
    /// <summary>동적 계획</summary>
    Planner,
    
    /// <summary>반응형 실행</summary>
    Reactive
}

/// <summary>
/// 계획 상태
/// </summary>
public enum PlanStatus
{
    /// <summary>준비됨</summary>
    Ready,
    
    /// <summary>실행 중</summary>
    Running,
    
    /// <summary>일시정지</summary>
    Paused,
    
    /// <summary>완료</summary>
    Completed,
    
    /// <summary>실패</summary>
    Failed,
    
    /// <summary>취소됨</summary>
    Cancelled
}

/// <summary>
/// 계획 설정
/// </summary>
public sealed record PlanSettings
{
    /// <summary>
    /// 최대 실행 시간
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }
    
    /// <summary>
    /// 병렬 실행 최대 개수
    /// </summary>
    public int MaxParallelSteps { get; init; } = 5;
    
    /// <summary>
    /// 실패 시 중단 여부
    /// </summary>
    public bool StopOnFirstFailure { get; init; } = true;
    
    /// <summary>
    /// 재시도 정책 (전역)
    /// </summary>
    public RetryPolicy? DefaultRetryPolicy { get; init; }
    
    /// <summary>
    /// 로깅 레벨
    /// </summary>
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
}

/// <summary>
/// 계획 실행 결과
/// </summary>
public sealed record PlanResult
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// 완료된 Step 수
    /// </summary>
    public int CompletedSteps { get; init; }
    
    /// <summary>
    /// 실패한 Step 수
    /// </summary>
    public int FailedSteps { get; init; }
    
    /// <summary>
    /// 건너뛴 Step 수
    /// </summary>
    public int SkippedSteps { get; init; }
    
    /// <summary>
    /// 총 실행 시간
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// 최종 출력
    /// </summary>
    public JsonDocument? FinalOutput { get; init; }
    
    /// <summary>
    /// 에러 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 로그 레벨
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
```

### 3. Orchestration/PlanBuilder.cs

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using Agent.Core.Abstractions.Common;

namespace Agent.Core.Abstractions.Orchestration;

/// <summary>
/// Plan을 구성하는 빌더
/// </summary>
public class PlanBuilder
{
    private readonly List<Step> _steps = new();
    private string _id = Guid.NewGuid().ToString();
    private OrchestrationType _type = OrchestrationType.Fixed;
    private string? _name;
    private string? _description;
    private readonly Dictionary<string, object> _context = new();
    private PlanSettings _settings = new();
    
    public PlanBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    
    public PlanBuilder WithType(OrchestrationType type)
    {
        _type = type;
        return this;
    }
    
    public PlanBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public PlanBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    public PlanBuilder AddStep(Step step)
    {
        _steps.Add(step);
        return this;
    }
    
    public PlanBuilder AddLlmStep(string prompt, StepId? dependsOn = null)
    {
        var step = new Step
        {
            Id = StepId.New(_steps.Count + 1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse($"{{\"prompt\": \"{prompt}\"}}"),
            Dependencies = dependsOn != null ? new[] { dependsOn.Value } : Array.Empty<StepId>()
        };
        
        return AddStep(step);
    }
    
    public PlanBuilder AddToolStep(string toolName, JsonDocument arguments, StepId? dependsOn = null)
    {
        var step = new Step
        {
            Id = StepId.New(_steps.Count + 1),
            Kind = StepKind.ToolCall,
            Input = JsonDocument.Parse($"{{\"tool\": \"{toolName}\", \"arguments\": {arguments.RootElement}}}"),
            Dependencies = dependsOn != null ? new[] { dependsOn.Value } : Array.Empty<StepId>()
        };
        
        return AddStep(step);
    }
    
    public PlanBuilder AddParallelSteps(params Action<PlanBuilder>[] stepBuilders)
    {
        var parallelSteps = new List<Step>();
        
        foreach (var builder in stepBuilders)
        {
            var subBuilder = new PlanBuilder();
            builder(subBuilder);
            parallelSteps.AddRange(subBuilder._steps);
        }
        
        var parallelStep = new Step
        {
            Id = StepId.New(_steps.Count + 1),
            Kind = StepKind.Parallel,
            Input = JsonDocument.Parse($"{{\"steps\": {JsonSerializer.Serialize(parallelSteps)}}}")
        };
        
        return AddStep(parallelStep);
    }
    
    public PlanBuilder WithContext(string key, object value)
    {
        _context[key] = value;
        return this;
    }
    
    public PlanBuilder WithSettings(Action<PlanSettings> configure)
    {
        var settings = new PlanSettings();
        configure(settings);
        _settings = settings;
        return this;
    }
    
    public Plan Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException("Plan must have at least one step");
        
        var plan = new Plan
        {
            Id = _id,
            Type = _type,
            Steps = _steps.ToArray(),
            Name = _name,
            Description = _description,
            Context = _context,
            Settings = _settings
        };
        
        if (!plan.ValidateExecutionOrder())
            throw new InvalidOperationException("Invalid execution order: some dependencies are not defined");
        
        return plan;
    }
}
```

### 4. Tests/Orchestration/PlanTests.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Agent.Core.Abstractions.Common;
using Agent.Core.Abstractions.Orchestration;

namespace Agent.Core.Abstractions.Tests.Orchestration;

public class PlanTests
{
    [Fact]
    public void Plan_Should_Validate_Execution_Order()
    {
        // Arrange
        var step1 = new Step
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        var step2 = new Step
        {
            Id = StepId.New(2),
            Kind = StepKind.ToolCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(1) }
        };
        
        var plan = new Plan
        {
            Id = "test-plan",
            Type = OrchestrationType.Fixed,
            Steps = new[] { step1, step2 }
        };
        
        // Act
        var isValid = plan.ValidateExecutionOrder();
        
        // Assert
        isValid.Should().BeTrue();
    }
    
    [Fact]
    public void Plan_Should_Detect_Invalid_Dependencies()
    {
        // Arrange
        var step1 = new Step
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(99) } // Non-existent dependency
        };
        
        var plan = new Plan
        {
            Id = "test-plan",
            Type = OrchestrationType.Fixed,
            Steps = new[] { step1 }
        };
        
        // Act
        var isValid = plan.ValidateExecutionOrder();
        
        // Assert
        isValid.Should().BeFalse();
    }
    
    [Fact]
    public void GetExecutableSteps_Should_Return_Ready_Steps()
    {
        // Arrange
        var step1 = new Step
        {
            Id = StepId.New(1),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        var step2 = new Step
        {
            Id = StepId.New(2),
            Kind = StepKind.ToolCall,
            Input = JsonDocument.Parse("{}"),
            Dependencies = new[] { StepId.New(1) }
        };
        
        var step3 = new Step
        {
            Id = StepId.New(3),
            Kind = StepKind.LlmCall,
            Input = JsonDocument.Parse("{}")
        };
        
        var plan = new Plan
        {
            Id = "test-plan",
            Type = OrchestrationType.Fixed,
            Steps = new[] { step1, step2, step3 }
        };
        
        // Act
        var executableSteps = plan.GetExecutableSteps(new HashSet<StepId>()).ToList();
        
        // Assert
        executableSteps.Should().HaveCount(2); // step1 and step3
        executableSteps.Should().Contain(s => s.Id == step1.Id);
        executableSteps.Should().Contain(s => s.Id == step3.Id);
    }
    
    [Fact]
    public void PlanBuilder_Should_Create_Valid_Plan()
    {
        // Act
        var plan = new PlanBuilder()
            .WithId("builder-plan")
            .WithName("Test Plan")
            .WithType(OrchestrationType.Planner)
            .AddLlmStep("Generate summary")
            .AddToolStep("calculator", JsonDocument.Parse("{\"operation\": \"add\"}"))
            .WithContext("user", "test-user")
            .Build();
        
        // Assert
        plan.Id.Should().Be("builder-plan");
        plan.Name.Should().Be("Test Plan");
        plan.Type.Should().Be(OrchestrationType.Planner);
        plan.Steps.Should().HaveCount(2);
        plan.Context["user"].Should().Be("test-user");
    }
}
```

## 체크리스트

- [ ] Step 구조 구현
- [ ] Plan 구조 구현
- [ ] RetryPolicy 구현
- [ ] PlanBuilder 구현
- [ ] 실행 순서 검증 로직
- [ ] 단위 테스트 작성
- [ ] 문서화 완료

## 다음 단계
Phase 5: 도구 추상화