# AI Agent Framework - API Reference

## 개요

AI Agent Framework는 엔터프라이즈급 AI Agent 오케스트레이션을 위한 종합적인 플랫폼입니다. 이 문서는 주요 API 인터페이스와 사용법을 설명합니다.

## 핵심 구성 요소

### 1. 오케스트레이션 (Orchestration)

#### IOrchestrationEngine

AI Agent의 핵심 오케스트레이션 엔진입니다.

```csharp
public interface IOrchestrationEngine
{
    /// <summary>
    /// 사용자 요청을 실행합니다.
    /// </summary>
    Task<IOrchestrationResult> ExecuteAsync(IUserRequest request);
    
    /// <summary>
    /// 기존 컨텍스트를 계속 실행합니다.
    /// </summary>
    Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context);
}
```

**사용 예제:**
```csharp
// 의존성 주입으로 엔진 가져오기
var engine = serviceProvider.GetRequiredService<IOrchestrationEngine>();

// 사용자 요청 생성
var request = new UserRequest
{
    RequestId = Guid.NewGuid().ToString(),
    UserId = "user123",
    Content = "AI 모델을 사용해서 텍스트를 요약해주세요",
    Metadata = new Dictionary<string, object>(),
    RequestedAt = DateTime.UtcNow
};

// 실행
var result = await engine.ExecuteAsync(request);

if (result.IsSuccess)
{
    Console.WriteLine($"결과: {result.FinalResponse}");
}
```

#### IOrchestrationResult

오케스트레이션 실행 결과를 나타냅니다.

```csharp
public interface IOrchestrationResult
{
    string SessionId { get; }
    bool IsSuccess { get; }
    bool IsCompleted { get; }
    string? FinalResponse { get; }
    List<IExecutionStep> ExecutionSteps { get; }
    TimeSpan TotalDuration { get; }
    Dictionary<string, object> Metadata { get; }
    string? ErrorMessage { get; }
}
```

#### IUserRequest

사용자의 요청을 나타내는 인터페이스입니다.

```csharp
public interface IUserRequest
{
    string RequestId { get; }
    string UserId { get; }
    string Content { get; }
    Dictionary<string, object> Metadata { get; }
    DateTime RequestedAt { get; }
}
```

### 2. LLM 통합 (LLM Integration)

#### ILLMProvider

LLM과의 통신을 담당하는 인터페이스입니다.

```csharp
public interface ILLMProvider
{
    string Name { get; }
    string DefaultModel { get; }
    IReadOnlyList<string> SupportedModels { get; }
    
    Task<bool> IsAvailableAsync();
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
    Task<int> CountTokensAsync(string text, string? model = null);
}
```

**사용 예제:**
```csharp
// LLM Provider 가져오기
var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();

// 가용성 확인
if (await llmProvider.IsAvailableAsync())
{
    // 텍스트 생성
    var response = await llmProvider.GenerateAsync("안녕하세요!", CancellationToken.None);
    
    // 토큰 수 계산
    var tokenCount = await llmProvider.CountTokensAsync(response);
    
    Console.WriteLine($"응답: {response}");
    Console.WriteLine($"토큰 수: {tokenCount}");
}
```

#### ILLMFunctionRegistry

LLM 함수들을 등록하고 관리하는 레지스트리입니다.

```csharp
public interface ILLMFunctionRegistry
{
    void RegisterFunction<T>(string name, T function) where T : class, ILLMFunction;
    T? GetFunction<T>(string name) where T : class, ILLMFunction;
    IEnumerable<string> GetRegisteredFunctionNames();
    bool IsRegistered(string name);
    void UnregisterFunction(string name);
}
```

### 3. 상태 관리 (State Management)

#### IStateProvider

분산 상태 저장소와의 인터페이스입니다.

```csharp
public interface IStateProvider
{
    Task SetAsync<T>(string key, T value, TimeSpan? ttl, CancellationToken cancellationToken);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    
    Task<IStateTransaction> BeginTransactionAsync();
    Task<StateProviderStatistics> GetStatisticsAsync();
    Task<bool> IsHealthyAsync();
}
```

**사용 예제:**
```csharp
var stateProvider = serviceProvider.GetRequiredService<IStateProvider>();

// 데이터 저장 (5분 TTL)
await stateProvider.SetAsync("user_session", new { UserId = "123", LoginTime = DateTime.UtcNow }, 
                           TimeSpan.FromMinutes(5), CancellationToken.None);

// 데이터 조회
var session = await stateProvider.GetAsync<dynamic>("user_session", CancellationToken.None);

// 존재 여부 확인
if (await stateProvider.ExistsAsync("user_session", CancellationToken.None))
{
    Console.WriteLine("세션이 존재합니다.");
}

// 데이터 삭제
await stateProvider.DeleteAsync("user_session", CancellationToken.None);
```

#### 트랜잭션 지원

```csharp
// 트랜잭션 사용 예제
using var transaction = await stateProvider.BeginTransactionAsync();

await transaction.SetAsync("key1", "value1", TimeSpan.FromMinutes(10));
await transaction.SetAsync("key2", "value2", TimeSpan.FromMinutes(10));

if (allOperationsSuccessful)
{
    await transaction.CommitAsync();
}
else
{
    await transaction.RollbackAsync();
}
```

### 4. 도구 시스템 (Tools System)

#### IToolRegistry

도구들을 등록하고 관리하는 레지스트리입니다.

```csharp
public interface IToolRegistry
{
    void RegisterTool<T>(string name, T tool) where T : class, ITool;
    T? GetTool<T>(string name) where T : class, ITool;
    IEnumerable<string> GetRegisteredToolNames();
    bool IsRegistered(string name);
    void UnregisterTool(string name);
}
```

#### ITool

도구의 기본 인터페이스입니다.

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolContract Contract { get; }
    
    Task<IToolResult> ExecuteAsync(IToolInput input);
    Task<ValidationResult> ValidateAsync(IToolInput input);
}
```

**사용 예제:**
```csharp
// 커스텀 도구 구현
public class WeatherTool : ITool
{
    public string Name => "weather";
    public string Description => "날씨 정보를 제공합니다";
    public ToolContract Contract => new ToolContract
    {
        RequiredParameters = new[] { "location" },
        OptionalParameters = new[] { "date" }
    };
    
    public async Task<IToolResult> ExecuteAsync(IToolInput input)
    {
        var location = input.Parameters["location"].ToString();
        var weatherData = await GetWeatherAsync(location);
        return ToolResult.Success(weatherData);
    }
    
    public Task<ValidationResult> ValidateAsync(IToolInput input)
    {
        if (!input.Parameters.ContainsKey("location"))
            return Task.FromResult(ValidationResult.Failed("위치가 필요합니다"));
            
        return Task.FromResult(ValidationResult.Success());
    }
    
    private async Task<object> GetWeatherAsync(string location)
    {
        // 실제 날씨 API 호출
        await Task.Delay(100); // 시뮬레이션
        return new { Location = location, Temperature = "22°C", Condition = "맑음" };
    }
}

// 도구 등록
var toolRegistry = serviceProvider.GetRequiredService<IToolRegistry>();
toolRegistry.RegisterTool("weather", new WeatherTool());

// 도구 사용
var tool = toolRegistry.GetTool<WeatherTool>("weather");
var input = new ToolInput 
{ 
    Parameters = new Dictionary<string, object> { { "location", "서울" } } 
};

var result = await tool.ExecuteAsync(input);
if (result.IsSuccess)
{
    Console.WriteLine($"날씨 정보: {result.Data}");
}
```

### 5. 모니터링 (Monitoring)

#### 텔레메트리 수집

```csharp
// 서비스 등록
services.AddAIAgentMonitoring();

// 텔레메트리 수집기 사용
var telemetryCollector = serviceProvider.GetService<TelemetryCollector>();
var metricsCollector = serviceProvider.GetService<MetricsCollector>();
```

#### 헬스 체크

```csharp
// 헬스 체크 추가 (확장 메서드가 구현되어 있다면)
services.AddAllHealthChecks(options =>
{
    options.EnableBackgroundChecks = true;
    options.BackgroundCheckIntervalSeconds = 60;
});

// 헬스 체크 실행
var healthCheckService = serviceProvider.GetService<IHealthCheckService>();
if (healthCheckService != null)
{
    var result = await healthCheckService.RunAllHealthChecksAsync();
    Console.WriteLine($"전체 상태: {result.OverallStatus}");
}
```

## 의존성 주입 설정

### 기본 설정

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 로깅
    services.AddLogging(builder => 
        builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    
    // 핵심 서비스
    services.AddSingleton<IOrchestrationEngine, TypeSafeOrchestrationEngine>();
    services.AddSingleton<ILLMFunctionRegistry, TypedLLMFunctionRegistry>();
    services.AddSingleton<IToolRegistry, TypedToolRegistry>();
    
    // LLM Provider (예: Claude)
    services.AddSingleton<ILLMProvider, ClaudeProvider>();
    services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));
    
    // 상태 관리 (InMemory 또는 Redis)
    services.AddSingleton<IStateProvider, InMemoryStateProvider>();
    // 또는 Redis 사용 시:
    // services.AddSingleton<IStateProvider, RedisStateProvider>();
    
    // 모니터링
    services.AddAIAgentMonitoring();
}
```

### 설정 파일 (appsettings.json)

```json
{
  "Claude": {
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://api.anthropic.com",
    "ApiVersion": "2023-06-01",
    "DefaultModel": "claude-3-sonnet-20240229",
    "MaxTokens": 4096,
    "Temperature": 0.7
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "KeyPrefix": "aiagent:"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AIAgentFramework": "Debug"
    }
  }
}
```

## 오류 처리

### 예외 타입

- **AIAgentException**: 기본 프레임워크 예외
- **OrchestrationException**: 오케스트레이션 관련 예외
- **LLMException**: LLM Provider 관련 예외
- **ToolException**: 도구 실행 관련 예외
- **StateException**: 상태 관리 관련 예외

### 오류 처리 예제

```csharp
try
{
    var result = await orchestrationEngine.ExecuteAsync(request);
    
    if (!result.IsSuccess)
    {
        Console.WriteLine($"실행 실패: {result.ErrorMessage}");
        
        // 실행 단계별 오류 확인
        foreach (var step in result.ExecutionSteps.Where(s => !s.IsSuccess))
        {
            Console.WriteLine($"단계 '{step.FunctionName}' 실패: {step.ErrorMessage}");
        }
    }
}
catch (OrchestrationException ex)
{
    Console.WriteLine($"오케스트레이션 오류: {ex.Message}");
}
catch (LLMException ex)
{
    Console.WriteLine($"LLM 오류: {ex.Message}");
}
catch (StateException ex)
{
    Console.WriteLine($"상태 관리 오류: {ex.Message}");
}
```

## 성능 고려사항

### 동시성

- 모든 인터페이스는 thread-safe 하게 설계되었습니다
- `IStateProvider`는 동시 액세스를 안전하게 처리합니다
- LLM Provider는 동시 요청을 효율적으로 관리합니다

### 메모리 관리

- `IDisposable` 패턴을 따르는 컴포넌트는 적절히 해제하세요
- 장기 실행 서비스에서는 메모리 누수를 모니터링하세요
- 상태 데이터에 적절한 TTL을 설정하세요

### 성능 최적화

- 배치 연산을 활용하여 네트워크 호출을 최소화하세요
- 캐싱을 적극 활용하세요
- 적절한 타임아웃 설정으로 리소스 누수를 방지하세요

## 확장성

### 커스텀 LLM Provider 구현

```csharp
public class CustomLLMProvider : ILLMProvider
{
    public string Name => "Custom LLM";
    public string DefaultModel => "custom-model-v1";
    public IReadOnlyList<string> SupportedModels => new[] { "custom-model-v1", "custom-model-v2" };
    
    public async Task<bool> IsAvailableAsync()
    {
        // 가용성 체크 로직
        return await CheckServiceAvailabilityAsync();
    }
    
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        // 커스텀 LLM API 호출
        return await CallCustomLLMAsync(prompt, cancellationToken);
    }
    
    public async Task<int> CountTokensAsync(string text, string? model = null)
    {
        // 토큰 카운팅 로직
        return await CountTokensInternalAsync(text, model ?? DefaultModel);
    }
    
    private async Task<bool> CheckServiceAvailabilityAsync()
    {
        // 구현
        await Task.Delay(10);
        return true;
    }
    
    private async Task<string> CallCustomLLMAsync(string prompt, CancellationToken cancellationToken)
    {
        // 구현
        await Task.Delay(100, cancellationToken);
        return $"Custom response to: {prompt}";
    }
    
    private async Task<int> CountTokensInternalAsync(string text, string model)
    {
        // 구현
        await Task.Delay(10);
        return text.Length / 4; // 간단한 추정
    }
}

// 등록
services.AddSingleton<ILLMProvider, CustomLLMProvider>();
```

### 커스텀 상태 제공자 구현

```csharp
public class DatabaseStateProvider : IStateProvider
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    
    public DatabaseStateProvider(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var json = JsonSerializer.Serialize(value);
        var expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;
        
        var entity = await context.StateEntries.FindAsync(key, cancellationToken);
        if (entity != null)
        {
            entity.Data = json;
            entity.ExpiresAt = expiresAt;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            context.StateEntries.Add(new StateEntity
            {
                Key = key,
                Data = json,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entity = await context.StateEntries
            .Where(e => e.Key == key && (e.ExpiresAt == null || e.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(cancellationToken);
            
        if (entity == null) return default;
        
        return JsonSerializer.Deserialize<T>(entity.Data);
    }
    
    // 기타 메서드들...
}
```

## 버전 정보

현재 버전: **1.0.0**

## 지원 및 문의

- GitHub Issues: [프로젝트 저장소](https://github.com/your-org/ai-agent-framework)
- 문서: [공식 문서 사이트](https://docs.your-org.com/ai-agent-framework)
- 샘플 코드: `samples/` 디렉터리 참조