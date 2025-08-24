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



현재 프로젝트 구조를 분석해보니, 추상화 계층은 잘 구현되어 있지만 `step3.md`의 LLM 계층 구현에서 몇 가지 보강이 필요한 부분들이 있습니다.

## 📋 LLM 계층 구현 보강 사항

### 1. **구체적 구현체 작성 필요**

#### OpenAI 클라이언트 구현
```csharp
// src/Agent.Llm.OpenAI/OpenAiClient.cs
- OpenAI API 연동 구현
- GPT-4, GPT-3.5 모델 지원
- 함수 호출 지원
- 스트리밍 응답 처리
- 토큰 카운팅 및 비용 계산
```

#### Claude 클라이언트 구현
```csharp
// src/Agent.Llm.Claude/ClaudeClient.cs
- Anthropic API 연동
- Claude 3 모델 지원
- 도구 사용 패턴 구현
- 컨텍스트 윈도우 관리
```

#### Ollama 클라이언트 구현
```csharp
// src/Agent.Llm.Ollama/OllamaClient.cs
- 로컬 Ollama API 연동
- 모델 자동 탐색
- 스트리밍 지원
- 리소스 모니터링
```

### 2. **LLM Router 구현**

```csharp
// src/Agent.Llm.Core/Routing/DefaultLlmRouter.cs
필요 기능:
- CallType별 최적 모델 선택 로직
- Capabilities 기반 fallback 처리
- 비용 최적화 라우팅
- 부하 분산 로직
- 실패 시 재라우팅
```

### 3. **Prompt Engine 구현**

```csharp
// src/Agent.Llm.Core/Prompts/PromptEngine.cs
필요 기능:
- Liquid/Scriban 템플릿 엔진 통합
- 변수 바인딩 및 검증
- Few-shot 예제 자동 삽입
- Schema 기반 JSON 지시문 추가
- 프롬프트 최적화 (토큰 절약)
- 다국어 프롬프트 지원
```

### 4. **Model Profile Manager**

```csharp
// src/Agent.Llm.Core/Profiles/ModelProfileManager.cs
필요 기능:
- JSON 프로필 파일 로딩
- 프로필 검증 및 병합
- 환경별 오버라이드
- 동적 프로필 업데이트
- 프로필 버전 관리
```

### 5. **CallType 핸들러**

각 CallType별 전문 핸들러 구현:

```csharp
// src/Agent.Llm.Core/Handlers/
- ChatGenerateHandler.cs
- ChatJsonHandler.cs  
- PlanGenerationHandler.cs
- ToolCallSuggestHandler.cs
- ToolCallLoopHandler.cs
- ReasoningHandler.cs
- VisionQAHandler.cs
- EmbeddingHandler.cs
```

### 6. **스트리밍 처리 개선**

```csharp
// src/Agent.Llm.Core/Streaming/StreamProcessor.cs
필요 기능:
- 청크 버퍼링 및 재조립
- JSON 부분 파싱
- 함수 호출 스트림 처리
- 에러 복구 메커니즘
- 백프레셔 처리
```

### 7. **JSON Schema 통합**

```csharp
// src/Agent.Llm.Core/Schema/SchemaEnforcer.cs
필요 기능:
- LLM 응답 검증
- 자동 수정 시도
- Schema → TypeScript/Python 타입 변환
- OpenAPI 스키마 호환성
```

### 8. **메트릭 및 모니터링**

```csharp
// src/Agent.Llm.Core/Monitoring/LlmMetricsCollector.cs
필요 기능:
- 토큰 사용량 추적
- 응답 시간 측정
- 에러율 모니터링
- 비용 계산 및 예산 관리
- 모델별 성능 비교
```

### 9. **캐싱 레이어**

```csharp
// src/Agent.Llm.Core/Caching/LlmCache.cs
필요 기능:
- 시맨틱 캐싱 (임베딩 기반)
- 정확한 매칭 캐시
- TTL 관리
- 캐시 무효화 정책
```

### 10. **테스트 및 Mock**

```csharp
// src/Agent.Llm.Tests/
필요 항목:
- MockLlmClient (테스트용)
- 각 CallType별 단위 테스트
- 통합 테스트 시나리오
- 성능 벤치마크
- 스트리밍 테스트
```

### 11. **설정 파일 구조**

```yaml
# configs/model-profiles/fast.yaml
profile: fast
description: "빠른 응답용 소형 모델"
providers:
  - name: openai
    model: gpt-4o-mini
    priority: 1
    settings:
      temperature: 0.7
      maxTokens: 512
  - name: claude
    model: claude-3-haiku
    priority: 2
fallback:
  enabled: true
  maxAttempts: 3
```

### 12. **프롬프트 템플릿 예제**

```liquid
# samples/prompts/plan/agent-plan.liquid
{% if system_context %}
System Context: {{ system_context }}
{% endif %}

Generate an execution plan for the following request:
{{ user_request }}

{% if constraints %}
Constraints:
{% for constraint in constraints %}
- {{ constraint }}
{% endfor %}
{% endif %}

Output Format: JSON following the schema {{ schema_ref }}
```

### 13. **오류 처리 및 재시도**

```csharp
// src/Agent.Llm.Core/Resilience/RetryPolicy.cs
필요 기능:
- 지수 백오프
- 서킷 브레이커
- 타임아웃 처리
- 부분 실패 복구
- 데드레터 큐
```

### 14. **보안 및 거버넌스**

```csharp
// src/Agent.Llm.Core/Security/
- ContentFilter.cs (유해 콘텐츠 필터링)
- PiiRedactor.cs (개인정보 마스킹)
- RateLimiter.cs (속도 제한)
- AuditLogger.cs (감사 로깅)
```

### 15. **문서화**

```markdown
필요 문서:
- API 레퍼런스
- 각 CallType 사용 가이드
- 모델 선택 가이드
- 프롬프트 엔지니어링 베스트 프랙티스
- 비용 최적화 가이드
- 트러블슈팅 가이드
```

이러한 구현 사항들을 단계적으로 진행하되, 우선순위는:
1. **핵심 클라이언트 구현** (OpenAI, Claude, Ollama)
2. **Router와 Prompt Engine**
3. **CallType 핸들러**
4. **스트리밍 및 캐싱**
5. **모니터링 및 보안**

순으로 진행하는 것을 추천합니다.