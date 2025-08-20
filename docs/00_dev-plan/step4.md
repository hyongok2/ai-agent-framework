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
