# 🔄 Pending Tasks - 추후 구현 예정

## 📋 우선순위별 정리

---

## 🚨 **High Priority (필수)**

### 1. **JSON Schema 정의**
**목적**: 타입 안전성과 API 계약 보장

**필요한 Schema 파일들:**
```
schemas/core/
├── Plan.schema.json              # 실행 계획 구조
├── ExecutionStep.schema.json     # 실행 단계 구조  
├── ToolDescriptor.schema.json    # 도구 설명 구조
├── ToolResult.schema.json        # 도구 실행 결과
├── AgentRequest.schema.json      # 에이전트 요청
├── AgentResponse.schema.json     # 에이전트 응답
├── LlmRequest.schema.json        # LLM 요청 구조
└── LlmResponse.schema.json       # LLM 응답 구조
```

**고려사항:**
- **버전 관리**: Schema 버전 전략 (v1.0, v1.1, v2.0)
- **하위 호환성**: 기존 시스템 영향 최소화
- **검증 정책**: 실패 시 예외 vs 경고 vs 기본값 적용
- **성능**: Schema 캐싱 및 부분 검증
- **확장성**: 사용자 정의 도구의 자체 Schema 정의

**예상 작업량**: 3-5일

---

### 2. **Agent 구현체 (DefaultAgent)**
**목적**: IAgent 인터페이스의 실제 구현

**구현 요소:**
- Orchestration 시스템 연동
- LLM 클라이언트 사용
- Tools 실행 관리
- Memory 시스템 통합
- 스트리밍 응답 처리

**예상 작업량**: 1-2주

---

### 3. **내부 메모리 도구 구현**
**목적**: IMemoryManager의 구체적 구현

**구현 요소:**
- 인메모리 구현 (개발/테스트용)
- 영구 저장소 구현 (파일 기반)
- 대화 기록 관리
- 컨텍스트 압축/요약
- 토큰 관리

**예상 작업량**: 1주

---

## ⚡ **Medium Priority (중요)**

### 4. **기본 내부 도구들**
- **FileSystem Tool**: 파일 읽기/쓰기/검색
- **WebSearch Tool**: 웹 검색 기능
- **CodeAnalysis Tool**: 코드 분석 및 이해

**예상 작업량**: 각 1-2일

---

### 5. **Plan 실행 엔진**
**목적**: ExecutionStep들을 실제로 실행하는 엔진

**구현 요소:**
- StepKind.LlmCall → LLM 호출
- StepKind.ToolCall → Tool 실행
- 의존성 관리 및 순서 보장
- 병렬 실행 지원
- 재시도 정책 적용

**예상 작업량**: 1-2주

---

### 6. **통합 테스트**
- End-to-End 테스트 시나리오
- Agent → Orchestration → LLM/Tools/Memory 전체 플로우
- 성능 테스트 및 벤치마크

**예상 작업량**: 1주

---

## 🔮 **Low Priority (향후)**

### 7. **플러그인 시스템**
- 외부 도구 플러그인 로딩
- 플러그인 격리 및 보안
- 플러그인 레지스트리

### 8. **MCP 통합**
- MCP 프로토콜 지원
- MCP 도구 래퍼

### 9. **성능 최적화**
- 메모리 사용량 최적화
- 응답 시간 개선
- 캐싱 전략

### 10. **모니터링 및 관찰성**
- 메트릭 수집
- 로깅 시스템
- 분산 추적

---

## 📊 **기술적 부채**

### 1. **네이밍 일관성**
- 일부 클래스/인터페이스 네이밍 통일 필요
- 네임스페이스 구조 정리

### 2. **문서화**
- API 문서 자동 생성
- 사용 예제 및 가이드
- 아키텍처 다이어그램

### 3. **예외 처리**
- 표준 예외 계층 구조
- 오류 코드 체계
- 복구 전략 가이드라인

---

## ⏰ **마일스톤**

| 마일스톤 | 목표일 | 주요 구성요소 |
|----------|--------|---------------|
| **Alpha** | TBD | IAgent + DefaultAgent + 기본 Memory |
| **Beta** | TBD | JSON Schema + Plan 실행 엔진 |
| **RC** | TBD | 내부 도구들 + 통합 테스트 |
| **v1.0** | TBD | 완전한 기능 + 문서화 |

---

## 📝 **참고사항**

- 이 문서는 현재(2024-12-24) 기준으로 작성됨
- 우선순위는 프로젝트 진행상황에 따라 변경 가능
- 새로운 요구사항 발견 시 이 문서에 추가

---

## 🤝 **기여 방법**

1. 각 작업에 대한 상세 설계 문서 작성
2. 구현 전 인터페이스 리뷰
3. 단위 테스트 우선 작성
4. 코드 리뷰 및 문서화

**마지막 업데이트**: 2024-12-24


## 📋 Pending Tasks 구현 보강 사항

### 1. **JSON Schema 정의 - 상세 구현**

#### Core Schemas
```json
// schemas/core/Plan.schema.json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "agent.core.plan",
  "title": "Execution Plan",
  "type": "object",
  "required": ["id", "type", "steps"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[a-zA-Z0-9-_]+$"
    },
    "type": {
      "type": "string",
      "enum": ["simple", "fixed", "planner", "reactive"]
    },
    "steps": {
      "type": "array",
      "items": { "$ref": "ExecutionStep.schema.json" },
      "minItems": 1
    },
    "context": {
      "type": "object",
      "additionalProperties": true
    },
    "settings": { "$ref": "PlanSettings.schema.json" }
  }
}
```

#### 추가 필요 스키마들
```csharp
// schemas/extended/
- StreamingConfig.schema.json    // 스트리밍 설정
- ToolConfig.schema.json        // 도구 설정
- LlmConfig.schema.json         // LLM 설정
- WorkflowDefinition.schema.json // 워크플로우 정의
- ErrorResponse.schema.json     // 에러 응답
- MetricsReport.schema.json     // 메트릭 리포트
- AuditLog.schema.json         // 감사 로그
```

#### Schema 버전 관리 전략
```csharp
// src/Agent.Schema/Versioning/
- SchemaVersionManager.cs
- SchemaUpgrader.cs
- BackwardCompatibilityChecker.cs
- MigrationScript.cs

// 버전 구조
schemas/
  v1.0/
    core/
    tools/
  v1.1/
    core/
    tools/
    migrations/
      v1.0-to-v1.1.json
```

### 2. **DefaultAgent 구현 - 완전한 구현체**

```csharp
// src/Agent.Core/DefaultAgent.cs
public class DefaultAgent : IAgent
{
    private readonly IOrchestrationSelector _selector;
    private readonly IExecutorFactory _executorFactory;
    private readonly ILlmRegistry _llmRegistry;
    private readonly IToolRegistry _toolRegistry;
    private readonly IMemoryManager _memoryManager;
    private readonly IStreamingAggregator _streamingAggregator;
    private readonly ILogger<DefaultAgent> _logger;
    private readonly IMetricsCollector _metrics;
    
    // 핵심 기능
    - 대화 컨텍스트 관리
    - 실행 파이프라인 구성
    - 에러 처리 및 복구
    - 상태 관리
    - 이벤트 발행
}

// 추가 구현 필요 컴포넌트
- AgentBuilder.cs (빌더 패턴)
- AgentFactory.cs (팩토리 패턴)
- AgentPool.cs (에이전트 풀링)
- AgentLifecycleManager.cs (생명주기 관리)
```

### 3. **내부 메모리 도구 구현 - 완전한 시스템**

#### 인메모리 구현
```csharp
// src/Agent.Memory/InMemory/
- InMemoryManager.cs
- MemoryIndex.cs (검색 인덱스)
- MemoryCompressor.cs (압축)
- MemoryEvictionPolicy.cs (퇴출 정책)

public class InMemoryManager : IMemoryManager
{
    private readonly ConcurrentDictionary<string, ConversationEntry> _conversations;
    private readonly IMemoryIndex _index;
    private readonly ICompressor _compressor;
    private readonly IEvictionPolicy _evictionPolicy;
    
    // LRU, LFU, FIFO 정책 지원
    // 시맨틱 검색 지원
    // 자동 요약 및 압축
}
```

#### 영구 저장소 구현
```csharp
// src/Agent.Memory/Persistent/
- SqliteMemoryManager.cs
- PostgresMemoryManager.cs
- RedisMemoryManager.cs
- MongoMemoryManager.cs

// 하이브리드 구현
- HybridMemoryManager.cs (인메모리 + 영구 저장소)
- MemorySynchronizer.cs (동기화)
- MemoryReplicator.cs (복제)
```

#### 고급 메모리 기능
```csharp
// src/Agent.Memory/Advanced/
- SemanticMemory.cs (시맨틱 검색)
- EpisodicMemory.cs (에피소드 메모리)
- WorkingMemory.cs (작업 메모리)
- LongTermMemory.cs (장기 메모리)
- MemoryConsolidator.cs (메모리 통합)
```

### 4. **기본 내부 도구들 - 완전한 구현**

#### FileSystem Tool
```csharp
// src/Agent.Tools.BuiltIn/FileSystem/
- FileSystemTool.cs
- FileOperations.cs
- PathValidator.cs
- PermissionChecker.cs

기능:
- 파일 읽기/쓰기/삭제
- 디렉토리 탐색
- 파일 검색 (glob 패턴)
- 메타데이터 조회
- 압축/압축해제
- 파일 감시
- 안전한 샌드박스 실행
```

#### WebSearch Tool
```csharp
// src/Agent.Tools.BuiltIn/WebSearch/
- WebSearchTool.cs
- SearchProviders/
  - GoogleSearchProvider.cs
  - BingSearchProvider.cs
  - DuckDuckGoProvider.cs
- WebScraper.cs
- ContentExtractor.cs
- SearchResultRanker.cs

기능:
- 멀티 프로바이더 지원
- 결과 집계 및 순위화
- 웹 스크래핑
- 콘텐츠 추출 및 요약
- 캐싱
```

#### CodeAnalysis Tool
```csharp
// src/Agent.Tools.BuiltIn/CodeAnalysis/
- CodeAnalysisTool.cs
- LanguageAnalyzers/
  - CSharpAnalyzer.cs
  - PythonAnalyzer.cs
  - JavaScriptAnalyzer.cs
- SyntaxTreeBuilder.cs
- CodeMetricsCalculator.cs
- DependencyAnalyzer.cs
- SecurityScanner.cs

기능:
- 구문 분석
- 코드 메트릭 계산
- 의존성 분석
- 보안 취약점 스캔
- 리팩토링 제안
- 문서 생성
```

#### 추가 유용한 도구들
```csharp
// src/Agent.Tools.BuiltIn/

// DatabaseTool
- SQL 쿼리 실행
- 스키마 조회
- 데이터 마이그레이션

// HttpTool  
- REST API 호출
- GraphQL 쿼리
- WebSocket 통신

// ShellTool
- 명령어 실행
- 프로세스 관리
- 시스템 정보 조회

// MathTool
- 수식 계산
- 통계 분석
- 그래프 생성

// DataTransformTool
- 데이터 변환
- 포맷 변경
- 검증 및 정리
```

### 5. **Plan 실행 엔진 - 완전한 구현**

```csharp
// src/Agent.Execution/
public class PlanExecutionEngine
{
    private readonly IStepRunner _stepRunner;
    private readonly IDependencyResolver _dependencyResolver;
    private readonly IExecutionScheduler _scheduler;
    private readonly IStateManager _stateManager;
    private readonly ICheckpointManager _checkpointManager;
    
    public async IAsyncEnumerable<ExecutionEvent> ExecuteAsync(
        Plan plan, 
        ExecutionContext context)
    {
        // DAG 구성 및 검증
        var dag = await _dependencyResolver.BuildDAG(plan);
        
        // 실행 스케줄링
        var schedule = await _scheduler.CreateSchedule(dag);
        
        // 체크포인트 설정
        await _checkpointManager.Initialize(plan.Id);
        
        // 실행 루프
        while (schedule.HasNext())
        {
            var batch = schedule.GetNextBatch();
            var tasks = batch.Select(step => ExecuteStepAsync(step));
            
            await Task.WhenAll(tasks);
            
            yield return new BatchCompletedEvent(batch);
        }
    }
}
```

#### 실행 스케줄러
```csharp
// src/Agent.Execution/Scheduling/
- TopologicalScheduler.cs (토폴로지 정렬)
- PriorityScheduler.cs (우선순위 기반)
- ResourceAwareScheduler.cs (리소스 고려)
- AdaptiveScheduler.cs (적응형)
```

#### 병렬 실행 최적화
```csharp
// src/Agent.Execution/Parallelization/
- ParallelExecutor.cs
- TaskBatcher.cs
- ResourcePool.cs
- ConcurrencyLimiter.cs
```

### 6. **통합 테스트 - 완전한 테스트 스위트**

```csharp
// tests/Integration/

// 시나리오 기반 테스트
- SimpleQueryTests.cs
- ComplexWorkflowTests.cs
- ToolIntegrationTests.cs
- LlmIntegrationTests.cs
- MemoryPersistenceTests.cs
- ErrorRecoveryTests.cs
- PerformanceTests.cs

// 부하 테스트
- LoadTests.cs
- StressTests.cs
- SpikeTests.cs
- VolumeTests.cs

// 카오스 테스트
- NetworkFailureTests.cs
- ResourceExhaustionTests.cs
- ConcurrencyTests.cs
- DataCorruptionTests.cs
```

#### 테스트 인프라
```csharp
// tests/TestInfrastructure/
- TestContainers.cs (도커 컨테이너)
- MockLlmServer.cs (Mock LLM)
- TestDataFactory.cs (테스트 데이터)
- AssertionHelpers.cs (검증 헬퍼)
```

### 7. **플러그인 시스템 - 완전한 구현**

```csharp
// src/Agent.Plugins/
public class PluginSystem
{
    private readonly IPluginLoader _loader;
    private readonly IPluginRegistry _registry;
    private readonly IPluginSandbox _sandbox;
    private readonly IPluginValidator _validator;
    
    public async Task LoadPluginAsync(string path)
    {
        // 플러그인 검증
        var validation = await _validator.ValidateAsync(path);
        
        // 샌드박스 로딩
        var plugin = await _loader.LoadInSandboxAsync(path);
        
        // 등록
        await _registry.RegisterAsync(plugin);
    }
}

// 플러그인 인터페이스
public interface IAgentPlugin
{
    PluginManifest Manifest { get; }
    Task InitializeAsync(IPluginContext context);
    Task<IEnumerable<ITool>> GetToolsAsync();
    Task DisposeAsync();
}
```

#### 플러그인 격리
```csharp
// src/Agent.Plugins/Isolation/
- AssemblyLoadContextIsolation.cs
- ProcessIsolation.cs
- ContainerIsolation.cs
- PermissionManager.cs
```

### 8. **MCP 통합 - 완전한 구현**

```csharp
// src/Agent.MCP/
- McpClient.cs (MCP 클라이언트)
- McpServer.cs (MCP 서버)
- McpProtocol.cs (프로토콜 구현)
- McpToolAdapter.cs (도구 어댑터)

// MCP 프로토콜 지원
- JSON-RPC 2.0
- WebSocket 통신
- 도구 디스커버리
- 스키마 협상
- 에러 처리
```

### 9. **성능 최적화 - 시스템 전반**

```csharp
// src/Agent.Performance/

// 메모리 최적화
- ObjectPooling.cs
- MemoryRecycler.cs
- StringIntern.cs
- BufferManager.cs

// 응답 시간 최적화
- CacheManager.cs (다층 캐싱)
- PrecomputedResponses.cs
- LazyLoading.cs
- AsyncPipeline.cs

// 처리량 최적화
- BatchProcessor.cs
- ParallelPipeline.cs
- LoadBalancer.cs
- BackpressureManager.cs
```

### 10. **모니터링 및 관찰성 - 완전한 시스템**

```csharp
// src/Agent.Monitoring/

// 메트릭 수집
- MetricsCollector.cs
- CustomMetrics.cs
- MetricsAggregator.cs
- MetricsExporter.cs (Prometheus, Grafana)

// 로깅
- StructuredLogger.cs
- LogCorrelation.cs
- LogSampling.cs
- LogShipping.cs (ELK, Splunk)

// 분산 추적
- TraceProvider.cs
- SpanCollector.cs
- TraceCorrelation.cs
- TraceExporter.cs (Jaeger, Zipkin)

// 대시보드
- RealtimeDashboard.cs
- HistoricalAnalytics.cs
- AlertingSystem.cs
- AnomalyDetection.cs
```

## 구현 로드맵

### Phase 1: Foundation (1-2주)
1. JSON Schema 정의 완료
2. DefaultAgent 기본 구현
3. 인메모리 메모리 매니저

### Phase 2: Core Tools (1-2주)
4. FileSystem Tool
5. WebSearch Tool
6. Plan 실행 엔진 기본

### Phase 3: Integration (2-3주)
7. 통합 테스트 구축
8. Plan 실행 엔진 완성
9. 영구 메모리 저장소

### Phase 4: Extensions (2-3주)
10. 플러그인 시스템
11. MCP 통합
12. 추가 내부 도구들

### Phase 5: Production (2-3주)
13. 성능 최적화
14. 모니터링 시스템
15. 운영 도구

### Phase 6: Polish (1-2주)
16. 문서화 완성
17. SDK 개발
18. 샘플 및 튜토리얼

## 기술적 부채 해결

### 네이밍 일관성
```csharp
// 통일할 패턴
- Interfaces: I{Name}
- Abstracts: {Name}Base
- Implementations: Default{Name} or {Specific}{Name}
- Events: {Name}Event
- Exceptions: {Name}Exception
- Handlers: {Name}Handler
```

### 문서화 표준
```csharp
// XML 문서 주석 필수
/// <summary>
/// 기능 설명
/// </summary>
/// <param name="name">파라미터 설명</param>
/// <returns>반환값 설명</returns>
/// <exception cref="Type">예외 설명</exception>
/// <example>
/// 사용 예제
/// </example>
```

### 예외 처리 계층
```csharp
// src/Agent.Core/Exceptions/
- AgentException (최상위)
  - ConfigurationException
  - ExecutionException
  - ValidationException
  - ResourceException
  - SecurityException
```

이러한 구현을 통해 프로덕션 레벨의 완성도 높은 AI Agent Framework를 구축할 수 있습니다.