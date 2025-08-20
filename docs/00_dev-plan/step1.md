# 📍 1단계: 추상화 정의 (Core Contracts)

---

## 🎯 목표

* **AI Agent 프레임워크의 최상위 계약(Abstraction Layer) 확립**
* 도구/LLM/오케스트레이션이 공통으로 의존하는 **데이터 구조 + 인터페이스** 정의
* “스트리밍/스키마 기반/Step 기반”을 표준으로 강제

---

## 📦 주요 구성 요소

### 1. 공통 식별자

* `RunId`: 하나의 실행 단위를 구분 (UUID)
* `StepId`: 실행 내 각 Step의 ID
* `Seq`: 순서 보장 및 스트리밍 이벤트 ordering

```csharp
public readonly record struct RunId(Guid Value);
public readonly record struct StepId(string Value);
```

---

### 2. 스트리밍 이벤트 (StreamChunk)

* 모든 실행 결과는 **스트리밍 가능한 조각 단위**로 제공
* 유형별로 명확히 분리

```csharp
public abstract record StreamChunk(RunId RunId, StepId StepId, long Seq);

public sealed record TokenChunk(RunId RunId, StepId StepId, long Seq, string Text) 
  : StreamChunk(RunId, StepId, Seq);

public sealed record ToolCallChunk(RunId RunId, StepId StepId, long Seq, string ToolName, JsonNode Arguments) 
  : StreamChunk(RunId, StepId, Seq);

public sealed record JsonPartialChunk(RunId RunId, StepId StepId, long Seq, string PartialJson) 
  : StreamChunk(RunId, StepId, Seq);

public sealed record StatusChunk(RunId RunId, StepId StepId, long Seq, string Status, string? Message = null) 
  : StreamChunk(RunId, StepId, Seq);

public sealed record FinalChunk(RunId RunId, StepId StepId, long Seq, JsonNode Result) 
  : StreamChunk(RunId, StepId, Seq);
```

---

### 3. Step & Plan

* **Step**: 실행 가능한 최소 단위 (LLM 호출, 도구 호출, 분기, 병렬)
* **Plan**: Step들의 집합 (Fixed / Planner / Reactive)

```csharp
public sealed record Step(
    StepId Id,
    string Kind,            // "llm", "tool", "branch", ...
    JsonNode Input,
    JsonNode? Output = null,
    string? Error = null
);

public sealed record Plan(
    string OrchestrationType,   // simple, fixed, planner, reactive
    IReadOnlyList<Step> Steps
);
```

---

### 4. 도구 추상화 (ITool)

* 모든 도구는 동일한 인터페이스로 제공

```csharp
public interface ITool
{
    ToolDescriptor Describe();
    Task<ToolResult> ExecuteAsync(JsonNode input, ToolContext ctx, CancellationToken ct);
}

public sealed record ToolDescriptor(
    string Provider,        // internal, plugin, mcp
    string Namespace,
    string Name,
    string Version,
    JsonNode InputSchema,
    JsonNode OutputSchema
);

public sealed record ToolResult(bool Success, JsonNode Output, string? Error = null);
```

---

### 5. LLM 추상화 (ILlmClient)

* CallType/Options/Prompt → Streaming Response

```csharp
public interface ILlmClient
{
    IAsyncEnumerable<LlmChunk> CompleteAsync(LlmRequest request, CancellationToken ct);
    LlmCapabilities Capabilities { get; }
}

public sealed record LlmRequest(
    LlmCallType CallType,
    PromptSpec Prompt,
    LlmOptions Options
);

public abstract record LlmChunk(RunId RunId, StepId StepId, long Seq);

public sealed record LlmTokenChunk(RunId RunId, StepId StepId, long Seq, string Text) : LlmChunk(RunId, StepId, Seq);
public sealed record LlmToolCallChunk(RunId RunId, StepId StepId, long Seq, string ToolName, JsonNode Args) : LlmChunk(RunId, StepId, Seq);
public sealed record LlmFinalChunk(RunId RunId, StepId StepId, long Seq, JsonNode Result) : LlmChunk(RunId, StepId, Seq);
```

---

### 6. JSON Schema 검증 계약

* 모든 입력/출력은 **Schema ID**와 함께
* 유효성 보장 → 실패 시 이전 Config/Plan 유지

```csharp
public interface ISchemaValidator
{
    Task<bool> ValidateAsync(JsonNode json, string schemaId, CancellationToken ct);
    Task<JsonNode> CoerceAsync(JsonNode json, string schemaId, CancellationToken ct);
}
```

---

## 📂 디렉토리 배치 (1단계 산출물)

```
src/Abstractions/
  Orchestration/
    Plan.cs
    Step.cs
    StreamChunk.cs
  Tools/
    ITool.cs
    ToolDescriptor.cs
    ToolResult.cs
  Llm/
    ILlmClient.cs
    LlmRequest.cs
    LlmChunk.cs
    LlmOptions.cs
  Common/
    Identifiers.cs
    ISchemaValidator.cs
schemas/core/
  Plan.schema.json
  Step.schema.json
  ToolDescriptor.schema.json
  ToolResult.schema.json
  OrchestrationType.schema.json
```

---

## ✅ 1단계 완료 기준

* [ ] `Abstractions` 프로젝트 컴파일 성공
* [ ] 모든 엔티티(`RunId`, `StepId`, `StreamChunk`, `Step`, `Plan`) 정의
* [ ] `ITool`, `ILlmClient`, `ISchemaValidator` 인터페이스 확정
* [ ] 최소 5개 Schema(JSON) 정의

---
