# 📍 4단계: 오케스트레이션 계층 (Orchestration Layer)

---

## 🎯 목표

* 실행 단위를 **Plan → Step → StreamChunk** 흐름으로 관리
* 요청을 받아서 적절한 **Orchestration Type**을 선택
* 각 타입별 실행 로직(Simple, Fixed, Planner, Reactive)을 분리 구현
* 실행 중간에도 스트리밍으로 사용자에게 응답 전달

---

## 📦 주요 구성 요소

### 1. OrchestrationType Enum

```csharp
public enum OrchestrationType {
    Simple,     // 단순 LLM 호출
    Fixed,      // 사전 정의된 Workflow 실행
    Planner,    // LLM이 Plan(JSON)을 생성 → 실행
    Reactive    // 실행 중간에도 재계획 가능
}
```

---

### 2. Orchestration Selector

* Rule-based 또는 LLM 기반으로 “어떤 타입을 쓸지” 결정
* LLM 기반인 경우 Schema 강제:

```json
{
  "type": "object",
  "required": ["orchestrationType"],
  "properties": {
    "orchestrationType": {
      "enum": ["simple", "fixed", "planner", "reactive"]
    }
  }
}
```

```csharp
public interface IOrchestrationSelector {
    Task<OrchestrationType> SelectAsync(string userInput, CancellationToken ct);
}
```

---

### 3. Step Runner

* **Step 단위 실행 엔진**
* Step의 Kind에 따라 분기:

  * `kind = "llm"` → LLM 호출
  * `kind = "tool"` → ToolRegistry 실행
  * `kind = "branch"` → 조건 분기
  * `kind = "parallel"` → 병렬 실행

```csharp
public interface IStepRunner {
    IAsyncEnumerable<StreamChunk> RunStepAsync(Step step, RunContext ctx, CancellationToken ct);
}
```

---

### 4. Executor (타입별 실행기)

* **SimpleExecutor**

  * 사용자 입력 → LLM → 최종 응답 반환
* **FixedWorkflowExecutor**

  * 미리 정의된 Steps(JSON or DSL) 차례대로 실행
* **PlannerExecutor**

  * LLM이 Plan(JSON)을 생성 → StepRunner로 실행
* **ReactiveExecutor**

  * 실행 중간에 오류나 조건 발생 시 → LLM에게 재계획 요청

```csharp
public interface IExecutor {
    IAsyncEnumerable<StreamChunk> ExecuteAsync(RunContext ctx, CancellationToken ct);
}
```

---

### 5. RunContext

* 실행 전역 상태 저장소

```csharp
public sealed record RunContext(
    RunId RunId,
    OrchestrationType Type,
    IDictionary<string, object> Inputs,
    IToolRegistry ToolRegistry,
    ILlmRouter LlmRouter,
    IDictionary<string, object> Memory
);
```

---

### 6. Streaming Aggregator

* StepRunner/Executor에서 발생한 `StreamChunk`를 모아서 사용자에게 스트리밍 전송
* Chunk 단위: `Token`, `ToolCall`, `JsonPartial`, `Status`, `Final`

---

## 📂 디렉토리 배치 (4단계 산출물)

```
src/Agent.Orchestration/
  OrchestrationType.cs
  IOrchestrationSelector.cs
  IExecutor.cs
  IStepRunner.cs
  RunContext.cs
  StreamingAggregator.cs

  Executors/
    SimpleExecutor.cs
    FixedWorkflowExecutor.cs
    PlannerExecutor.cs
    ReactiveExecutor.cs

  Selectors/
    RuleBasedSelector.cs
    LlmBasedSelector.cs

schemas/
  OrchestrationType.schema.json
  AgentPlan.schema.json
  FixedWorkflow.schema.json
```

---

## ✅ 4단계 완료 기준

* [ ] `OrchestrationType` 정의 및 Schema 완료
* [ ] `IOrchestrationSelector` 구현 (Rule-based + LLM 기반)
* [ ] `IStepRunner` 구현 (`llm`, `tool`, `branch`, `parallel`)
* [ ] `IExecutor` 4종 구현 (Simple, Fixed, Planner, Reactive)
* [ ] `RunContext` 구조 확정 (ToolRegistry + LlmRouter + Memory 포함)
* [ ] Streaming Aggregator로 실행 결과를 순차적으로 반환 가능

---


## 📋 오케스트레이션 계층 구현 보강 사항

### 1. **Orchestration Selector 구체 구현**

#### Rule-based Selector
```csharp
// src/Agent.Orchestration/Selectors/RuleBasedSelector.cs
필요 기능:
- 키워드 기반 매칭 규칙
- 복잡도 점수 계산
- 도구 요구사항 분석
- 의도 분류 (질문/명령/대화)
- 우선순위 기반 규칙 체인
- 규칙 설정 파일 로딩 (YAML/JSON)
```

#### LLM-based Selector
```csharp
// src/Agent.Orchestration/Selectors/LlmBasedSelector.cs
필요 기능:
- LLM 프롬프트 템플릿 관리
- Schema 강제 응답 파싱
- 캐싱 메커니즘
- Fallback 처리
- 신뢰도 점수 계산
- 하이브리드 모드 (Rule + LLM)
```

### 2. **Step Runner 구체 구현**

```csharp
// src/Agent.Orchestration/Runners/DefaultStepRunner.cs
필요 기능:
- Step 타입별 실행 분기
- 의존성 해결 및 검증
- 병렬 실행 오케스트레이션
- 실행 상태 추적
- 중간 결과 저장
- 타임아웃 처리
- 에러 전파 및 복구
```

#### 특화된 Step Runner들
```csharp
// src/Agent.Orchestration/Runners/
- LlmStepRunner.cs (LLM 호출 전문)
- ToolStepRunner.cs (도구 실행 전문)
- BranchStepRunner.cs (조건 분기)
- ParallelStepRunner.cs (병렬 처리)
- LoopStepRunner.cs (반복 실행)
- UserInputStepRunner.cs (사용자 입력 대기)
```

### 3. **Executor 구현 상세**

#### SimpleExecutor
```csharp
// src/Agent.Orchestration/Executors/SimpleExecutor.cs
필요 기능:
- 단일 LLM 호출 최적화
- 스트리밍 직접 전달
- 최소 오버헤드
- 빠른 응답 시작
```

#### FixedWorkflowExecutor
```csharp
// src/Agent.Orchestration/Executors/FixedWorkflowExecutor.cs
필요 기능:
- Workflow 정의 파일 로딩
- DAG 실행 엔진
- 조건부 분기 처리
- 루프 및 반복 지원
- 체크포인트 및 재시작
- 워크플로우 버전 관리
```

#### PlannerExecutor
```csharp
// src/Agent.Orchestration/Executors/PlannerExecutor.cs
필요 기능:
- Plan 생성 프롬프트 관리
- Plan 검증 및 최적화
- 동적 Step 생성
- Plan 수정 및 재계획
- 실행 추적 및 로깅
```

#### ReactiveExecutor
```csharp
// src/Agent.Orchestration/Executors/ReactiveExecutor.cs
필요 기능:
- 실시간 상태 모니터링
- 동적 재계획 트리거
- 이벤트 기반 실행
- 피드백 루프 처리
- 적응형 실행 전략
- 학습 및 개선
```

### 4. **RunContext 확장**

```csharp
// src/Agent.Orchestration/Context/EnhancedRunContext.cs
필요 기능:
- 실행 이력 관리
- 변수 스코프 관리
- 상태 스냅샷
- 이벤트 버스
- 메트릭 수집
- 분산 추적 컨텍스트
```

### 5. **Streaming Aggregator 구현**

```csharp
// src/Agent.Orchestration/Streaming/DefaultStreamingAggregator.cs
필요 기능:
- 다중 소스 스트림 병합
- 버퍼링 및 배치 처리
- 순서 보장
- 압축 및 필터링
- 백프레셔 관리
- 스트림 변환 파이프라인
```

### 6. **실행 조정자 (Coordinator)**

```csharp
// src/Agent.Orchestration/Coordination/ExecutionCoordinator.cs
필요 기능:
- 전체 실행 라이프사이클 관리
- Selector → Executor → Runner 조정
- 리소스 할당 및 관리
- 우선순위 큐 관리
- 동시 실행 제어
- 데드락 감지 및 해결
```

### 7. **상태 관리**

```csharp
// src/Agent.Orchestration/State/StateManager.cs
필요 기능:
- Step 상태 전이 관리
- 실행 상태 영속화
- 상태 복구 메커니즘
- 분산 상태 동기화
- 상태 이벤트 발행
```

### 8. **의존성 해결**

```csharp
// src/Agent.Orchestration/Dependencies/DependencyResolver.cs
필요 기능:
- DAG 구성 및 검증
- 순환 의존성 감지
- 토폴로지 정렬
- 동적 의존성 추가
- 의존성 그래프 시각화
```

### 9. **실행 정책**

```csharp
// src/Agent.Orchestration/Policies/
- RetryPolicy.cs (재시도 정책)
- TimeoutPolicy.cs (타임아웃 정책)
- CircuitBreakerPolicy.cs (서킷 브레이커)
- BulkheadPolicy.cs (격벽 패턴)
- RateLimitPolicy.cs (속도 제한)
```

### 10. **워크플로우 DSL**

```yaml
# samples/workflows/data-analysis.yaml
workflow:
  name: "데이터 분석 워크플로우"
  version: "1.0.0"
  steps:
    - id: load_data
      type: tool
      tool: database.query
      input:
        query: "SELECT * FROM sales"
    
    - id: analyze
      type: llm
      depends_on: [load_data]
      input:
        prompt: "분석 수행"
        context: "${load_data.output}"
    
    - id: visualize
      type: parallel
      depends_on: [analyze]
      steps:
        - tool: chart.create
          input: "${analyze.output.chart_data}"
        - tool: report.generate
          input: "${analyze.output.summary}"
```

### 11. **이벤트 시스템**

```csharp
// src/Agent.Orchestration/Events/
- OrchestrationEvent.cs (기본 이벤트)
- StepStartedEvent.cs
- StepCompletedEvent.cs
- StepFailedEvent.cs
- PlanModifiedEvent.cs
- ExecutionCompletedEvent.cs
```

### 12. **메트릭 및 관찰성**

```csharp
// src/Agent.Orchestration/Observability/
- ExecutionMetrics.cs (실행 메트릭)
- PerformanceTracker.cs (성능 추적)
- ExecutionTracer.cs (분산 추적)
- AuditLogger.cs (감사 로깅)
```

### 13. **최적화 엔진**

```csharp
// src/Agent.Orchestration/Optimization/
- PlanOptimizer.cs (실행 계획 최적화)
- ParallelizationAnalyzer.cs (병렬화 분석)
- ResourceOptimizer.cs (리소스 최적화)
- CostOptimizer.cs (비용 최적화)
```

### 14. **테스트 및 시뮬레이션**

```csharp
// src/Agent.Orchestration.Tests/
- MockExecutor.cs (테스트용 실행기)
- WorkflowSimulator.cs (워크플로우 시뮬레이터)
- StepRecorder.cs (실행 기록)
- ChaosEngine.cs (카오스 테스팅)
```

### 15. **설정 스키마**

```json
// schemas/orchestration/
{
  "orchestration-config.schema.json": {
    "selector": {
      "type": "hybrid",
      "rules": ["path/to/rules.yaml"],
      "llm": {
        "model": "fast",
        "cache": true
      }
    },
    "executor": {
      "maxConcurrency": 10,
      "timeout": "5m",
      "retries": 3
    },
    "streaming": {
      "bufferSize": 1000,
      "flushInterval": "100ms"
    }
  }
}
```

### 16. **에러 처리 및 복구**

```csharp
// src/Agent.Orchestration/Recovery/
- CheckpointManager.cs (체크포인트 관리)
- RecoveryStrategy.cs (복구 전략)
- CompensationHandler.cs (보상 트랜잭션)
- RollbackManager.cs (롤백 관리)
```

### 17. **분산 실행 지원**

```csharp
// src/Agent.Orchestration/Distributed/
- DistributedLock.cs (분산 락)
- WorkQueue.cs (작업 큐)
- NodeCoordinator.cs (노드 조정)
- MessageBus.cs (메시지 버스)
```

### 18. **동적 계획 수정**

```csharp
// src/Agent.Orchestration/Planning/
- PlanModifier.cs (계획 수정)
- StepInjector.cs (Step 동적 삽입)
- BranchPredictor.cs (분기 예측)
- AdaptiveStrategy.cs (적응형 전략)
```

### 19. **실행 시각화**

```csharp
// src/Agent.Orchestration/Visualization/
- ExecutionGraph.cs (실행 그래프)
- ProgressReporter.cs (진행 상황)
- FlowDiagram.cs (플로우 다이어그램)
- MetricsDashboard.cs (메트릭 대시보드)
```

### 20. **통합 테스트 시나리오**

```csharp
// tests/Integration/Orchestration/
필요 시나리오:
- 단순 질의 응답
- 복잡한 다단계 워크플로우
- 병렬 도구 실행
- 실패 및 재시도
- 동적 재계획
- 사용자 입력 대기
- 타임아웃 처리
- 리소스 경합
```

## 우선순위 구현 순서

1. **핵심 실행 엔진**
   - DefaultStepRunner
   - ExecutionCoordinator
   - StateManager

2. **Executor 구현체들**
   - SimpleExecutor
   - FixedWorkflowExecutor
   - PlannerExecutor

3. **Selector 구현**
   - RuleBasedSelector
   - LlmBasedSelector

4. **스트리밍 및 이벤트**
   - StreamingAggregator
   - Event System

5. **고급 기능**
   - ReactiveExecutor
   - 최적화 엔진
   - 분산 실행

이러한 구현을 통해 강력하고 확장 가능한 오케스트레이션 계층을 구축할 수 있습니다.
