# 📍 3단계: LLM 계층 (LLM Layer)

---

## 🎯 목표

* LLM 호출을 **CallType 단위**로 표준화
* 공급자(OpenAI, Claude, Ollama 등)별 API 차이를 흡수 → 같은 추상화 인터페이스 제공
* **Prompt + Schema + Model Profile** 조합으로 완전한 호출 스펙 구성
* **Streaming 이벤트** 기반 응답 구조화

---

## 📦 주요 구성 요소

### 1. CallType (LLM 호출 유형)

* 용도에 따라 세분화된 호출 타입 정의

```csharp
public enum LlmCallType {
    ChatGenerate,      // 일반 대화/텍스트 생성
    ChatJSON,          // JSON Schema 기반 구조화 응답
    Plan,              // 실행 플랜(JSON) 생성
    ToolCallSuggest,   // 함수/도구 호출 제안
    ToolCallLoop,      // 도구 호출 루프
    Rerank,            // 후보 재순위
    Judge,             // 평가/채점
    VisionQA,          // 이미지 입력 Q&A
    VisionExtract,     // 이미지→JSON 추출
    Embed,             // 벡터 임베딩
    Reasoning,         // 고도 추론
    CodeGen            // 코드 생성
}
```

---

### 2. Capabilities (모델 능력 플래그)

* 모델별 지원 범위 차이를 플래그로 캡슐화

```csharp
[Flags]
public enum LlmCapabilities {
    StreamTokens = 1 << 0,
    JsonSchema   = 1 << 1,
    ToolUse      = 1 << 2,
    Vision       = 1 << 3,
    AudioIn      = 1 << 4,
    AudioOut     = 1 << 5,
    Reasoning    = 1 << 6,
    Batch        = 1 << 7,
    LongContext  = 1 << 8
}
```

---

### 3. LLM Request / Options

* 모든 호출은 **CallType + Prompt + Options** 조합으로 구성

```csharp
public sealed record LlmRequest(
    LlmCallType CallType,
    PromptSpec Prompt,
    LlmOptions Options
);

public sealed record LlmOptions(
    string? ModelProfile = null,   // fast / analytic / local 등
    double? Temperature = null,
    int? MaxOutputTokens = null,
    TimeSpan? Deadline = null,
    string[]? Guardrails = null
);

public sealed record PromptSpec(
    string PromptTemplateId,       // prompts/plan/agent-plan.hbs
    IDictionary<string, object> Variables,
    string? SchemaRef = null       // schemas/AgentPlan.schema.json
);
```

---

### 4. LLM Chunk (Streaming 응답)

* LLM 응답도 스트리밍 단위(`LlmChunk`)로 흘러나옴

```csharp
public abstract record LlmChunk(RunId RunId, StepId StepId, long Seq);

public sealed record LlmTokenChunk(RunId RunId, StepId StepId, long Seq, string Text) : LlmChunk(RunId, StepId, Seq);
public sealed record LlmJsonPartialChunk(RunId RunId, StepId StepId, long Seq, string PartialJson) : LlmChunk(RunId, StepId, Seq);
public sealed record LlmToolCallChunk(RunId RunId, StepId StepId, long Seq, string ToolName, JsonNode Args) : LlmChunk(RunId, StepId, Seq);
public sealed record LlmFinalChunk(RunId RunId, StepId StepId, long Seq, JsonNode Result) : LlmChunk(RunId, StepId, Seq);
```

---

### 5. ILlmClient (표준 인터페이스)

* 모든 LLM Provider가 구현해야 하는 계약

```csharp
public interface ILlmClient
{
    IAsyncEnumerable<LlmChunk> CompleteAsync(LlmRequest request, CancellationToken ct);
    LlmCapabilities Capabilities { get; }
}
```

---

### 6. Model Profiles (라우팅 단위)

* `configs/model-profiles/` 디렉토리에 JSON으로 정의
* 예시: `fast.json`

```json
{
  "description": "빠른 응답용 소형 모델",
  "provider": "openai",
  "model": "gpt-4o-mini",
  "temperature": 0.7,
  "maxOutputTokens": 512
}
```

* 예시: `local.json`

```json
{
  "description": "로컬 GPU 모델",
  "provider": "ollama",
  "model": "qwen2.5:14b-instruct-q5",
  "endpoint": "http://localhost:11434",
  "temperature": 0.2
}
```

---

### 7. Router

* `(CallType, ModelProfile)`을 보고 실제 모델을 선택
* Capabilities 검사 후 불일치 시 fallback 처리

```csharp
public interface ILlmRouter
{
    ILlmClient Resolve(LlmRequest request);
}
```

---

### 8. Prompt Engine

* 템플릿(`.liquid` 또는 `.scriban`) + 변수 바인딩 → 최종 문자열
* SchemaRef 있는 경우 → "JSON only" directive 자동 삽입
* Few-shot 예시 지원 (`samples/fewshots/…`)

```csharp
public interface IPromptEngine
{
    string Render(PromptSpec spec);
}
```

---

### 9. Schema Validator (연계)

* `ChatJSON`, `Plan`, `ToolCallSuggest` 등은 **Schema 검증 필수**
* Autofix 모드: LLM 응답이 잘못된 JSON일 경우 → 다시 LLM에 수정 요청

---

## 📂 디렉토리 배치 (3단계 산출물)

```
src/Agent.Llm.Abstractions/
  ILlmClient.cs
  LlmRequest.cs
  LlmOptions.cs
  LlmChunk.cs
  LlmCapabilities.cs
  LlmCallType.cs
  ILlmRouter.cs
  IPromptEngine.cs

src/Agent.Llm.OpenAI/
  OpenAiClient.cs
  OpenAiPromptEngine.cs

src/Agent.Llm.Claude/
  ClaudeClient.cs

src/Agent.Llm.Ollama/
  OllamaClient.cs

configs/model-profiles/
  fast.json
  analytic.json
  local.json
  secure.json

samples/prompts/
  plan/agent-plan.liquid
  tool/suggest.liquid
  extract/fields.liquid

samples/fewshots/
  classify/colors.json
  summarize/meeting.json
```

---

## ✅ 3단계 완료 기준

* [ ] `LlmCallType`, `LlmCapabilities` 정의
* [ ] `ILlmClient`, `ILlmRouter`, `IPromptEngine` 계약 확정
* [ ] Model Profiles 최소 3개 작성 (`fast`, `analytic`, `local`)
* [ ] OpenAI Adapter에서 최소 3 CallType 동작 (`ChatGenerate`, `ChatJSON`, `Plan`)
* [ ] Claude Adapter에서 `ToolCallSuggest` + `ToolCallLoop` 구현
* [ ] Ollama Adapter에서 `ChatGenerate` 구현
* [ ] Prompt Engine에서 `.liquid` 템플릿 로딩 및 변수 치환 테스트 성공
* [ ] Schema Validator로 JSON 출력 강제 테스트 성공
