# 📍 5단계: 실행/운영 레이어 (Execution & Ops)

---

## 🎯 목표

* CLI/HTTP/gRPC 등 외부 인터페이스 제공 → 프레임워크 실행 가능
* 실행 시점의 설정/흐름/결과를 기록하고 재현 가능하게 만들기
* 로그·메트릭·트레이스를 통해 관측(Observability) 확보
* 평가(Evaluation) 도구를 붙여 품질 관리 가능

---

## 📦 주요 구성 요소

### 1. Gateway (외부 인터페이스)

* **CLI**: 개발/테스트용 빠른 실행

  ```bash
  agent run --mode planner --input "매출 데이터 요약"
  ```
* **HTTP API**: REST 스타일

  * `POST /api/agent/run` (입력: text/json, 응답: stream/json)
* **gRPC API**: 고성능·양방향 스트리밍 지원

```csharp
public interface IAgentGateway {
    IAsyncEnumerable<StreamChunk> RunAsync(string input, RunOptions options, CancellationToken ct);
}
```

---

### 2. Config Snapshot

* 실행 시 사용된 **모델/도구/프롬프트/옵션**을 기록
* Secret은 마스킹 처리
* 나중에 “재현 가능 실행(reproducible run)” 지원

예시 저장 JSON:

```json
{
  "runId": "1234-5678",
  "orchestrationType": "planner",
  "modelProfile": "fast",
  "tools": ["math.calculate:v1", "sql.query:v1"],
  "promptTemplate": "plan/agent-plan.hbs",
  "timestamp": "2025-08-20T12:34:56Z"
}
```

---

### 3. Observability (관측성)

* **로그 (Logs)**

  * Step 시작/종료, 오류 메시지
* **메트릭 (Metrics)**

  * 토큰 수, 실행 시간, 도구 호출 횟수
* **트레이스 (Traces)**

  * 실행 경로 기록 (예: Jaeger, OpenTelemetry Export)

```csharp
public sealed record LogItem(
    RunId RunId,
    StepId StepId,
    string Level,        // info, warn, error
    string Message,
    DateTimeOffset Timestamp
);
```

---

### 4. Eval Harness (평가 도구)

* 입력 → 기대 출력 → 실제 출력 비교
* 지표:

  * Schema 준수율 (%)
  * JSON 오류율
  * 요약 정확도/정밀도 (LLM 기반 채점 가능)
* 샘플 구조:

```
samples/eval/
  inputs/
    report1.json
  expected/
    report1.expected.json
  results/
    report1.run.json
```

---

### 5. 배포 모드

* **로컬 실행 모드**: 개발자 PC, 로컬 LLM/도구 활용
* **클라우드 실행 모드**: OpenAI/Claude 등 SaaS 활용
* **온프렘 실행 모드**: 내부 GPU 클러스터 연결

환경별 `configs/instances/{env}.json`로 분리 관리

---

## 📂 디렉토리 배치 (5단계 산출물)

```
src/Agent.Gateway.Cli/
  Program.cs
  CliRunner.cs

src/Agent.Gateway.Http/
  Controllers/
    AgentController.cs
  Startup.cs

src/Agent.Observability/
  Logger.cs
  Metrics.cs
  Tracer.cs

samples/eval/
  inputs/
  expected/
  results/
```

---

## ✅ 5단계 완료 기준

* [ ] CLI에서 `agent run "질문"` 실행 가능
* [ ] HTTP API에서 `POST /api/agent/run` → 스트리밍 응답 확인
* [ ] 실행 시 Config Snapshot 기록 & 재현 가능
* [ ] 로그/메트릭/트레이스 기본 수집 (OpenTelemetry 호환)

