# AI Agent Framework - Quick Start Guide

## 빠른 시작

이 가이드는 AI Agent Framework를 빠르게 시작할 수 있도록 도와드립니다.

## 1. 설치

### NuGet 패키지 설치

```bash
# 핵심 패키지들
dotnet add package AIAgentFramework.Core
dotnet add package AIAgentFramework.LLM
dotnet add package AIAgentFramework.State
dotnet add package AIAgentFramework.Monitoring

# 선택적 패키지들
dotnet add package AIAgentFramework.Tools      # 내장 도구들
dotnet add package AIAgentFramework.Registry   # 타입 안전 레지스트리
```

### 의존성

```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
```

## 2. 기본 설정

### Program.cs (콘솔 애플리케이션)

```csharp

using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.Monitoring.Extensions;
using AIAgentFramework.State.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// 로깅 설정
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// AI Agent Framework 서비스 등록
ConfigureAIAgentServices(builder.Services, builder.Configuration);

var host = builder.Build();

// 간단한 테스트 실행
await RunSimpleTestAsync(host.Services);

static void ConfigureAIAgentServices(IServiceCollection services, IConfiguration configuration)
{
    // 핵심 서비스들
    services.AddSingleton<IOrchestrationEngine, TypeSafeOrchestrationEngine>();
    services.AddSingleton<ILLMFunctionRegistry, TypedLLMFunctionRegistry>();
    services.AddSingleton<IToolRegistry, TypedToolRegistry>();
    
    // Mock LLM Provider (테스트용)
    services.AddSingleton<ILLMProvider, MockLLMProvider>();
    
    // 상태 관리 (메모리 기반)
    services.AddSingleton<IStateProvider, InMemoryStateProvider>();
    
    // 모니터링
    services.AddAIAgentMonitoring();
}

static async Task RunSimpleTestAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // LLM Provider 테스트
        var llmProvider = services.GetRequiredService<ILLMProvider>();
        logger.LogInformation("LLM Provider: {Name}", llmProvider.Name);
        
        if (await llmProvider.IsAvailableAsync())
        {
            var response = await llmProvider.GenerateAsync("안녕하세요! AI Agent Framework 테스트입니다.", CancellationToken.None);
            logger.LogInformation("LLM 응답: {Response}", response);
        }
        
        // 상태 관리 테스트
        var stateProvider = services.GetRequiredService<IStateProvider>();
        await stateProvider.SetAsync("test_key", "Hello, AI Agent!", TimeSpan.FromMinutes(5), CancellationToken.None);
        
        var storedValue = await stateProvider.GetAsync<string>("test_key", CancellationToken.None);
        logger.LogInformation("저장된 값: {Value}", storedValue);
        
        logger.LogInformation("✅ AI Agent Framework 초기 설정 완료!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "테스트 실행 중 오류 발생");
    }
}
```

### Mock LLM Provider (개발/테스트용)

```csharp
public class MockLLMProvider : ILLMProvider
{
    public string Name => "Mock LLM";
    public string DefaultModel => "mock-model";
    public IReadOnlyList<string> SupportedModels => new[] { "mock-model" };
    
    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
    
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // 네트워크 지연 시뮬레이션
        return $"Mock AI 응답: '{prompt}'에 대한 처리를 완료했습니다.";
    }
    
    public Task<int> CountTokensAsync(string text, string? model = null)
    {
        return Task.FromResult(text.Length / 4); // 대략적인 토큰 수 추정
    }
}
```

## 3. 실제 LLM Provider 설정

### Claude API 사용

```csharp
// appsettings.json
{
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "BaseUrl": "https://api.anthropic.com",
    "ApiVersion": "2023-06-01",
    "DefaultModel": "claude-3-sonnet-20240229",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 30
  }
}

// Program.cs에서
services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));
services.AddSingleton<ILLMProvider, ClaudeProvider>();
```

### OpenAI API 사용

```csharp
// appsettings.json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "BaseUrl": "https://api.openai.com",
    "DefaultModel": "gpt-4",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 30
  }
}

// Program.cs에서
services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
services.AddSingleton<ILLMProvider, OpenAIProvider>();
```

## 4. Redis 상태 관리 설정

### Redis 설정

```csharp
// appsettings.json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "KeyPrefix": "aiagent:",
    "DefaultTtlMinutes": 60
  }
}

// Program.cs에서
services.Configure<RedisOptions>(configuration.GetSection("Redis"));
services.AddSingleton<IStateProvider, RedisStateProvider>();
```

### Docker로 Redis 실행

```bash
docker run -d --name redis-aiagent -p 6379:6379 redis:7-alpine
```

## 5. 첫 번째 AI Agent 만들기

```csharp
public class SimpleAIAgent
{
    private readonly IOrchestrationEngine _engine;
    private readonly ILogger<SimpleAIAgent> _logger;
    
    public SimpleAIAgent(IOrchestrationEngine engine, ILogger<SimpleAIAgent> logger)
    {
        _engine = engine;
        _logger = logger;
    }
    
    public async Task<string> ProcessRequestAsync(string userInput)
    {
        var request = new UserRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            UserId = "default-user",
            Content = userInput,
            Metadata = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["source"] = "simple-agent"
            },
            RequestedAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("처리 요청: {Content}", request.Content);
        
        var result = await _engine.ExecuteAsync(request);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("처리 완료: {Duration}ms", result.TotalDuration.TotalMilliseconds);
            return result.FinalResponse ?? "처리 완료";
        }
        else
        {
            _logger.LogError("처리 실패: {Error}", result.ErrorMessage);
            return $"오류 발생: {result.ErrorMessage}";
        }
    }
}

// 사용 예제
var agent = services.GetRequiredService<SimpleAIAgent>();

var responses = new[]
{
    await agent.ProcessRequestAsync("오늘 날씨는 어때요?"),
    await agent.ProcessRequestAsync("Python으로 간단한 계산기 만드는 방법 알려주세요"),
    await agent.ProcessRequestAsync("AI와 머신러닝의 차이점이 뭔가요?")
};

foreach (var response in responses)
{
    Console.WriteLine($"응답: {response}\n");
}
```

## 6. 커스텀 도구 추가

```csharp
public class CalculatorTool : ITool
{
    public string Name => "calculator";
    public string Description => "기본적인 수학 계산을 수행합니다";
    public ToolContract Contract => new ToolContract
    {
        RequiredParameters = new[] { "expression" },
        OptionalParameters = Array.Empty<string>()
    };
    
    public Task<IToolResult> ExecuteAsync(IToolInput input)
    {
        try
        {
            var expression = input.Parameters["expression"].ToString();
            var result = EvaluateExpression(expression);
            
            return Task.FromResult(ToolResult.Success(new { expression, result }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failed($"계산 오류: {ex.Message}"));
        }
    }
    
    public Task<ValidationResult> ValidateAsync(IToolInput input)
    {
        if (!input.Parameters.ContainsKey("expression"))
            return Task.FromResult(ValidationResult.Failed("expression 파라미터가 필요합니다"));
            
        return Task.FromResult(ValidationResult.Success());
    }
    
    private double EvaluateExpression(string expression)
    {
        // 간단한 계산 로직 (실제로는 더 안전한 파서 사용 권장)
        var dataTable = new System.Data.DataTable();
        return Convert.ToDouble(dataTable.Compute(expression, null));
    }
}

// 도구 등록
var toolRegistry = services.GetRequiredService<IToolRegistry>();
toolRegistry.RegisterTool("calculator", new CalculatorTool());
```

## 7. 모니터링 설정

### 기본 모니터링

```csharp
// 서비스 등록에서
services.AddAIAgentMonitoring();

// 사용
var telemetryCollector = services.GetService<TelemetryCollector>();
var metricsCollector = services.GetService<MetricsCollector>();

// 커스텀 메트릭 기록
if (metricsCollector != null)
{
    metricsCollector.RecordCounter("custom.requests", 1, new[] { ("operation", "test") });
}
```

### ASP.NET Core 통합

```csharp
var builder = WebApplication.CreateBuilder(args);

// AI Agent Framework 서비스 추가
ConfigureAIAgentServices(builder.Services, builder.Configuration);

// 헬스 체크 (구현되어 있다면)
builder.Services.AddHealthChecks();

var app = builder.Build();

// 헬스 체크 엔드포인트
app.MapHealthChecks("/health");

// AI Agent API 엔드포인트
app.MapPost("/api/agent/process", async (ProcessRequest request, SimpleAIAgent agent) =>
{
    var response = await agent.ProcessRequestAsync(request.Input);
    return Results.Ok(new { response });
});

public record ProcessRequest(string Input);
```

## 8. 고급 설정

### 배치 연산 사용

```csharp
// 배치 상태 연산 (지원하는 Provider의 경우)
if (stateProvider is IBatchStateProvider batchProvider)
{
    var keys = Enumerable.Range(0, 10).Select(i => $"batch_key_{i}").ToArray();
    var values = keys.Select(k => new { key = k, timestamp = DateTime.UtcNow }).ToArray();
    
    await batchProvider.SetBatchAsync(keys.Zip(values).ToDictionary(x => x.First, x => (object)x.Second), 
                                     TimeSpan.FromMinutes(10), CancellationToken.None);
                                     
    var results = await batchProvider.GetBatchAsync<object>(keys, CancellationToken.None);
    
    Console.WriteLine($"배치 조회 결과: {results.Count}개");
}
```

### 트랜잭션 사용

```csharp
using var transaction = await stateProvider.BeginTransactionAsync();

try
{
    await transaction.SetAsync("user:123:balance", 1000, TimeSpan.FromDays(1));
    await transaction.SetAsync("user:456:balance", 2000, TimeSpan.FromDays(1));
    
    // 모든 연산이 성공하면 커밋
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## 9. 테스팅

### 단위 테스트 예제

```csharp
[Test]
public async Task SimpleAgent_Should_Process_Request_Successfully()
{
    // Arrange
    var services = new ServiceCollection();
    ConfigureTestServices(services);
    var provider = services.BuildServiceProvider();
    
    var agent = new SimpleAIAgent(
        provider.GetRequiredService<IOrchestrationEngine>(),
        provider.GetRequiredService<ILogger<SimpleAIAgent>>());
    
    // Act
    var response = await agent.ProcessRequestAsync("테스트 요청");
    
    // Assert
    response.Should().NotBeNullOrEmpty();
    response.Should().Contain("Mock AI 응답");
}

private void ConfigureTestServices(IServiceCollection services)
{
    services.AddLogging();
    services.AddSingleton<ILLMProvider, MockLLMProvider>();
    services.AddSingleton<IStateProvider, InMemoryStateProvider>();
    services.AddSingleton<IOrchestrationEngine, TypeSafeOrchestrationEngine>();
    services.AddSingleton<ILLMFunctionRegistry, TypedLLMFunctionRegistry>();
    services.AddSingleton<IToolRegistry, TypedToolRegistry>();
}
```

## 10. 프로덕션 배포

### Docker 설정

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyAIAgent.csproj", "."]
RUN dotnet restore "MyAIAgent.csproj"
COPY . .
RUN dotnet build "MyAIAgent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyAIAgent.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyAIAgent.dll"]
```

### 환경별 설정

```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "AIAgentFramework": "Information"
    }
  },
  "Claude": {
    "ApiKey": "${CLAUDE_API_KEY}",
    "TimeoutSeconds": 60
  },
  "Redis": {
    "ConnectionString": "${REDIS_CONNECTION_STRING}",
    "DefaultTtlMinutes": 120
  }
}
```

## 다음 단계

1. [API Reference](API-Reference.md)에서 자세한 인터페이스 정보 확인
2. `samples/` 디렉터리의 예제 프로젝트들 살펴보기
3. 실제 LLM Provider 설정 및 테스트
4. 커스텀 도구 및 함수 개발
5. 프로덕션 환경 모니터링 설정

문제가 있거나 질문이 있으시면 GitHub Issues에 남겨주세요!