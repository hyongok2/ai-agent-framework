# AI Agent Framework - Claude Development Guidelines

## 🎯 프로젝트 비전

**실제 프로덕션 환경에서 사용 가능한 엔터프라이즈급 AI Agent 오케스트레이션 플랫폼 구축**

### 핵심 가치
- **타입 안전성**: 컴파일 타임 검증으로 런타임 오류 최소화
- **확장성**: 플러그인 시스템으로 무제한 확장
- **관찰성**: 모든 작업 추적 및 모니터링 가능
- **복원력**: 장애 상황에서도 안정적 작동
- **우아함**: 클린 아키텍처와 SOLID 원칙 준수

## 📋 현재 상태 및 개선 계획

### 🟡 현재 완성도: 35% (타입 안전성 대폭 개선)
**Critical Issues**: 4개 | **High Issues**: 6개 | **Medium Issues**: 5개

### ✅ 최근 완료 작업 (Phase 1, Day 1)
- **오케스트레이션 엔진 타입 안전성** (우선순위 #1) - 완료! ✅
- 문자열 파싱 의존 `GetActionType()` 메서드 완전 제거 ✅
- `IOrchestrationAction` 인터페이스 및 `ActionType` 열거형 구현 ✅
- `LLMAction`, `ToolAction` 구체 액션 클래스 구현 ✅
- `ActionFactory` 타입 안전 팩토리 구현 ✅
- `IExecutionContext` 인터페이스 구현 ✅
- 모든 테스트 통과 (20/20) ✅
- 빌드 성공 (오류 0개) ✅

### 🎯 다음 우선순위 작업 (Phase 1, Day 2)
- **LLM Provider 실제 구현** (우선순위 #2, Score: 33)
- 가짜 토큰 카운팅 → 실제 tiktoken 기반 카운팅
- 테스트 코드 → 실제 Claude/OpenAI API 호출 구현
- 스트리밍 응답 지원 추가

### 🎯 목표 완성도: 95% (프로덕션 레디)
**6주 전면 리팩토링 로드맵** 수립 완료

## 📂 프로젝트 폴더 구조

### 목표 폴더 구조
```
AIAgentFramework/
├── src/
│   ├── AIAgentFramework.Core/                     # 핵심 추상화
│   │   ├── Abstractions/                          # 인터페이스 모음
│   │   │   ├── Orchestration/
│   │   │   │   ├── IOrchestrationEngine.cs
│   │   │   │   ├── IOrchestrationStrategy.cs
│   │   │   │   ├── IOrchestrationAction.cs
│   │   │   │   └── IExecutionContext.cs
│   │   │   ├── LLM/
│   │   │   │   ├── ILLMProvider.cs
│   │   │   │   ├── ILLMFunction.cs
│   │   │   │   └── ITokenCounter.cs
│   │   │   ├── Tools/
│   │   │   │   ├── ITool.cs
│   │   │   │   ├── IToolRegistry.cs
│   │   │   │   └── IToolExecutor.cs
│   │   │   ├── State/
│   │   │   │   ├── IStateProvider.cs
│   │   │   │   ├── IStateManager.cs
│   │   │   │   └── IStateTransaction.cs
│   │   │   └── Common/
│   │   │       ├── IResult.cs
│   │   │       ├── IRegistry.cs
│   │   │       └── IFactory.cs
│   │   ├── Models/                                # 데이터 모델
│   │   │   ├── Orchestration/
│   │   │   │   ├── OrchestrationContext.cs
│   │   │   │   ├── OrchestrationResult.cs
│   │   │   │   ├── ExecutionStep.cs
│   │   │   │   └── UserRequest.cs
│   │   │   ├── LLM/
│   │   │   │   ├── LLMRequest.cs
│   │   │   │   ├── LLMResponse.cs
│   │   │   │   ├── LLMContext.cs
│   │   │   │   └── TokenUsage.cs
│   │   │   ├── Tools/
│   │   │   │   ├── ToolInput.cs
│   │   │   │   ├── ToolResult.cs
│   │   │   │   └── ToolContract.cs
│   │   │   └── Common/
│   │   │       ├── Result.cs
│   │   │       ├── Error.cs
│   │   │       └── Metadata.cs
│   │   ├── Exceptions/                            # 도메인 예외
│   │   │   ├── AIAgentException.cs
│   │   │   ├── OrchestrationException.cs
│   │   │   ├── LLMException.cs
│   │   │   ├── ToolException.cs
│   │   │   └── StateException.cs
│   │   └── Enums/                                 # 열거형
│   │       ├── ActionType.cs
│   │       ├── ExecutionStatus.cs
│   │       ├── FunctionCategory.cs
│   │       └── ErrorSeverity.cs
│   │
│   ├── AIAgentFramework.Orchestration/            # 오케스트레이션 구현
│   │   ├── Engines/
│   │   │   ├── TypeSafeOrchestrationEngine.cs
│   │   │   └── StatefulOrchestrationEngine.cs
│   │   ├── Strategies/
│   │   │   ├── Base/
│   │   │   │   └── OrchestrationStrategyBase.cs
│   │   │   ├── PlanExecuteStrategy.cs
│   │   │   ├── ReActStrategy.cs
│   │   │   └── HybridReasoningStrategy.cs
│   │   ├── Actions/
│   │   │   ├── Base/
│   │   │   │   └── OrchestrationActionBase.cs
│   │   │   ├── LLMAction.cs
│   │   │   ├── ToolAction.cs
│   │   │   ├── DelayAction.cs
│   │   │   └── ConditionalAction.cs
│   │   ├── Context/
│   │   │   ├── ExecutionContextFactory.cs
│   │   │   └── ContextManager.cs
│   │   └── Factories/
│   │       ├── ActionFactory.cs
│   │       └── StrategyFactory.cs
│   │
│   ├── AIAgentFramework.LLM/                      # LLM 시스템
│   │   ├── Providers/
│   │   │   ├── Base/
│   │   │   │   └── LLMProviderBase.cs
│   │   │   ├── OpenAIProvider.cs
│   │   │   ├── ClaudeProvider.cs
│   │   │   ├── LocalLLMProvider.cs
│   │   │   └── ResilientLLMProvider.cs
│   │   ├── Functions/
│   │   │   ├── Base/
│   │   │   │   └── LLMFunctionBase.cs
│   │   │   ├── Planning/
│   │   │   │   ├── PlannerFunction.cs
│   │   │   │   └── CompletionCheckerFunction.cs
│   │   │   ├── Analysis/
│   │   │   │   ├── AnalyzerFunction.cs
│   │   │   │   ├── ClassifierFunction.cs
│   │   │   │   └── SentimentAnalyzer.cs
│   │   │   └── Generation/
│   │   │       ├── GeneratorFunction.cs
│   │   │       └── SummarizerFunction.cs
│   │   ├── TokenManagement/
│   │   │   ├── TiktokenCounter.cs
│   │   │   ├── TokenBudgetManager.cs
│   │   │   └── TokenUsageTracker.cs
│   │   └── Factories/
│   │       ├── LLMProviderFactory.cs
│   │       └── LLMFunctionFactory.cs
│   │
│   ├── AIAgentFramework.Tools/                    # 도구 시스템
│   │   ├── BuiltIn/
│   │   │   ├── Base/
│   │   │   │   └── ToolBase.cs
│   │   │   ├── Search/
│   │   │   │   └── WebSearchTool.cs
│   │   │   ├── Data/
│   │   │   │   ├── DatabaseTool.cs
│   │   │   │   └── VectorDBTool.cs
│   │   │   └── System/
│   │   │       └── FileSystemTool.cs
│   │   ├── Plugin/
│   │   │   ├── Base/
│   │   │   │   └── PluginToolBase.cs
│   │   │   ├── Loader/
│   │   │   │   └── PluginLoader.cs
│   │   │   └── Registry/
│   │   │       └── PluginRegistry.cs
│   │   ├── MCP/
│   │   │   ├── Adapters/
│   │   │   │   └── MCPToolAdapter.cs
│   │   │   ├── Client/
│   │   │   │   └── MCPClient.cs
│   │   │   └── Protocol/
│   │   │       └── MCPProtocol.cs
│   │   ├── Execution/
│   │   │   ├── ToolExecutor.cs
│   │   │   └── ToolValidator.cs
│   │   └── Registry/
│   │       ├── ToolRegistry.cs
│   │       └── TypedToolRegistry.cs
│   │
│   ├── AIAgentFramework.State/                    # 상태 관리
│   │   ├── Providers/
│   │   │   ├── Base/
│   │   │   │   └── StateProviderBase.cs
│   │   │   ├── RedisStateProvider.cs
│   │   │   ├── SqlServerStateProvider.cs
│   │   │   └── InMemoryStateProvider.cs
│   │   ├── Managers/
│   │   │   ├── StateManager.cs
│   │   │   └── CheckpointManager.cs
│   │   ├── Transactions/
│   │   │   ├── StateTransaction.cs
│   │   │   └── TransactionManager.cs
│   │   └── Serialization/
│   │       ├── JsonStateSerializer.cs
│   │       └── BinaryStateSerializer.cs
│   │
│   ├── AIAgentFramework.Resilience/               # 복원력 패턴
│   │   ├── Policies/
│   │   │   ├── RetryPolicy.cs
│   │   │   ├── CircuitBreaker.cs
│   │   │   ├── TimeoutPolicy.cs
│   │   │   └── FallbackPolicy.cs
│   │   ├── Pipeline/
│   │   │   └── ResiliencePipeline.cs
│   │   └── Patterns/
│   │       ├── Saga/
│   │       │   ├── ISaga.cs
│   │       │   ├── SagaCoordinator.cs
│   │       │   └── SagaStep.cs
│   │       └── UnitOfWork/
│   │           ├── IUnitOfWork.cs
│   │           └── UnitOfWorkManager.cs
│   │
│   ├── AIAgentFramework.Configuration/            # 설정 관리
│   │   ├── Managers/
│   │   │   ├── ConfigurationManager.cs
│   │   │   └── CacheManager.cs
│   │   ├── Providers/
│   │   │   ├── YamlConfigurationProvider.cs
│   │   │   └── JsonConfigurationProvider.cs
│   │   ├── Options/
│   │   │   ├── AIAgentOptions.cs
│   │   │   ├── LLMOptions.cs
│   │   │   └── ToolOptions.cs
│   │   └── Validation/
│   │       ├── ConfigurationValidator.cs
│   │       └── OptionsValidator.cs
│   │
│   ├── AIAgentFramework.Monitoring/               # 모니터링
│   │   ├── Telemetry/
│   │   │   ├── TelemetryCollector.cs
│   │   │   └── ActivitySourceManager.cs
│   │   ├── Metrics/
│   │   │   ├── MetricsCollector.cs
│   │   │   └── PrometheusExporter.cs
│   │   ├── Health/
│   │   │   ├── OrchestrationHealthCheck.cs
│   │   │   ├── LLMHealthCheck.cs
│   │   │   └── StateHealthCheck.cs
│   │   └── Logging/
│   │       ├── StructuredLogger.cs
│   │       └── LoggerExtensions.cs
│   │
│   └── AIAgentFramework.Infrastructure/           # 인프라 서비스
│       ├── DependencyInjection/
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── ServiceRegistrar.cs
│       ├── Hosting/
│       │   ├── AIAgentHostBuilder.cs
│       │   └── BackgroundServices/
│       │       └── OrchestrationBackgroundService.cs
│       └── Serialization/
│           ├── JsonSerializer.cs
│           └── SerializationOptions.cs
│
├── samples/
│   ├── CustomerSupport/
│   │   ├── CustomerSupportAgent.cs
│   │   ├── Models/
│   │   └── Tools/
│   ├── DataAnalysis/
│   │   └── DataAnalysisAgent.cs
│   └── ContentGeneration/
│       └── ContentAgent.cs
│
├── tests/
│   ├── AIAgentFramework.Core.Tests/
│   ├── AIAgentFramework.Orchestration.Tests/
│   ├── AIAgentFramework.LLM.Tests/
│   ├── AIAgentFramework.Tools.Tests/
│   ├── AIAgentFramework.State.Tests/
│   └── AIAgentFramework.Integration.Tests/
│
└── docs/
    ├── architecture/
    ├── api/
    └── samples/
```

### 폴더 구조 설계 원칙

1. **1 Class = 1 File**: 모든 클래스는 독립된 파일
2. **의미적 그룹핑**: 관련 기능별 폴더 분류
3. **깊이 제한**: 최대 4단계 깊이까지만 허용
4. **명확한 네이밍**: 폴더명으로 역할 명확히 표현
5. **Base 클래스 분리**: 추상 클래스는 Base 폴더에 격리

## 🗓️ 상세 Task List - 6주 리팩토링 계획

## 📁 구현된 새로운 파일들 (Phase 1, Day 1)

### 새로 생성된 핵심 파일들
```
AIAgentFramework.Core/
├── Interfaces/
│   ├── IOrchestrationAction.cs        # 타입 안전한 액션 인터페이스
│   └── IExecutionContext.cs           # 액션 실행 컨텍스트
├── Models/
│   ├── ActionType.cs                  # 액션 타입 열거형
│   └── ActionResult.cs                # 액션 실행 결과 모델
├── Actions/
│   ├── OrchestrationActionBase.cs     # 액션 기본 클래스
│   ├── LLMAction.cs                   # LLM 기능 실행 액션
│   └── ToolAction.cs                  # 도구 실행 액션
└── Factories/
    └── ActionFactory.cs               # 타입 안전 액션 팩토리
```

### 업데이트된 기존 파일들
```
AIAgentFramework.Orchestration/
├── OrchestrationEngine.cs             # GetActionType() 제거, 타입 안전 로직 적용
├── OrchestrationContext.cs            # IExecutionContext 구현 추가
└── Context/ContextManager.cs          # Registry 의존성 추가

AIAgentFramework.WebAPI/
└── Controllers/OrchestrationController.cs  # Registry 의존성 추가

AIAgentFramework.Tests/
└── ContextManagerTests.cs            # Registry Mock 추가
```

### 📋 Phase 1: Critical Core Issues (Week 1)

#### Day 1: 오케스트레이션 엔진 재설계
- [x] `IOrchestrationAction` 인터페이스 생성
- [x] `ActionType` 열거형 정의  
- [x] `LLMAction` 클래스 구현
- [x] `ToolAction` 클래스 구현
- [ ] `DelayAction` 클래스 구현
- [ ] `ConditionalAction` 클래스 구현
- [x] `ActionFactory` 클래스 구현
- [x] 기존 `GetActionType()` 메서드 완전 제거
- [x] 단위 테스트 작성 및 통과

#### Day 2: 타입 안전한 Registry 구현
- [ ] `ILLMFunctionRegistry` 인터페이스 생성
- [ ] `IToolRegistry` 인터페이스 생성
- [ ] `TypedLLMFunctionRegistry` 클래스 구현
- [ ] `TypedToolRegistry` 클래스 구현
- [ ] 기존 문자열 기반 Registry 사용 코드 모두 교체
- [ ] DI 컨테이너 설정 업데이트
- [ ] 단위 테스트 작성 및 통과

#### Day 3: TypeSafeOrchestrationEngine 구현
- [x] `IExecutionContext` 인터페이스 생성
- [ ] `ExecutionContextFactory` 클래스 구현
- [ ] `TypeSafeOrchestrationEngine` 클래스 구현
- [x] 기존 오케스트레이션 로직 마이그레이션
- [ ] 타입 안전성 검증 테스트
- [ ] 성능 벤치마크 테스트

#### Day 4: Configuration 시스템 완성
- [ ] `IConfigurationCache` 인터페이스 구현 완성
- [ ] `CacheManager` 클래스에서 실제 캐시 무효화 로직 구현
- [ ] `ConcurrentSet<string>` 기반 키 추적 시스템 구현
- [ ] 패턴 기반 캐시 무효화 기능 구현
- [ ] 캐시 무효화 성능 테스트
- [ ] Configuration 로딩 성능 최적화

#### Day 5: LLM Provider 토큰 카운팅 실제 구현
- [ ] `ITokenCounter` 인터페이스 완성
- [ ] `TiktokenCounter` 클래스 실제 구현
- [ ] 모델별 인코딩 매핑 완성
- [ ] `ClaudeProvider`에서 가짜 토큰 카운팅 제거
- [ ] 실제 토큰 계산 로직 통합
- [ ] 토큰 카운팅 정확도 95% 이상 달성 검증

### 📋 Phase 2: State Management System (Week 2)

#### Day 1: State Provider 인터페이스 설계
- [ ] `IStateProvider` 인터페이스 완성
- [ ] `IStateTransaction` 인터페이스 생성
- [ ] `StateProviderException` 예외 클래스 생성
- [ ] `StateTransaction` 기본 구현 클래스 생성
- [ ] 인터페이스 설계 검토 및 승인

#### Day 2: Redis StateProvider 구현
- [ ] `RedisStateProvider` 클래스 완전 구현
- [ ] Redis 연결 관리 로직 구현
- [ ] JSON 직렬화/역직렬화 통합
- [ ] TTL(Time To Live) 지원 구현
- [ ] Redis 연결 오류 처리 구현
- [ ] Redis StateProvider 단위 테스트

#### Day 3: InMemory StateProvider 구현
- [ ] `InMemoryStateProvider` 클래스 구현 (개발/테스트용)
- [ ] 메모리 기반 상태 저장 로직
- [ ] TTL 기반 자동 만료 처리
- [ ] Thread-safe 구현 보장
- [ ] 메모리 사용량 제한 기능
- [ ] InMemory StateProvider 단위 테스트

#### Day 4: StatefulOrchestrationEngine 구현
- [ ] `StatefulOrchestrationEngine` 클래스 구현
- [ ] 상태 복원 로직 구현
- [ ] 상태 저장 로직 구현
- [ ] 실패 시 상태 저장 구현 (복구용)
- [ ] 상태 지속성 통합 테스트
- [ ] 장애 복구 시나리오 테스트

#### Day 5: Checkpoint & Recovery 시스템
- [ ] `ICheckpointManager` 인터페이스 생성
- [ ] `CheckpointManager` 클래스 구현
- [ ] 체크포인트 생성 로직
- [ ] 상태 복원 로직
- [ ] 체크포인트 히스토리 관리
- [ ] 복구 성능 테스트 (5분 이내)

### 📋 Phase 3: Complete LLM Integration (Week 3)

#### Day 1: 실제 LLM API 통합
- [ ] `ClaudeProvider`에서 실제 API 호출 구현
- [ ] HTTP 요청 생성 로직 완성
- [ ] API 응답 파싱 로직 완성
- [ ] 에러 응답 처리 로직
- [ ] API 호출 단위 테스트
- [ ] 실제 Claude API 통합 테스트

#### Day 2: Token Budget Management
- [ ] `ITokenBudgetManager` 인터페이스 생성
- [ ] `TokenBudgetManager` 클래스 구현
- [ ] 일일 토큰 한도 관리
- [ ] 시간당 토큰 한도 관리  
- [ ] 토큰 사용량 추적 및 기록
- [ ] 예산 초과 시 예외 처리

#### Day 3: Streaming Support 구현
- [ ] `IStreamingLLMProvider` 인터페이스 생성
- [ ] `ClaudeProvider`에 스트리밍 지원 추가
- [ ] SSE(Server-Sent Events) 파싱 구현
- [ ] 스트림 청크 모델 정의
- [ ] 스트리밍 취소 처리
- [ ] 스트리밍 성능 테스트

#### Day 4: Resilient LLM Provider
- [ ] `ResilientLLMProvider` 클래스 구현
- [ ] 여러 Provider 간 Failover 구현
- [ ] Circuit Breaker 통합
- [ ] Provider별 가용성 모니터링
- [ ] 자동 Provider 선택 로직
- [ ] Resilience 통합 테스트

#### Day 5: LLM Function 완성
- [ ] 모든 LLM Function 실제 구현 완성
- [ ] `PlannerFunction` 완전 구현
- [ ] `AnalyzerFunction` 완전 구현
- [ ] `ClassifierFunction` 완전 구현
- [ ] `CompletionCheckerFunction` 완전 구현
- [ ] E2E LLM 워크플로우 테스트

### 📋 Phase 4: Transaction Support (Week 4)

#### Day 1-2: Saga Pattern 구현
- [ ] `ISaga` 인터페이스 생성
- [ ] `ISagaStep` 인터페이스 생성
- [ ] `SagaCoordinator` 클래스 구현
- [ ] `OrchestrationSaga` 클래스 구현
- [ ] 보상 트랜잭션 로직 구현
- [ ] Saga 실행 및 롤백 테스트

#### Day 3: Unit of Work Pattern
- [ ] `IUnitOfWork` 인터페이스 생성
- [ ] `OrchestrationUnitOfWork` 클래스 구현
- [ ] Entity 변경 추적 구현
- [ ] 트랜잭션 커밋/롤백 구현
- [ ] 동시성 제어 구현
- [ ] UnitOfWork 통합 테스트

#### Day 4: Idempotency Support
- [ ] `IIdempotencyManager` 인터페이스 생성
- [ ] `IdempotencyManager` 클래스 구현
- [ ] 멱등성 키 관리 구현
- [ ] 결과 캐싱 및 재사용
- [ ] `IdempotentOrchestrationEngine` 구현
- [ ] 멱등성 보장 테스트

#### Day 5: Transaction 통합 테스트
- [ ] 복합 트랜잭션 시나리오 테스트
- [ ] 부분 실패 복구 테스트
- [ ] 동시성 충돌 처리 테스트
- [ ] 성능 부하 테스트
- [ ] 트랜잭션 성능 최적화

### 📋 Phase 5: Monitoring & Observability (Week 5)

#### Day 1-2: OpenTelemetry 통합
- [ ] `TelemetryCollector` 클래스 구현
- [ ] `ActivitySourceManager` 클래스 구현
- [ ] 분산 추적 구현
- [ ] 커스텀 메트릭 정의
- [ ] `TelemetryOrchestrationEngine` 구현
- [ ] OpenTelemetry 통합 테스트

#### Day 3: Health Checks 구현
- [ ] `OrchestrationHealthCheck` 클래스 구현
- [ ] `LLMHealthCheck` 클래스 구현
- [ ] `StateHealthCheck` 클래스 구현
- [ ] 종합 Health 대시보드
- [ ] Health Check 자동화
- [ ] Health 상태 모니터링

#### Day 4: Metrics & Logging
- [ ] `MetricsCollector` 클래스 구현
- [ ] `PrometheusExporter` 클래스 구현
- [ ] 구조화된 로깅 시스템
- [ ] 로그 레벨 동적 조정
- [ ] 메트릭 대시보드 구성
- [ ] 성능 메트릭 수집 테스트

#### Day 5: Distributed Tracing
- [ ] 분산 추적 미들웨어 구현
- [ ] 트레이스 컨텍스트 전파
- [ ] 마이크로서비스 추적 지원
- [ ] 트레이스 샘플링 구현
- [ ] 트레이스 시각화 연동
- [ ] 엔드투엔드 추적 테스트

### 📋 Phase 6: Testing & Documentation (Week 6)

#### Day 1-2: 통합 테스트 완성
- [ ] 전체 워크플로우 통합 테스트
- [ ] 실제 종속성을 사용한 E2E 테스트
- [ ] 다양한 시나리오별 테스트
- [ ] 오류 상황 복구 테스트
- [ ] 테스트 커버리지 80% 달성
- [ ] 테스트 자동화 파이프라인

#### Day 3: 부하 테스트
- [ ] 동시성 테스트 (100개 요청)
- [ ] 장기 실행 안정성 테스트
- [ ] 메모리 누수 검사
- [ ] 성능 벤치마크 수립
- [ ] 95th percentile 응답시간 5초 이내
- [ ] 부하 테스트 자동화

#### Day 4: API 문서 생성
- [ ] OpenAPI 스펙 자동 생성
- [ ] 코드 주석 기반 문서화
- [ ] 사용 예제 작성
- [ ] 통합 가이드 문서
- [ ] 문서 사이트 구축
- [ ] 문서 품질 검토

#### Day 5: 최종 검증 및 배포 준비
- [ ] 전체 기능 검증 체크리스트
- [ ] 성능 기준 달성 확인
- [ ] 보안 감사 수행
- [ ] 프로덕션 환경 설정 검토
- [ ] 배포 가이드 작성
- [ ] 프로덕션 배포 승인

#### 1.1 오케스트레이션 엔진 재설계 (우선순위 #1)
```csharp
// 현재 문제: 문자열 파싱 의존
private static string GetActionType(object action) {
    return action.ToString()?.Split('_')[0] ?? "unknown"; // 위험!
}

// 목표: 타입 안전한 액션 시스템
public interface IOrchestrationAction 
{
    ActionType Type { get; }
    string Name { get; }
    IReadOnlyDictionary<string, object> Parameters { get; }
    Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken);
}

public sealed record LLMAction(
    string FunctionName,
    IReadOnlyDictionary<string, object> Parameters) : IOrchestrationAction
{
    public ActionType Type => ActionType.LLM;
    public string Name => FunctionName;
    
    public async Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var function = context.Registry.GetLLMFunction(FunctionName);
        var llmContext = new LLMContext 
        {
            Parameters = Parameters,
            ExecutionHistory = context.ExecutionHistory,
            SharedData = context.SharedData
        };
        
        var result = await function.ExecuteAsync(llmContext, cancellationToken);
        return ActionResult.FromLLMResult(result);
    }
}

public sealed record ToolAction(
    string ToolName,
    IReadOnlyDictionary<string, object> Parameters) : IOrchestrationAction
{
    public ActionType Type => ActionType.Tool;
    public string Name => ToolName;
    
    public async Task<ActionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var tool = context.Registry.GetTool(ToolName);
        var toolInput = new ToolInput { Parameters = Parameters.ToDictionary(kv => kv.Key, kv => kv.Value) };
        
        var result = await tool.ExecuteAsync(toolInput, cancellationToken);
        return ActionResult.FromToolResult(result);
    }
}
```

#### 1.2 타입 안전한 Registry 시스템
```csharp
// SRP 준수: 각 Registry는 단일 책임
public interface ILLMFunctionRegistry
{
    void Register<T>() where T : class, ILLMFunction;
    void Register<T>(T instance) where T : class, ILLMFunction;
    T Resolve<T>() where T : class, ILLMFunction;
    ILLMFunction Resolve(string name);
    IEnumerable<ILLMFunction> GetAll();
}

public interface IToolRegistry  
{
    void Register<T>() where T : class, ITool;
    void Register<T>(T instance) where T : class, ITool;
    T Resolve<T>() where T : class, ITool;
    ITool Resolve(string name);
    IEnumerable<ITool> GetAll();
}

// DIP 준수: 고수준 모듈이 추상화에 의존
public class TypeSafeOrchestrationEngine : IOrchestrationEngine
{
    private readonly ILLMFunctionRegistry _llmRegistry;
    private readonly IToolRegistry _toolRegistry;
    private readonly IActionFactory _actionFactory;
    private readonly IStateManager _stateManager;
    private readonly ILogger<TypeSafeOrchestrationEngine> _logger;
    
    public TypeSafeOrchestrationEngine(
        ILLMFunctionRegistry llmRegistry,
        IToolRegistry toolRegistry, 
        IActionFactory actionFactory,
        IStateManager stateManager,
        ILogger<TypeSafeOrchestrationEngine> logger)
    {
        _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

#### 1.3 Configuration 시스템 완성
```csharp
// ISP 준수: 인터페이스 분리
public interface IConfigurationReader
{
    Task<T> GetAsync<T>(string key) where T : class;
    Task<T> GetRequiredAsync<T>(string key) where T : class;
}

public interface IConfigurationWriter
{
    Task SetAsync<T>(string key, T value) where T : class;
    Task RemoveAsync(string key);
}

public interface IConfigurationCache
{
    void Invalidate(string keyPattern = null);
    void InvalidateAll();
    Task WarmupAsync(IEnumerable<string> keys);
}

// 실제 구현: SRP 준수
public class ConfigurationManager : IConfigurationReader, IConfigurationWriter, IConfigurationCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentSet<string> _cacheKeys = new(); // 키 추적
    private readonly IOptionsMonitor<AIAgentOptions> _options;
    private readonly ILogger<ConfigurationManager> _logger;
    
    public void Invalidate(string keyPattern = null)
    {
        if (keyPattern == null)
        {
            // 전체 캐시 클리어 - 주석이 아닌 실제 구현!
            var keysToRemove = _cacheKeys.ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key);
            }
            _logger.LogInformation("전체 설정 캐시 무효화 완료: {Count}개", keysToRemove.Count);
        }
        else
        {
            // 패턴 매칭 캐시 클리어  
            var matchingKeys = _cacheKeys
                .Where(key => key.Contains(keyPattern, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            foreach (var key in matchingKeys)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key);
            }
            _logger.LogInformation("패턴 '{Pattern}' 설정 캐시 무효화 완료: {Count}개", keyPattern, matchingKeys.Count);
        }
    }
}
```

### Phase 2: State Management System (Week 2) 🏗️
**목표**: 분산 환경 지원 상태 지속성

```csharp
// 추상화 우선 설계 - DIP 준수
public interface IStateProvider
{
    Task<T> GetAsync<T>(string sessionId) where T : class;
    Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null) where T : class;
    Task<bool> ExistsAsync(string sessionId);
    Task DeleteAsync(string sessionId);
    Task<IStateTransaction> BeginTransactionAsync();
}

// LSP 준수: 모든 구현체가 동일한 계약 준수
public class RedisStateProvider : IStateProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<RedisStateProvider> _logger;
    
    public async Task<T> GetAsync<T>(string sessionId) where T : class
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        
        try
        {
            var db = _redis.GetDatabase();
            var json = await db.StringGetAsync($"session:{sessionId}");
            
            if (!json.HasValue)
            {
                _logger.LogDebug("세션 상태 없음: {SessionId}", sessionId);
                return null;
            }
            
            var state = _serializer.Deserialize<T>(json);
            _logger.LogDebug("세션 상태 복원: {SessionId}, Type: {Type}", sessionId, typeof(T).Name);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "세션 상태 조회 실패: {SessionId}", sessionId);
            throw new StateProviderException($"Failed to get state for session {sessionId}", ex);
        }
    }
    
    public async Task SetAsync<T>(string sessionId, T state, TimeSpan? expiry = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(state);
        
        try
        {
            var db = _redis.GetDatabase();
            var json = _serializer.Serialize(state);
            var key = $"session:{sessionId}";
            
            if (expiry.HasValue)
            {
                await db.StringSetAsync(key, json, expiry.Value);
            }
            else
            {
                await db.StringSetAsync(key, json);
            }
            
            _logger.LogDebug("세션 상태 저장: {SessionId}, Type: {Type}, Expiry: {Expiry}", 
                sessionId, typeof(T).Name, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "세션 상태 저장 실패: {SessionId}", sessionId);
            throw new StateProviderException($"Failed to set state for session {sessionId}", ex);
        }
    }
}

// OCP 준수: 새로운 구현체 추가 용이
public class SqlServerStateProvider : IStateProvider
{
    // SQL Server 기반 구현
}

public class InMemoryStateProvider : IStateProvider  
{
    // 개발/테스트용 메모리 기반 구현
}
```

### Phase 3: Complete LLM Integration (Week 3) 🤖
**목표**: 실제 사용 가능한 LLM Provider

```csharp
// 실제 토큰 카운팅 - 하드코딩 제거
public interface ITokenCounter
{
    int CountTokens(string text, string model);
    TokenUsage EstimateUsage(LLMRequest request);
    bool IsValidModel(string model);
}

public class TiktokenCounter : ITokenCounter
{
    private readonly ConcurrentDictionary<string, Encoding> _encodings = new();
    private static readonly Dictionary<string, string> ModelEncodings = new()
    {
        ["gpt-4"] = "cl100k_base",
        ["gpt-4-turbo"] = "cl100k_base", 
        ["gpt-3.5-turbo"] = "cl100k_base",
        ["claude-3-sonnet"] = "claude", // Claude용 별도 인코딩
        ["claude-3-5-sonnet"] = "claude"
    };
    
    public int CountTokens(string text, string model)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentException.ThrowIfNullOrEmpty(model);
        
        if (!IsValidModel(model))
            throw new ArgumentException($"지원되지 않는 모델: {model}", nameof(model));
            
        var encoding = GetOrCreateEncoding(model);
        return encoding.Encode(text).Count; // 실제 토큰 계산!
    }
    
    private Encoding GetOrCreateEncoding(string model)
    {
        return _encodings.GetOrAdd(model, modelName =>
        {
            var encodingName = ModelEncodings[modelName];
            return Encoding.Get(encodingName);
        });
    }
}

// 실제 API 호출 구현 - 가짜 구현 제거
public class ClaudeProvider : LLMProviderBase, IStreamingLLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ITokenCounter _tokenCounter;
    private readonly ITokenBudgetManager _budgetManager;
    
    public override async Task<LLMResponse> GenerateAsync(
        LLMRequest request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        // 토큰 예산 확인
        var estimatedUsage = _tokenCounter.EstimateUsage(request);
        if (!await _budgetManager.CanUseTokensAsync(estimatedUsage, cancellationToken))
        {
            throw new TokenBudgetExceededException($"토큰 예산 초과: {estimatedUsage.TotalTokens}");
        }
        
        // 실제 API 호출
        var httpRequest = CreateHttpRequest(request);
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        httpResponse.EnsureSuccessStatusCode();
        
        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = ParseResponse(responseContent);
        
        // 실제 사용량 기록
        await _budgetManager.RecordUsageAsync(response.TokensUsed, cancellationToken);
        
        return response;
    }
    
    public async IAsyncEnumerable<LLMStreamChunk> GenerateStreamAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateStreamingHttpRequest(request);
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line?.StartsWith("data: ", StringComparison.Ordinal) == true)
            {
                var chunk = ParseStreamChunk(line);
                if (chunk != null)
                    yield return chunk;
            }
        }
    }
}
```

## 🏛️ 아키텍처 원칙

### SOLID 원칙 엄격 적용

#### Single Responsibility Principle (SRP)
```csharp
// ✅ 단일 책임: 프롬프트 로딩만 담당
public class PromptLoader : IPromptLoader
{
    public async Task<string> LoadAsync(string promptName, CancellationToken cancellationToken = default)
    {
        // 파일에서 프롬프트 로딩만 담당
    }
}

// ✅ 단일 책임: 프롬프트 처리만 담당
public class PromptProcessor : IPromptProcessor  
{
    public string ProcessTemplate(string template, IReadOnlyDictionary<string, object> parameters)
    {
        // 템플릿 변수 치환만 담당
    }
}

// ✅ 단일 책임: 프롬프트 캐싱만 담당
public class PromptCache : IPromptCache
{
    public async Task<string> GetOrSetAsync(string key, Func<Task<string>> factory, TimeSpan expiry)
    {
        // 캐싱 로직만 담당
    }
}
```

#### Open/Closed Principle (OCP)
```csharp
// 새로운 전략 추가 시 기존 코드 수정 불필요
public abstract class OrchestrationStrategyBase : IOrchestrationStrategy
{
    protected readonly ILogger Logger;
    
    protected OrchestrationStrategyBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract bool CanHandle(IOrchestrationContext context);
    public abstract Task<IOrchestrationResult> ExecuteAsync(IOrchestrationContext context, CancellationToken cancellationToken);
    
    protected virtual void LogStrategySelection(IOrchestrationContext context)
    {
        Logger.LogInformation("전략 선택됨: {Strategy}, 세션: {SessionId}", Name, context.SessionId);
    }
}

// 새로운 전략 추가
public class HybridReasoningStrategy : OrchestrationStrategyBase
{
    public HybridReasoningStrategy(ILogger<HybridReasoningStrategy> logger) : base(logger) { }
    
    public override string Name => "Hybrid";
    public override string Description => "추론과 계획 수립을 혼합한 하이브리드 전략";
    
    public override bool CanHandle(IOrchestrationContext context)
    {
        // 복잡도와 분석 요구사항 동시 평가
        return context.GetComplexityScore() > 0.6 && context.RequiresAnalysis();
    }
    
    public override async Task<IOrchestrationResult> ExecuteAsync(IOrchestrationContext context, CancellationToken cancellationToken)
    {
        LogStrategySelection(context);
        // 하이브리드 실행 로직
        return await ExecuteHybridAsync(context, cancellationToken);
    }
}
```

#### Liskov Substitution Principle (LSP)
```csharp
// 모든 구현체가 동일한 계약을 준수해야 함
public abstract class ToolBase : ITool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Category { get; }
    public abstract IToolContract Contract { get; }
    
    // LSP: 모든 하위 클래스에서 동일한 보장 제공
    public virtual async Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        
        try
        {
            // 전처리: 모든 도구에서 동일한 검증
            await ValidateInputAsync(input, cancellationToken);
            
            // 실제 실행: 하위 클래스에서 구현
            var result = await ExecuteInternalAsync(input, cancellationToken);
            
            // 후처리: 모든 도구에서 동일한 메타데이터 추가
            AddExecutionMetadata(result);
            
            return result;
        }
        catch (Exception ex)
        {
            // LSP: 모든 구현체에서 동일한 예외 처리 보장
            return CreateFailureResult(ex);
        }
    }
    
    protected abstract Task<IToolResult> ExecuteInternalAsync(IToolInput input, CancellationToken cancellationToken);
    
    protected virtual async Task ValidateInputAsync(IToolInput input, CancellationToken cancellationToken)
    {
        foreach (var requiredParam in Contract.RequiredParameters)
        {
            if (!input.Parameters.ContainsKey(requiredParam))
                throw new ArgumentException($"필수 파라미터 누락: {requiredParam}");
        }
    }
    
    private IToolResult CreateFailureResult(Exception ex)
    {
        return new ToolResult
        {
            IsSuccess = false,
            ErrorMessage = ex.Message,
            ExecutionTime = TimeSpan.Zero,
            Metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["error_type"] = ex.GetType().Name,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };
    }
}
```

#### Interface Segregation Principle (ISP)
```csharp
// 역할별로 인터페이스 분리
public interface IExecutable
{
    Task<IResult> ExecuteAsync(IInput input, CancellationToken cancellationToken = default);
}

public interface IValidatable  
{
    Task<ValidationResult> ValidateAsync(IInput input);
}

public interface IDescriptive
{
    string Name { get; }
    string Description { get; }
    string Category { get; }
}

public interface ICacheable
{
    bool IsCacheable { get; }
    string GetCacheKey(IInput input);
    TimeSpan CacheTTL { get; }
}

// 필요한 인터페이스만 구현
public class WebSearchTool : IExecutable, IValidatable, IDescriptive
{
    // 캐싱이 필요 없으므로 ICacheable 구현하지 않음
    public string Name => "web_search";
    public string Description => "웹 검색 기능";
    public string Category => "Search";
    
    public async Task<IResult> ExecuteAsync(IInput input, CancellationToken cancellationToken = default)
    {
        // 웹 검색 실행
    }
    
    public async Task<ValidationResult> ValidateAsync(IInput input)
    {
        // 입력 검증
    }
}

public class DatabaseTool : IExecutable, IValidatable, IDescriptive, ICacheable
{
    // 데이터베이스 조회는 캐싱 필요
    public bool IsCacheable => true;
    public TimeSpan CacheTTL => TimeSpan.FromMinutes(10);
    
    public string GetCacheKey(IInput input)
    {
        var query = input.Parameters["query"]?.ToString() ?? "";
        return $"db_query_{query.GetHashCode()}";
    }
    
    // 나머지 구현...
}
```

#### Dependency Inversion Principle (DIP)
```csharp
// 고수준 모듈이 추상화에 의존
public class StatefulOrchestrationEngine : IOrchestrationEngine
{
    // 모든 의존성이 추상화 (인터페이스)
    private readonly IOrchestrationStrategy _strategy;
    private readonly IStateProvider _stateProvider;
    private readonly IActionFactory _actionFactory;
    private readonly IResiliencePipeline _resilience;
    private readonly ITelemetryCollector _telemetry;
    private readonly ILogger<StatefulOrchestrationEngine> _logger;
    
    public StatefulOrchestrationEngine(
        IOrchestrationStrategy strategy,
        IStateProvider stateProvider,
        IActionFactory actionFactory,
        IResiliencePipeline resilience,
        ITelemetryCollector telemetry,
        ILogger<StatefulOrchestrationEngine> logger)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
        _resilience = resilience ?? throw new ArgumentNullException(nameof(resilience));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = _telemetry.StartActivity("orchestration.execute");
        activity?.SetTag("session.id", request.SessionId);
        
        return await _resilience.ExecuteAsync(async ct =>
        {
            // 상태 복원 또는 생성
            var context = await _stateProvider.GetAsync<OrchestrationContext>(request.SessionId) 
                ?? new OrchestrationContext(request);
                
            try
            {
                var result = await _strategy.ExecuteAsync(context, ct);
                
                // 상태 저장
                await _stateProvider.SetAsync(request.SessionId, context, TimeSpan.FromHours(24));
                
                _telemetry.RecordSuccess("orchestration.execute");
                return result;
            }
            catch (Exception ex)
            {
                // 실패 시에도 상태 저장 (복구용)
                context.LastError = ex;
                await _stateProvider.SetAsync(request.SessionId, context, TimeSpan.FromHours(1));
                
                _telemetry.RecordFailure("orchestration.execute", ex);
                throw;
            }
        }, cancellationToken);
    }
}
```

## 🎨 클린 코드 원칙

### 의미 있는 이름
```csharp
// ✅ 의도가 명확한 이름
public class OrchestrationExecutionContext
{
    public string SessionId { get; }
    public DateTime StartedAt { get; }
    public IReadOnlyList<IExecutionStep> ExecutionHistory { get; }
    public IReadOnlyDictionary<string, object> SharedData { get; }
    
    // 비즈니스 의미가 명확한 메서드명
    public void RecordSuccessfulExecution(IExecutionStep step)
    {
        // 구현
    }
    
    public void RecordFailedExecution(IExecutionStep step, Exception error)
    {
        // 구현  
    }
    
    public bool HasReachedMaxRetryCount(int maxRetries)
    {
        // 구현
    }
}

// ❌ 의도가 불분명한 이름
public class Context
{
    public string Id { get; }
    public DateTime Time { get; }
    public List<object> History { get; }
    public Dictionary<string, object> Data { get; }
    
    public void Process(object step) { }
    public void Handle(object step, Exception ex) { }
    public bool Check(int max) { }
}
```

### 함수는 작고 한 가지 일만
```csharp
// ✅ 단일 책임 함수들
public class OrchestrationPlanParser
{
    public OrchestrationPlan ParsePlan(string planJson)
    {
        ValidateJsonFormat(planJson);
        var planData = DeserializePlan(planJson);
        var actions = ExtractActions(planData);
        return CreatePlan(actions);
    }
    
    private void ValidateJsonFormat(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("계획 JSON이 비어있습니다", nameof(json));
            
        if (!IsValidJson(json))
            throw new ArgumentException("유효하지 않은 JSON 형식입니다", nameof(json));
    }
    
    private PlanData DeserializePlan(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PlanData>(json, JsonOptions.Default);
        }
        catch (JsonException ex)
        {
            throw new PlanParsingException("계획 역직렬화 실패", ex);
        }
    }
    
    private IReadOnlyList<IOrchestrationAction> ExtractActions(PlanData planData)
    {
        return planData.Steps
            .Select(CreateActionFromStep)
            .ToList()
            .AsReadOnly();
    }
    
    private OrchestrationPlan CreatePlan(IReadOnlyList<IOrchestrationAction> actions)
    {
        return new OrchestrationPlan
        {
            Id = Guid.NewGuid().ToString(),
            Actions = actions,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

### 주석보다는 코드로 의도 표현
```csharp
// ✅ 코드 자체가 의도를 설명
public class TokenBudgetManager
{
    private readonly int _dailyTokenLimit;
    private readonly int _hourlyTokenLimit;
    
    public async Task<bool> CanUseTokensAsync(TokenUsage requestedUsage)
    {
        var currentDailyUsage = await GetDailyTokenUsageAsync();
        var currentHourlyUsage = await GetHourlyTokenUsageAsync();
        
        return IsWithinDailyLimit(currentDailyUsage, requestedUsage) &&
               IsWithinHourlyLimit(currentHourlyUsage, requestedUsage);
    }
    
    private bool IsWithinDailyLimit(int currentUsage, TokenUsage requested)
    {
        return currentUsage + requested.TotalTokens <= _dailyTokenLimit;
    }
    
    private bool IsWithinHourlyLimit(int currentUsage, TokenUsage requested)
    {
        return currentUsage + requested.TotalTokens <= _hourlyTokenLimit;
    }
}

// ❌ 주석에 의존하는 코드
public class TokenBudgetManager
{
    public async Task<bool> CanUse(TokenUsage usage)
    {
        // 일일 사용량 확인
        var daily = await GetUsage(1);
        // 시간당 사용량 확인  
        var hourly = await GetUsage(2);
        
        // 한도 내인지 확인
        return daily + usage.Total <= 10000 && hourly + usage.Total <= 1000;
    }
}
```

### 예외를 활용한 에러 처리
```csharp
// ✅ 구체적이고 의미 있는 예외
public class LLMProvider
{
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        ThrowIfInvalidRequest(request);
        
        try
        {
            return await CallLLMServiceAsync(request);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit"))
        {
            throw new RateLimitExceededException("API 호출 한도 초과", ex);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("unauthorized"))
        {
            throw new AuthenticationFailedException("API 키 인증 실패", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new LLMServiceException("LLM 서비스 호출 실패", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new LLMTimeoutException("LLM 호출 시간 초과", ex);
        }
    }
    
    private static void ThrowIfInvalidRequest(LLMRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("프롬프트가 비어있습니다", nameof(request));
            
        if (request.MaxTokens <= 0)
            throw new ArgumentException("최대 토큰 수는 양수여야 합니다", nameof(request));
    }
}

// 도메인별 예외 정의
public abstract class AIAgentException : Exception
{
    protected AIAgentException(string message) : base(message) { }
    protected AIAgentException(string message, Exception innerException) : base(message, innerException) { }
}

public class RateLimitExceededException : AIAgentException
{
    public RateLimitExceededException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class AuthenticationFailedException : AIAgentException
{
    public AuthenticationFailedException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

## 🔧 코딩 스타일 가이드

### C# 코딩 컨벤션 준수
참조: `.kiro/steering/dotnet-coding-standards.md`

### 추가 품질 규칙

#### Nullable Reference Types 활용
```csharp
#nullable enable

public class OrchestrationContext
{
    public string SessionId { get; } = null!; // 생성자에서 초기화됨을 보장
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; set; } // null 가능
    public string? LastErrorMessage { get; set; } // null 가능
    
    public OrchestrationContext(string sessionId)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(string? finalMessage = null)
    {
        CompletedAt = DateTime.UtcNow;
        LastErrorMessage = finalMessage; // null 명시적 허용
    }
}
```

#### Record Types 활용
```csharp
// 불변 데이터 모델
public sealed record TokenUsage(int PromptTokens, int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal EstimatedCost => TotalTokens * 0.0001m; // 예시 가격
}

public sealed record LLMRequest(
    string Prompt,
    string Model,
    int MaxTokens,
    decimal Temperature = 0.7m,
    CancellationToken CancellationToken = default)
{
    public static LLMRequest Create(string prompt, string model = "gpt-4")
    {
        ArgumentException.ThrowIfNullOrEmpty(prompt);
        ArgumentException.ThrowIfNullOrEmpty(model);
        
        return new LLMRequest(prompt, model, MaxTokens: 4096);
    }
}
```

#### Pattern Matching 적극 활용
```csharp
public string GetActionDescription(IOrchestrationAction action) => action switch
{
    LLMAction llm => $"LLM 기능 실행: {llm.FunctionName}",
    ToolAction tool => $"도구 실행: {tool.ToolName}",
    DelayAction delay => $"{delay.Duration.TotalSeconds}초 대기",
    ConditionalAction conditional => $"조건부 실행: {conditional.Condition}",
    _ => $"알 수 없는 액션: {action.GetType().Name}"
};

public async Task<ActionResult> ProcessActionAsync(IOrchestrationAction action) => action switch
{
    LLMAction llm => await ExecuteLLMActionAsync(llm),
    ToolAction tool => await ExecuteToolActionAsync(tool), 
    DelayAction delay => await ExecuteDelayActionAsync(delay),
    ConditionalAction conditional when await EvaluateConditionAsync(conditional.Condition) 
        => await ProcessActionAsync(conditional.ThenAction),
    ConditionalAction conditional 
        => conditional.ElseAction != null 
            ? await ProcessActionAsync(conditional.ElseAction) 
            : ActionResult.Skipped,
    _ => throw new NotSupportedException($"지원되지 않는 액션 타입: {action.GetType().Name}")
};
```

## 📈 품질 메트릭

### 목표 지표
- **코드 커버리지**: 80% 이상
- **순환 복잡도**: 클래스당 평균 5 이하
- **유지보수성 지수**: 80점 이상
- **기술 부채 비율**: 5% 이하
- **SOLID 원칙 준수율**: 95% 이상

### 지속적 품질 관리
```csharp
// 품질 게이트 자동화
public class QualityGateChecker
{
    public async Task<QualityReport> CheckQualityAsync(string projectPath)
    {
        var report = new QualityReport();
        
        // 정적 분석
        report.CodeCoverage = await RunCodeCoverageAsync(projectPath);
        report.CyclomaticComplexity = await AnalyzeComplexityAsync(projectPath);
        report.TechnicalDebt = await CalculateTechnicalDebtAsync(projectPath);
        
        // SOLID 원칙 준수 검사
        report.SOLIDCompliance = await CheckSOLIDComplianceAsync(projectPath);
        
        // 보안 취약점 검사
        report.SecurityIssues = await RunSecurityScanAsync(projectPath);
        
        return report;
    }
}
```

## 🎯 성공 기준

### 완료 조건
- [ ] 모든 하드코딩 제거 (100%)
- [ ] 타입 안전성 확보 (컴파일 타임 검증)
- [ ] SOLID 원칙 준수 (95% 이상)
- [ ] 클린 코드 원칙 적용 (코드 리뷰 통과)
- [ ] 테스트 커버리지 80% 이상
- [ ] 성능 기준 달성 (응답시간 < 2초)
- [ ] 프로덕션 배포 가능 (안정성 검증)

## 🚫 코드 품질 금지 사항

### 절대 금지되는 패턴들

#### 1. 의미 없는 리턴 값 금지
```csharp
// ❌ 절대 금지 - 의미 없는 return true
public virtual Task<bool> ValidateAsync(IToolInput input)
{
    // 실제 검증 로직 없이
    return Task.FromResult(true); // 이런 코드 절대 금지!
}

// ✅ 올바른 구현 - 실제 검증 로직
public virtual async Task<ValidationResult> ValidateAsync(IToolInput input)
{
    if (input == null)
        return ValidationResult.Failed("입력이 null입니다");
        
    if (input.Parameters == null || input.Parameters.Count == 0)
        return ValidationResult.Failed("파라미터가 없습니다");
        
    foreach (var requiredParam in Contract.RequiredParameters)
    {
        if (!input.Parameters.ContainsKey(requiredParam))
            return ValidationResult.Failed($"필수 파라미터 누락: {requiredParam}");
    }
    
    return ValidationResult.Success();
}
```

#### 2. 임시/테스트 코드 삽입 금지
```csharp
// ❌ 절대 금지 - 임시 테스트 코드
public async Task<LLMResponse> GenerateAsync(LLMRequest request)
{
    // TODO: 임시로 하드코딩
    await Task.Delay(100); // 임시 지연
    return new LLMResponse { Content = "test response" }; // 가짜 응답 금지!
}

// ✅ 올바른 구현 - 완전한 실제 구현
public async Task<LLMResponse> GenerateAsync(LLMRequest request)
{
    ValidateRequest(request);
    
    var httpRequest = CreateHttpRequest(request);
    var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
    
    httpResponse.EnsureSuccessStatusCode();
    
    var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
    return ParseResponse(responseContent);
}
```

#### 3. 불완전한 예외 처리 금지
```csharp
// ❌ 절대 금지 - 빈 catch 블록
public async Task<IToolResult> ExecuteAsync(IToolInput input)
{
    try
    {
        // 실행 로직
    }
    catch
    {
        // 빈 catch 블록 절대 금지!
    }
    
    return null; // null 반환도 금지
}

// ✅ 올바른 구현 - 완전한 예외 처리
public async Task<IToolResult> ExecuteAsync(IToolInput input)
{
    try
    {
        var result = await ExecuteInternalAsync(input);
        return ToolResult.Success(result);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "잘못된 입력 파라미터: {ToolName}", Name);
        return ToolResult.Failed($"입력 오류: {ex.Message}");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "네트워크 오류: {ToolName}", Name);
        return ToolResult.Failed($"네트워크 오류: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "도구 실행 실패: {ToolName}", Name);
        return ToolResult.Failed($"실행 실패: {ex.Message}");
    }
}
```

#### 4. 하드코딩된 값 금지
```csharp
// ❌ 절대 금지 - 하드코딩된 설정값
public class ClaudeProvider : ILLMProvider
{
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"); // 하드코딩 금지!
        httpRequest.Headers.Add("anthropic-version", "2023-06-01"); // 하드코딩 금지!
        httpRequest.Headers.Add("x-api-key", "sk-ant-api03-..."); // 절대 금지!
    }
}

// ✅ 올바른 구현 - 설정 기반
public class ClaudeProvider : ILLMProvider
{
    private readonly ClaudeOptions _options;
    private readonly HttpClient _httpClient;
    
    public ClaudeProvider(IOptions<ClaudeOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }
    
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/messages");
        httpRequest.Headers.Add("anthropic-version", _options.ApiVersion);
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        
        // 실제 구현...
    }
}
```

#### 5. Magic Number/String 금지
```csharp
// ❌ 절대 금지 - Magic Number/String
public class TokenBudgetManager
{
    public async Task<bool> CanUseTokensAsync(int requestedTokens)
    {
        var dailyUsage = await GetDailyUsageAsync();
        return dailyUsage + requestedTokens <= 10000; // Magic Number 금지!
    }
    
    public async Task RecordUsageAsync(string model, int tokens)
    {
        if (model == "gpt-4") // Magic String 금지!
        {
            // 처리 로직
        }
    }
}

// ✅ 올바른 구현 - 상수 및 설정 사용
public class TokenBudgetManager
{
    private const int DEFAULT_DAILY_TOKEN_LIMIT = 10_000;
    private const int DEFAULT_HOURLY_TOKEN_LIMIT = 1_000;
    
    private readonly TokenLimits _limits;
    
    public TokenBudgetManager(IOptions<TokenLimits> limits)
    {
        _limits = limits.Value;
    }
    
    public async Task<bool> CanUseTokensAsync(int requestedTokens)
    {
        var dailyUsage = await GetDailyUsageAsync();
        var dailyLimit = _limits.DailyLimit ?? DEFAULT_DAILY_TOKEN_LIMIT;
        
        return dailyUsage + requestedTokens <= dailyLimit;
    }
    
    public async Task RecordUsageAsync(string model, int tokens)
    {
        var modelConfig = _limits.ModelConfigurations
            .FirstOrDefault(c => c.ModelName.Equals(model, StringComparison.OrdinalIgnoreCase));
            
        if (modelConfig != null)
        {
            // 모델별 처리 로직
        }
    }
}
```

### 코드 완성도 요구사항

#### 1. 모든 메서드는 완전한 구현
- 임시 구현, TODO 주석 금지
- 실제 비즈니스 로직 완전 구현
- 모든 예외 상황 처리

#### 2. 모든 클래스는 단일 파일
- 1 Class = 1 File 엄격 준수
- 부분 클래스(partial class) 사용 금지
- 중첩 클래스는 Private으로만 허용

#### 3. 의미 있는 반환값
- `return true/false` 대신 구체적인 결과 객체
- `return null` 대신 빈 컬렉션이나 결과 객체
- 성공/실패 정보와 상세 메시지 포함

#### 4. 완전한 검증 로직
- 입력 파라미터 null 체크
- 비즈니스 규칙 검증
- 데이터 형식 및 범위 검증
- 의미 있는 오류 메시지

## 📁 폴더 구조 엄격 규칙

### 필수 준수사항
1. **최대 깊이 4레벨**: `src/Project/Category/Subcategory/`
2. **의미적 그룹핑**: 관련 기능끼리 묶기
3. **Base 클래스 격리**: 추상 클래스는 `Base/` 폴더
4. **파일명 = 클래스명**: 정확히 일치해야 함
5. **폴더당 최대 7개 파일**: 초과 시 하위 폴더 생성

### 금지되는 구조
```
❌ 너무 깊은 구조 (5레벨 이상)
src/AIAgentFramework.Core/Abstractions/Orchestration/Engines/Strategies/Base/

❌ 한 폴더에 너무 많은 파일 (8개 이상)
src/AIAgentFramework.Tools/
├── WebSearchTool.cs
├── DatabaseTool.cs  
├── FileSystemTool.cs
├── EmailTool.cs
├── SlackTool.cs
├── DiscordTool.cs
├── TwitterTool.cs
├── GitHubTool.cs  # 8개째 - 분리 필요!

❌ 의미 불분명한 폴더명
src/AIAgentFramework.Core/Utils/
src/AIAgentFramework.Core/Helpers/
src/AIAgentFramework.Core/Common/
```

### 권장되는 구조
```
✅ 명확하고 체계적인 구조
src/AIAgentFramework.Tools/
├── BuiltIn/
│   ├── Search/
│   │   └── WebSearchTool.cs
│   ├── Data/  
│   │   ├── DatabaseTool.cs
│   │   └── VectorDBTool.cs
│   └── System/
│       └── FileSystemTool.cs
├── Integration/
│   ├── Communication/
│   │   ├── EmailTool.cs
│   │   ├── SlackTool.cs
│   │   └── DiscordTool.cs
│   └── Social/
│       ├── TwitterTool.cs
│       └── GitHubTool.cs
```

이 가이드라인을 준수하여 **엔터프라이즈급 AI Agent 플랫폼**을 완성합니다.