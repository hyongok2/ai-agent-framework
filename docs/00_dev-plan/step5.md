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



## 📋 실행/운영 레이어 구현 보강 사항

### 1. **CLI Gateway 상세 구현**

#### CLI 명령 구조
```csharp
// src/Agent.Gateway.Cli/Commands/
- RunCommand.cs (기본 실행)
- InteractiveCommand.cs (대화형 모드)
- BatchCommand.cs (배치 실행)
- ConfigCommand.cs (설정 관리)
- ToolCommand.cs (도구 관리)
- ProfileCommand.cs (프로필 관리)
- DebugCommand.cs (디버그 모드)
```

#### CLI 기능 확장
```csharp
// src/Agent.Gateway.Cli/Features/
필요 기능:
- 자동 완성 지원
- 컬러 출력 및 포맷팅
- 프로그레스 바 및 스피너
- 스트리밍 출력 렌더링
- 마크다운 렌더링
- 테이블 출력
- 파일 입출력 지원
- 환경 변수 오버라이드
```

#### 예시 CLI 사용법
```bash
# 기본 실행
agent run "데이터 분석해줘" --mode planner --profile fast

# 대화형 모드
agent chat --model gpt-4 --stream

# 배치 실행
agent batch --input queries.txt --output results.json --parallel 5

# 도구 관리
agent tool list --category data
agent tool install mcp://github.com/example/tool
agent tool test calculator --input '{"a": 5, "b": 3}'

# 디버그 모드
agent debug "테스트 쿼리" --trace --verbose --save-logs
```

### 2. **HTTP API Gateway 구현**

#### REST API 엔드포인트
```csharp
// src/Agent.Gateway.Http/Controllers/

// AgentController.cs
POST   /api/v1/agent/run          // 단일 실행
POST   /api/v1/agent/run/stream   // 스트리밍 실행
POST   /api/v1/agent/batch        // 배치 실행
GET    /api/v1/agent/status/{id}  // 실행 상태 조회
DELETE /api/v1/agent/cancel/{id}  // 실행 취소

// ConversationController.cs  
POST   /api/v1/conversations                    // 새 대화 시작
GET    /api/v1/conversations/{id}              // 대화 조회
POST   /api/v1/conversations/{id}/messages     // 메시지 추가
DELETE /api/v1/conversations/{id}              // 대화 삭제
GET    /api/v1/conversations/{id}/history      // 이력 조회

// ToolController.cs
GET    /api/v1/tools                          // 도구 목록
GET    /api/v1/tools/{id}                    // 도구 정보
POST   /api/v1/tools/{id}/execute            // 도구 실행
POST   /api/v1/tools/register                // 도구 등록
DELETE /api/v1/tools/{id}                    // 도구 제거

// ConfigController.cs
GET    /api/v1/config                        // 설정 조회
PUT    /api/v1/config                        // 설정 업데이트
GET    /api/v1/config/profiles               // 프로필 목록
POST   /api/v1/config/profiles               // 프로필 추가
```

#### WebSocket 지원
```csharp
// src/Agent.Gateway.Http/WebSockets/
- AgentWebSocketHandler.cs
- StreamingProtocol.cs
- ConnectionManager.cs
- HeartbeatManager.cs

// WebSocket 엔드포인트
WS /ws/agent/stream    // 실시간 스트리밍
WS /ws/agent/chat      // 대화형 세션
```

#### API 미들웨어
```csharp
// src/Agent.Gateway.Http/Middleware/
- AuthenticationMiddleware.cs (인증)
- RateLimitingMiddleware.cs (속도 제한)
- RequestLoggingMiddleware.cs (요청 로깅)
- ErrorHandlingMiddleware.cs (에러 처리)
- CorrelationIdMiddleware.cs (추적 ID)
- CompressionMiddleware.cs (압축)
```

### 3. **gRPC Gateway 구현**

#### Proto 정의
```protobuf
// protos/agent.proto
service AgentService {
    rpc Execute(ExecuteRequest) returns (ExecuteResponse);
    rpc ExecuteStream(ExecuteRequest) returns (stream StreamChunk);
    rpc GetStatus(StatusRequest) returns (StatusResponse);
    rpc Cancel(CancelRequest) returns (CancelResponse);
}

service ToolService {
    rpc ListTools(ListToolsRequest) returns (ListToolsResponse);
    rpc ExecuteTool(ToolExecuteRequest) returns (ToolExecuteResponse);
    rpc RegisterTool(RegisterToolRequest) returns (RegisterToolResponse);
}
```

#### gRPC 서비스 구현
```csharp
// src/Agent.Gateway.Grpc/Services/
- AgentGrpcService.cs
- ToolGrpcService.cs
- ConfigGrpcService.cs
- HealthGrpcService.cs
```

### 4. **Config Snapshot 시스템**

```csharp
// src/Agent.Observability/Snapshot/
- ConfigSnapshotManager.cs
- SnapshotStorage.cs (파일/DB 저장)
- SnapshotComparer.cs (차이 비교)
- SnapshotReplay.cs (재현 실행)

// 스냅샷 구조
public class ExecutionSnapshot
{
    public string RunId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public OrchestrationType Type { get; set; }
    public ModelProfile ModelProfile { get; set; }
    public List<ToolConfiguration> Tools { get; set; }
    public Dictionary<string, string> Environment { get; set; }
    public PromptTemplate PromptTemplate { get; set; }
    public JsonNode Input { get; set; }
    public JsonNode Output { get; set; }
    public ExecutionMetrics Metrics { get; set; }
    public string GitCommitHash { get; set; }
}
```

### 5. **Observability 상세 구현**

#### 로깅 시스템
```csharp
// src/Agent.Observability/Logging/
- StructuredLogger.cs (구조화 로깅)
- LogEnricher.cs (로그 보강)
- LogSink.cs (다중 출력)
- LogFilter.cs (필터링)
- SensitiveDataMasker.cs (민감 정보 마스킹)

// 로그 레벨별 처리
- TRACE: 상세 디버그 정보
- DEBUG: 디버그 정보
- INFO: 일반 정보
- WARN: 경고
- ERROR: 에러
- FATAL: 치명적 오류
```

#### 메트릭 수집
```csharp
// src/Agent.Observability/Metrics/
- MetricsCollector.cs
- PrometheusExporter.cs
- CustomMetrics.cs

// 핵심 메트릭
- agent_requests_total (카운터)
- agent_request_duration_seconds (히스토그램)
- agent_active_executions (게이지)
- agent_tokens_used_total (카운터)
- agent_tool_calls_total (카운터)
- agent_errors_total (카운터)
- agent_llm_latency_seconds (히스토그램)
- agent_cache_hit_ratio (게이지)
```

#### 분산 추적
```csharp
// src/Agent.Observability/Tracing/
- TracingProvider.cs (OpenTelemetry)
- SpanEnricher.cs
- TraceExporter.cs (Jaeger/Zipkin)
- TraceContextPropagator.cs

// 추적 스팬 구조
- agent.execute (루트 스팬)
  - orchestration.select
  - llm.call
    - llm.tokenize
    - llm.generate
    - llm.parse
  - tool.execute
    - tool.validate
    - tool.run
  - streaming.aggregate
```

### 6. **Evaluation Harness**

```csharp
// src/Agent.Evaluation/
- EvaluationRunner.cs (평가 실행)
- TestCaseLoader.cs (테스트 케이스 로딩)
- MetricCalculator.cs (지표 계산)
- ResultComparer.cs (결과 비교)
- ReportGenerator.cs (보고서 생성)

// 평가 지표
- Accuracy (정확도)
- Precision/Recall (정밀도/재현율)
- F1 Score
- BLEU Score (텍스트 품질)
- Latency (응답 시간)
- Token Efficiency (토큰 효율성)
- Cost per Query (쿼리당 비용)
- Schema Compliance (스키마 준수율)
```

#### 평가 데이터 구조
```yaml
# samples/eval/test-cases/search-query.yaml
test_case:
  id: "search-001"
  category: "information_retrieval"
  input:
    query: "What is the capital of France?"
  expected:
    type: "simple_answer"
    content: "Paris"
    keywords: ["Paris", "capital", "France"]
  constraints:
    max_tokens: 100
    max_latency_ms: 2000
    required_accuracy: 0.95
```

### 7. **배포 모드별 구성**

#### 로컬 실행 모드
```csharp
// src/Agent.Deployment/Local/
- LocalRuntime.cs
- LocalResourceManager.cs
- LocalModelLoader.cs
- GpuDetector.cs

// 설정 예시
{
  "mode": "local",
  "models": {
    "path": "/models",
    "preload": ["llama2", "mistral"]
  },
  "resources": {
    "maxMemory": "16GB",
    "gpuEnabled": true
  }
}
```

#### 클라우드 실행 모드
```csharp
// src/Agent.Deployment/Cloud/
- CloudRuntime.cs
- AutoScaler.cs
- LoadBalancer.cs
- FailoverManager.cs

// 설정 예시
{
  "mode": "cloud",
  "providers": {
    "primary": "openai",
    "fallback": ["claude", "azure"]
  },
  "scaling": {
    "min": 2,
    "max": 100,
    "targetCpu": 70
  }
}
```

#### 온프렘 실행 모드
```csharp
// src/Agent.Deployment/OnPrem/
- OnPremRuntime.cs
- ClusterManager.cs
- NodeDiscovery.cs
- ResourceScheduler.cs

// 설정 예시
{
  "mode": "onprem",
  "cluster": {
    "nodes": ["node1:8080", "node2:8080"],
    "loadBalancing": "round-robin"
  },
  "security": {
    "tls": true,
    "mtls": true
  }
}
```

### 8. **보안 및 인증**

```csharp
// src/Agent.Security/
- AuthenticationService.cs (인증)
- AuthorizationService.cs (인가)
- ApiKeyManager.cs (API 키 관리)
- JwtTokenService.cs (JWT 토큰)
- RbacManager.cs (역할 기반 접근 제어)
- AuditService.cs (감사)
- EncryptionService.cs (암호화)
```

### 9. **운영 도구**

#### 헬스체크
```csharp
// src/Agent.Operations/Health/
- HealthCheckService.cs
- ReadinessProbe.cs
- LivenessProbe.cs
- DependencyChecker.cs

// 헬스체크 엔드포인트
GET /health/live     // 살아있는지
GET /health/ready    // 준비되었는지
GET /health/startup  // 시작 완료
```

#### 백업 및 복구
```csharp
// src/Agent.Operations/Backup/
- BackupService.cs
- RestoreService.cs
- SnapshotScheduler.cs
- DataExporter.cs
```

#### 모니터링 대시보드
```csharp
// src/Agent.Operations/Dashboard/
- DashboardService.cs
- MetricsAggregator.cs
- AlertManager.cs
- ReportScheduler.cs
```

### 10. **SDK 및 클라이언트 라이브러리**

```csharp
// src/Agent.Sdk/
- AgentClient.cs (C# 클라이언트)
- AgentClientBuilder.cs
- RetryPolicy.cs
- ResponseParser.cs

// 다른 언어 SDK
// sdk/python/agent_client.py
// sdk/typescript/agent-client.ts
// sdk/go/agent_client.go
```

### 11. **테스트 인프라**

```csharp
// src/Agent.Testing/
- IntegrationTestBase.cs
- MockAgentServer.cs
- TestDataGenerator.cs
- PerformanceBenchmark.cs
- LoadTestRunner.cs
- ChaosTestRunner.cs
```

### 12. **CI/CD 파이프라인**

```yaml
# .github/workflows/ci.yml
- 코드 빌드 및 테스트
- 도커 이미지 빌드
- 통합 테스트
- 성능 테스트
- 보안 스캔
- 배포 (dev/staging/prod)
```

### 13. **문서화 시스템**

```csharp
// src/Agent.Documentation/
- ApiDocGenerator.cs (OpenAPI 생성)
- MarkdownGenerator.cs
- ExampleGenerator.cs
- ChangelogGenerator.cs
```

### 14. **운영 스크립트**

```bash
# scripts/operations/
- health-check.sh        # 헬스 체크
- backup.sh             # 백업
- restore.sh            # 복구
- scale.sh              # 스케일링
- deploy.sh             # 배포
- rollback.sh           # 롤백
- monitor.sh            # 모니터링
```

### 15. **에러 처리 및 복구**

```csharp
// src/Agent.Operations/Recovery/
- ErrorClassifier.cs (에러 분류)
- AutoRecovery.cs (자동 복구)
- CircuitBreaker.cs (서킷 브레이커)
- FallbackHandler.cs (폴백 처리)
- ErrorReporter.cs (에러 리포팅)
```

## 우선순위 구현 순서

1. **기본 Gateway 구현**
   - CLI 기본 명령
   - HTTP REST API
   - 기본 인증

2. **Observability 기초**
   - 구조화 로깅
   - 기본 메트릭
   - 헬스체크

3. **Config Snapshot**
   - 스냅샷 저장
   - 재현 실행

4. **운영 도구**
   - 모니터링
   - 백업/복구
   - 에러 처리

5. **고급 기능**
   - gRPC Gateway
   - 평가 시스템
   - SDK 개발

이러한 구현을 통해 프로덕션 환경에서 안정적으로 운영 가능한 AI Agent Framework를 구축할 수 있습니다.