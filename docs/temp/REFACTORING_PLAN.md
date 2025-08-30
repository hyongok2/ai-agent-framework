# AI Agent Framework - 전면 리팩토링 계획

## 🎯 목표
프로토타입 수준의 현재 코드를 **프로덕션 레디** 상태로 전환

## 📊 현재 상태 평가
- **전체 완성도**: 30% (프로토타입)
- **Critical Issues**: 5개
- **High Issues**: 6개
- **예상 소요 시간**: 6주 (풀타임 기준)

## 🗓️ 6주 리팩토링 로드맵

### 📌 Phase 1: Critical Core Issues (Week 1)
**목표**: 하드코딩 제거 및 타입 안전성 확보

#### Day 1-2: 오케스트레이션 엔진 재설계
```csharp
// BEFORE (현재 문제)
private static string GetActionType(object action) {
    return action.ToString()?.Split('_')[0] ?? "unknown";
}

// AFTER (개선안)
public interface IOrchestrationAction {
    ActionType Type { get; }
    string Name { get; }
    Task<ActionResult> ExecuteAsync(IActionContext context);
}

public class TypedOrchestrationEngine {
    private readonly IActionFactory _actionFactory;
    private readonly IActionExecutor _actionExecutor;
    
    public async Task<IOrchestrationResult> ExecuteAsync(
        IOrchestrationPlan plan,
        CancellationToken cancellationToken) {
        // 타입 안전한 실행
        foreach (var action in plan.Actions) {
            var result = await _actionExecutor.ExecuteAsync(action, cancellationToken);
            // ...
        }
    }
}
```

#### Day 3-4: Registry 패턴 개선
```csharp
// 문자열 기반 Registry 제거
public interface ITypedRegistry {
    void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TImplementation : class, TInterface;
    
    T Resolve<T>() where T : class;
    IEnumerable<T> ResolveAll<T>() where T : class;
}

// 컴파일 타임 안전성
public class ServiceRegistry : ITypedRegistry {
    private readonly IServiceCollection _services;
    
    public void Register<ILLMFunction, PlannerFunction>() {
        _services.AddSingleton<ILLMFunction, PlannerFunction>();
    }
}
```

#### Day 5: Configuration 시스템 개선
```csharp
public class ConfigurationManager {
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<AIAgentOptions> _options;
    
    public async Task InvalidateCacheAsync(string key) {
        // 실제 캐시 무효화 구현
        _cache.Remove(key);
        await _options.OnChangeAsync();
    }
}
```

### 📌 Phase 2: State Management System (Week 2)
**목표**: 분산 환경 지원 상태 관리

#### Day 1-2: State Provider 추상화
```csharp
public interface IStateProvider {
    Task<T> GetStateAsync<T>(string sessionId) where T : class;
    Task SetStateAsync<T>(string sessionId, T state, TimeSpan? expiry = null);
    Task<bool> ExistsAsync(string sessionId);
    Task DeleteAsync(string sessionId);
}

// Redis 구현
public class RedisStateProvider : IStateProvider {
    private readonly IConnectionMultiplexer _redis;
    
    public async Task<T> GetStateAsync<T>(string sessionId) {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync($"session:{sessionId}");
        return json.HasValue ? JsonSerializer.Deserialize<T>(json) : null;
    }
}

// In-Memory 구현 (개발/테스트용)
public class InMemoryStateProvider : IStateProvider {
    private readonly IMemoryCache _cache;
    // ...
}
```

#### Day 3-4: Orchestration Context 지속성
```csharp
public class StatefulOrchestrationEngine : IOrchestrationEngine {
    private readonly IStateProvider _stateProvider;
    private readonly IOrchestrationStrategy _strategy;
    
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request) {
        // 상태 복원 또는 생성
        var context = await _stateProvider.GetStateAsync<OrchestrationContext>(request.SessionId)
            ?? new OrchestrationContext(request);
        
        try {
            var result = await _strategy.ExecuteAsync(context);
            
            // 상태 저장
            await _stateProvider.SetStateAsync(
                request.SessionId, 
                context,
                TimeSpan.FromHours(1));
                
            return result;
        }
        catch (Exception ex) {
            // 실패 시에도 상태 저장 (복구용)
            context.LastError = ex;
            await _stateProvider.SetStateAsync(request.SessionId, context);
            throw;
        }
    }
}
```

#### Day 5: Checkpoint & Recovery
```csharp
public interface ICheckpointManager {
    Task CreateCheckpointAsync(string sessionId, IOrchestrationContext context);
    Task<IOrchestrationContext> RestoreCheckpointAsync(string sessionId);
    Task<IEnumerable<Checkpoint>> GetCheckpointsAsync(string sessionId);
}

public class CheckpointManager : ICheckpointManager {
    private readonly IStateProvider _stateProvider;
    
    public async Task CreateCheckpointAsync(string sessionId, IOrchestrationContext context) {
        var checkpoint = new Checkpoint {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Context = context,
            ExecutionStep = context.ExecutionHistory.Count
        };
        
        await _stateProvider.SetStateAsync(
            $"checkpoint:{sessionId}:{checkpoint.Id}",
            checkpoint);
    }
}
```

### 📌 Phase 3: Complete LLM Integration (Week 3)
**목표**: 실제 사용 가능한 LLM Provider 구현

#### Day 1-2: Token Management
```csharp
public interface ITokenCounter {
    int CountTokens(string text, string model);
    TokenUsage EstimateUsage(LLMRequest request);
}

public class TiktokenCounter : ITokenCounter {
    private readonly Dictionary<string, Encoding> _encodings = new();
    
    public int CountTokens(string text, string model) {
        var encoding = GetEncoding(model);
        return encoding.Encode(text).Count;
    }
}

public class LLMProviderBase {
    protected readonly ITokenCounter _tokenCounter;
    protected readonly ITokenBudgetManager _budgetManager;
    
    public async Task<LLMResponse> GenerateAsync(LLMRequest request) {
        // 토큰 예산 확인
        var estimated = _tokenCounter.EstimateUsage(request);
        if (!await _budgetManager.CanUseTokensAsync(estimated)) {
            throw new TokenBudgetExceededException();
        }
        
        var response = await GenerateInternalAsync(request);
        
        // 실제 사용량 기록
        await _budgetManager.RecordUsageAsync(response.TokensUsed);
        
        return response;
    }
}
```

#### Day 3-4: Streaming Support
```csharp
public interface IStreamingLLMProvider : ILLMProvider {
    IAsyncEnumerable<LLMStreamChunk> GenerateStreamAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default);
}

public class ClaudeProvider : IStreamingLLMProvider {
    public async IAsyncEnumerable<LLMStreamChunk> GenerateStreamAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        
        using var stream = await GetStreamAsync(request, cancellationToken);
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested) {
            var line = await reader.ReadLineAsync();
            if (line?.StartsWith("data: ") == true) {
                var chunk = ParseSSEChunk(line);
                yield return chunk;
            }
        }
    }
}
```

#### Day 5: Provider Factory & Fallback
```csharp
public class ResilientLLMProvider : ILLMProvider {
    private readonly IList<ILLMProvider> _providers;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public async Task<LLMResponse> GenerateAsync(LLMRequest request) {
        Exception lastException = null;
        
        foreach (var provider in _providers) {
            try {
                if (_circuitBreaker.IsOpen(provider.Name)) {
                    continue; // Skip unavailable provider
                }
                
                return await provider.GenerateAsync(request);
            }
            catch (Exception ex) {
                lastException = ex;
                _circuitBreaker.RecordFailure(provider.Name);
                _logger.LogWarning(ex, "Provider {Provider} failed", provider.Name);
            }
        }
        
        throw new AllProvidersFailedException(lastException);
    }
}
```

### 📌 Phase 4: Transaction Support (Week 4)
**목표**: 분산 트랜잭션 및 보상 패턴

#### Day 1-2: Saga Pattern Implementation
```csharp
public interface ISaga {
    string Id { get; }
    IReadOnlyList<ISagaStep> Steps { get; }
    Task<SagaResult> ExecuteAsync(CancellationToken cancellationToken);
}

public interface ISagaStep {
    string Name { get; }
    Task<StepResult> ExecuteAsync(SagaContext context);
    Task<CompensationResult> CompensateAsync(SagaContext context);
}

public class OrchestrationSaga : ISaga {
    private readonly IList<ISagaStep> _steps;
    private readonly ISagaCoordinator _coordinator;
    
    public async Task<SagaResult> ExecuteAsync(CancellationToken cancellationToken) {
        var executedSteps = new Stack<ISagaStep>();
        
        try {
            foreach (var step in _steps) {
                var result = await step.ExecuteAsync(_context);
                executedSteps.Push(step);
                
                if (!result.Success) {
                    throw new SagaExecutionException(step, result);
                }
            }
            
            return SagaResult.Success();
        }
        catch (Exception ex) {
            // 보상 트랜잭션 실행
            while (executedSteps.Count > 0) {
                var step = executedSteps.Pop();
                try {
                    await step.CompensateAsync(_context);
                }
                catch (Exception compensationEx) {
                    _logger.LogError(compensationEx, 
                        "Compensation failed for step {Step}", step.Name);
                }
            }
            
            return SagaResult.Failed(ex);
        }
    }
}
```

#### Day 3-4: Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable {
    void RegisterNew(IEntity entity);
    void RegisterModified(IEntity entity);
    void RegisterDeleted(IEntity entity);
    Task<int> CommitAsync();
    Task RollbackAsync();
}

public class OrchestrationUnitOfWork : IUnitOfWork {
    private readonly List<IEntity> _newEntities = new();
    private readonly List<IEntity> _modifiedEntities = new();
    private readonly List<IEntity> _deletedEntities = new();
    private readonly IStateProvider _stateProvider;
    
    public async Task<int> CommitAsync() {
        using var transaction = await _stateProvider.BeginTransactionAsync();
        
        try {
            foreach (var entity in _newEntities) {
                await _stateProvider.CreateAsync(entity);
            }
            
            foreach (var entity in _modifiedEntities) {
                await _stateProvider.UpdateAsync(entity);
            }
            
            foreach (var entity in _deletedEntities) {
                await _stateProvider.DeleteAsync(entity);
            }
            
            await transaction.CommitAsync();
            return _newEntities.Count + _modifiedEntities.Count + _deletedEntities.Count;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

#### Day 5: Idempotency Support
```csharp
public interface IIdempotencyManager {
    Task<bool> IsProcessedAsync(string idempotencyKey);
    Task<T> GetResultAsync<T>(string idempotencyKey);
    Task RecordResultAsync<T>(string idempotencyKey, T result, TimeSpan expiry);
}

public class IdempotentOrchestrationEngine : IOrchestrationEngine {
    private readonly IIdempotencyManager _idempotency;
    private readonly IOrchestrationEngine _inner;
    
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request) {
        var idempotencyKey = request.IdempotencyKey ?? GenerateKey(request);
        
        // 이미 처리된 요청인지 확인
        if (await _idempotency.IsProcessedAsync(idempotencyKey)) {
            return await _idempotency.GetResultAsync<IOrchestrationResult>(idempotencyKey);
        }
        
        var result = await _inner.ExecuteAsync(request);
        
        // 결과 저장 (재시도 시 동일 결과 반환)
        await _idempotency.RecordResultAsync(
            idempotencyKey, 
            result, 
            TimeSpan.FromHours(24));
            
        return result;
    }
}
```

### 📌 Phase 5: Monitoring & Observability (Week 5)
**목표**: 완전한 관찰성 구현

#### Day 1-2: OpenTelemetry Integration
```csharp
public class TelemetryOrchestrationEngine : IOrchestrationEngine {
    private readonly IOrchestrationEngine _inner;
    private readonly ActivitySource _activitySource;
    private readonly IMeterProvider _meterProvider;
    
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request) {
        using var activity = _activitySource.StartActivity(
            "Orchestration.Execute",
            ActivityKind.Internal);
            
        activity?.SetTag("user.id", request.UserId);
        activity?.SetTag("session.id", request.SessionId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try {
            var result = await _inner.ExecuteAsync(request);
            
            _meterProvider.Counter("orchestration.success").Add(1);
            _meterProvider.Histogram("orchestration.duration")
                .Record(stopwatch.ElapsedMilliseconds);
                
            return result;
        }
        catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meterProvider.Counter("orchestration.failure").Add(1);
            throw;
        }
    }
}
```

#### Day 3-4: Custom Metrics & Health Checks
```csharp
public interface IMetricsCollector {
    void RecordLatency(string operation, double milliseconds);
    void IncrementCounter(string metric, Dictionary<string, string> tags = null);
    void RecordGauge(string metric, double value);
}

public class PrometheusMetricsCollector : IMetricsCollector {
    private readonly CollectorRegistry _registry;
    
    public void RecordLatency(string operation, double milliseconds) {
        Metrics.CreateHistogram(
            $"operation_{operation}_duration_ms",
            "Operation duration in milliseconds",
            new HistogramConfiguration {
                Buckets = Histogram.ExponentialBuckets(1, 2, 10)
            }).Observe(milliseconds);
    }
}

public class OrchestrationHealthCheck : IHealthCheck {
    private readonly IOrchestrationEngine _engine;
    private readonly IStateProvider _stateProvider;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        
        var data = new Dictionary<string, object>();
        
        try {
            // 상태 저장소 연결 확인
            var stateHealthy = await _stateProvider.ExistsAsync("health-check");
            data["state_provider"] = stateHealthy ? "healthy" : "unhealthy";
            
            // LLM Provider 가용성 확인
            var llmHealthy = await CheckLLMProvidersAsync();
            data["llm_providers"] = llmHealthy;
            
            // 최근 실행 메트릭
            data["recent_executions"] = await GetRecentMetricsAsync();
            
            return HealthCheckResult.Healthy("All systems operational", data);
        }
        catch (Exception ex) {
            return HealthCheckResult.Unhealthy("Health check failed", ex, data);
        }
    }
}
```

#### Day 5: Distributed Tracing
```csharp
public class TracingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ITracer _tracer;
    
    public async Task InvokeAsync(HttpContext context) {
        using var scope = _tracer.BuildSpan("http.request")
            .WithTag("http.method", context.Request.Method)
            .WithTag("http.url", context.Request.Path)
            .StartActive();
            
        try {
            await _next(context);
            scope.Span.SetTag("http.status_code", context.Response.StatusCode);
        }
        catch (Exception ex) {
            scope.Span.SetTag("error", true);
            scope.Span.Log(new Dictionary<string, object> {
                ["event"] = "error",
                ["error.kind"] = ex.GetType().Name,
                ["message"] = ex.Message,
                ["stack"] = ex.StackTrace
            });
            throw;
        }
    }
}
```

### 📌 Phase 6: Testing & Documentation (Week 6)
**목표**: 80% 테스트 커버리지 달성

#### Day 1-2: Integration Testing
```csharp
public class OrchestrationIntegrationTests {
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    
    [Fact]
    public async Task CompleteOrchestrationFlow_WithRealDependencies() {
        // Arrange
        var services = new ServiceCollection();
        services.AddAIAgentFramework()
            .AddRedisStateProvider("localhost:6379")
            .AddOpenTelemetry()
            .AddHealthChecks();
            
        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IOrchestrationEngine>();
        
        // Act
        var request = new UserRequest {
            Content = "Analyze customer sentiment",
            UserId = "test-user",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        
        var result = await engine.ExecuteAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Context.ExecutionHistory);
        
        // Verify state persistence
        var stateProvider = provider.GetRequiredService<IStateProvider>();
        var savedContext = await stateProvider.GetStateAsync<OrchestrationContext>(
            result.Context.SessionId);
        Assert.NotNull(savedContext);
    }
}
```

#### Day 3-4: Load Testing
```csharp
public class LoadTests {
    [Fact]
    public async Task OrchestrationEngine_HandlesHighLoad() {
        // 100 concurrent requests
        var tasks = Enumerable.Range(0, 100)
            .Select(i => ExecuteOrchestrationAsync($"request-{i}"))
            .ToArray();
            
        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assertions
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.True(stopwatch.ElapsedMilliseconds < 30000); // 30초 이내
        
        // 95th percentile latency
        var latencies = results.Select(r => r.ExecutionTime.TotalMilliseconds)
            .OrderBy(l => l).ToList();
        var p95 = latencies[(int)(latencies.Count * 0.95)];
        Assert.True(p95 < 5000); // 95% 요청이 5초 이내
    }
}
```

#### Day 5: Documentation Generation
```csharp
/// <summary>
/// API 문서 자동 생성
/// </summary>
public class DocumentationGenerator {
    public void GenerateApiDocs() {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => {
            options.SwaggerDoc("v1", new OpenApiInfo {
                Title = "AI Agent Framework API",
                Version = "v1",
                Description = "Production-ready AI orchestration platform"
            });
            
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });
    }
}
```

## 📈 성공 지표

### Week 1 완료 시점
- ✅ 모든 하드코딩 제거
- ✅ 타입 안전성 100%
- ✅ 컴파일 타임 검증 가능

### Week 2 완료 시점
- ✅ 상태 지속성 구현
- ✅ 분산 환경 지원
- ✅ 장애 복구 가능

### Week 3 완료 시점
- ✅ 실제 LLM API 통합
- ✅ 토큰 관리 시스템
- ✅ 스트리밍 지원

### Week 4 완료 시점
- ✅ 트랜잭션 지원
- ✅ 보상 패턴 구현
- ✅ 멱등성 보장

### Week 5 완료 시점
- ✅ 완전한 관찰성
- ✅ 분산 추적
- ✅ 실시간 메트릭

### Week 6 완료 시점
- ✅ 테스트 커버리지 80%
- ✅ 부하 테스트 통과
- ✅ 완전한 API 문서

## 🎯 최종 목표

```
구조 설계: ██████████ 100%
핵심 구현: ██████████ 100%
프로덕션: █████████░ 90%
전체 평가: █████████░ 95%
```

**프로덕션 레디 체크리스트**:
- [ ] 모든 Critical 이슈 해결
- [ ] 상태 관리 시스템 구현
- [ ] 실제 LLM 통합 완료
- [ ] 트랜잭션 지원
- [ ] 모니터링 구현
- [ ] 80% 테스트 커버리지
- [ ] 성능 벤치마크 통과
- [ ] 보안 감사 통과
- [ ] 문서화 완료
- [ ] CI/CD 파이프라인 구축

## 💡 핵심 원칙

1. **타입 안전성 우선** - 모든 문자열 기반 로직 제거
2. **테스트 가능한 설계** - 모든 컴포넌트 모킹 가능
3. **관찰 가능성** - 모든 작업 추적 가능
4. **복원력** - 모든 실패 복구 가능
5. **확장성** - 수평 확장 가능

이 계획을 따르면 6주 후에는 **실제 프로덕션 환경에서 사용 가능한** AI Agent Framework가 완성됩니다.