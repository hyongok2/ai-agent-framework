---
inclusion: always
---

# 테스트 전략 및 품질 관리 가이드

## 테스트 피라미드 구조

```
    /\
   /  \     E2E Tests (적음)
  /____\    
 /      \   Integration Tests (중간)
/________\  Unit Tests (많음)
```

### 1. 단위 테스트 (Unit Tests) - 70%

각 클래스와 메서드의 독립적인 기능을 테스트합니다.

```csharp
[TestFixture]
public class PromptManagerTests
{
    private IPromptManager _promptManager;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<IMemoryCache> _mockCache;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCache = new Mock<IMemoryCache>();
        _promptManager = new PromptManager(_mockConfiguration.Object, _mockCache.Object);
    }

    [Test]
    public async Task LoadPromptAsync_WithValidParameters_ShouldReturnProcessedPrompt()
    {
        // Arrange
        var role = "planner";
        var parameters = new Dictionary<string, object>
        {
            ["user_request"] = "Create a web application",
            ["available_tools"] = "web_search, database_query"
        };

        var expectedTemplate = "You are a {role}. User request: {user_request}. Available tools: {available_tools}";
        var expectedResult = "You are a planner. User request: Create a web application. Available tools: web_search, database_query";

        // Mock file system
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.ReadAllTextAsync(It.IsAny<string>()))
                     .ReturnsAsync(expectedTemplate);

        // Act
        var result = await _promptManager.LoadPromptAsync(role, parameters);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task LoadPromptAsync_WithMissingParameter_ShouldThrowArgumentException()
    {
        // Arrange
        var role = "planner";
        var parameters = new Dictionary<string, object>(); // 빈 파라미터

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _promptManager.LoadPromptAsync(role, parameters));
        
        Assert.That(ex.Message, Does.Contain("Required parameter"));
    }
}
```

### 2. 통합 테스트 (Integration Tests) - 20%

여러 구성 요소 간의 상호작용을 테스트합니다.

```csharp
[TestFixture]
public class OrchestrationEngineIntegrationTests
{
    private IServiceProvider _serviceProvider;
    private IOrchestrationEngine _orchestrationEngine;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        
        // 실제 서비스 등록 (테스트용 설정)
        services.AddScoped<IOrchestrationEngine, OrchestrationEngine>();
        services.AddScoped<ILLMSystem, LLMSystem>();
        services.AddScoped<IToolSystem, ToolSystem>();
        services.AddSingleton<IRegistry, Registry>();
        
        // Mock 서비스 등록
        services.AddScoped<ILLMProvider>(provider => 
        {
            var mock = new Mock<ILLMProvider>();
            mock.Setup(p => p.GenerateAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"actions\": [], \"is_completed\": true}");
            return mock.Object;
        });

        _serviceProvider = services.BuildServiceProvider();
        _orchestrationEngine = _serviceProvider.GetRequiredService<IOrchestrationEngine>();
    }

    [Test]
    public async Task ExecuteAsync_WithSimpleRequest_ShouldCompleteSuccessfully()
    {
        // Arrange
        var request = new UserRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            UserId = "test_user",
            Content = "Hello, how are you?",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        var result = await _orchestrationEngine.ExecuteAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(result.ExecutionSteps, Is.Not.Empty);
    }

    [Test]
    public async Task ExecuteAsync_WithComplexRequest_ShouldExecuteMultipleSteps()
    {
        // Arrange
        var request = new UserRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            UserId = "test_user",
            Content = "Search for information about AI and summarize it",
            RequestedAt = DateTime.UtcNow
        };

        // Mock LLM to return a plan with multiple steps
        var mockLLMProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
        Mock.Get(mockLLMProvider)
            .Setup(p => p.GenerateAsync(It.IsAny<string>()))
            .ReturnsAsync("{\"actions\": [{\"type\": \"TOOL\", \"name\": \"web_search\"}, {\"type\": \"LLM\", \"name\": \"summarizer\"}], \"is_completed\": false}");

        // Act
        var result = await _orchestrationEngine.ExecuteAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.ExecutionSteps.Count, Is.GreaterThan(1));
    }
}
```

### 3. E2E 테스트 (End-to-End Tests) - 10%

전체 시스템의 완전한 워크플로우를 테스트합니다.

```csharp
[TestFixture]
public class AIAgentFrameworkE2ETests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 테스트용 설정 오버라이드
                    services.Configure<LLMConfiguration>(config =>
                    {
                        config.DefaultProvider = "mock";
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [Test]
    public async Task CompleteWorkflow_FromRequestToResponse_ShouldWork()
    {
        // Arrange
        var request = new
        {
            userId = "test_user",
            content = "Create a simple web page with a greeting message"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orchestration/execute", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OrchestrationResult>();
        
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.FinalResponse, Is.Not.Null.And.Not.Empty);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

## 테스트 더블 (Test Doubles) 활용

### 1. Mock 객체 사용

```csharp
public class LLMSystemTests
{
    [Test]
    public async Task ExecutePlannerAsync_ShouldCallProviderWithCorrectPrompt()
    {
        // Arrange
        var mockProvider = new Mock<ILLMProvider>();
        var mockPromptManager = new Mock<IPromptManager>();
        var mockLogger = new Mock<ILogger<LLMSystem>>();

        var expectedPrompt = "You are a planner...";
        mockPromptManager.Setup(pm => pm.LoadPromptAsync("planner", It.IsAny<Dictionary<string, object>>()))
                        .ReturnsAsync(expectedPrompt);

        mockProvider.Setup(p => p.GenerateAsync(expectedPrompt))
                   .ReturnsAsync("{\"actions\": [], \"is_completed\": true}");

        var llmSystem = new LLMSystem(mockProvider.Object, mockPromptManager.Object, mockLogger.Object);

        // Act
        var context = new OrchestrationContext { /* 테스트 데이터 */ };
        await llmSystem.ExecutePlannerAsync(context);

        // Assert
        mockProvider.Verify(p => p.GenerateAsync(expectedPrompt), Times.Once);
        mockPromptManager.Verify(pm => pm.LoadPromptAsync("planner", It.IsAny<Dictionary<string, object>>()), Times.Once);
    }
}
```

### 2. Stub 객체 사용

```csharp
public class StubLLMProvider : ILLMProvider
{
    private readonly Dictionary<string, string> _responses = new();

    public void SetResponse(string prompt, string response)
    {
        _responses[prompt] = response;
    }

    public Task<string> GenerateAsync(string prompt)
    {
        return Task.FromResult(_responses.GetValueOrDefault(prompt, "Default response"));
    }

    public Task<T> GenerateStructuredAsync<T>(string prompt) where T : class
    {
        var response = _responses.GetValueOrDefault(prompt, "{}");
        var result = JsonConvert.DeserializeObject<T>(response);
        return Task.FromResult(result);
    }
}

[Test]
public async Task TestWithStub()
{
    // Arrange
    var stubProvider = new StubLLMProvider();
    stubProvider.SetResponse("test prompt", "{\"result\": \"success\"}");
    
    var service = new SomeService(stubProvider);

    // Act
    var result = await service.ProcessAsync("test prompt");

    // Assert
    Assert.That(result, Is.Not.Null);
}
```

## 테스트 데이터 관리

### 1. Builder Pattern 활용

```csharp
public class UserRequestBuilder
{
    private string _requestId = Guid.NewGuid().ToString();
    private string _userId = "default_user";
    private string _content = "default content";
    private DateTime _requestedAt = DateTime.UtcNow;
    private Dictionary<string, object> _metadata = new();

    public UserRequestBuilder WithRequestId(string requestId)
    {
        _requestId = requestId;
        return this;
    }

    public UserRequestBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public UserRequestBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public UserRequestBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public UserRequest Build()
    {
        return new UserRequest
        {
            RequestId = _requestId,
            UserId = _userId,
            Content = _content,
            RequestedAt = _requestedAt,
            Metadata = _metadata
        };
    }
}

// 사용 예시
[Test]
public async Task TestWithBuilder()
{
    // Arrange
    var request = new UserRequestBuilder()
        .WithUserId("test_user")
        .WithContent("Test request content")
        .WithMetadata("priority", "high")
        .Build();

    // Act & Assert
    // ...
}
```

### 2. Object Mother Pattern

```csharp
public static class TestDataMother
{
    public static UserRequest SimpleUserRequest()
    {
        return new UserRequest
        {
            RequestId = "simple_request_001",
            UserId = "simple_user",
            Content = "Hello, world!",
            RequestedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            Metadata = new Dictionary<string, object>()
        };
    }

    public static UserRequest ComplexUserRequest()
    {
        return new UserRequest
        {
            RequestId = "complex_request_001",
            UserId = "power_user",
            Content = "Analyze the market trends and create a comprehensive report",
            RequestedAt = new DateTime(2024, 1, 1, 12, 0, 0),
            Metadata = new Dictionary<string, object>
            {
                ["priority"] = "high",
                ["department"] = "analytics",
                ["deadline"] = "2024-01-15"
            }
        };
    }

    public static OrchestrationContext DefaultOrchestrationContext()
    {
        return new OrchestrationContext
        {
            SessionId = "test_session_001",
            OriginalRequest = SimpleUserRequest(),
            ExecutionHistory = new List<IExecutionStep>(),
            SharedData = new Dictionary<string, object>(),
            StartedAt = DateTime.UtcNow
        };
    }
}
```

## 성능 테스트

### 1. 부하 테스트

```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    public async Task OrchestrationEngine_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var engine = CreateOrchestrationEngine();
        var requests = Enumerable.Range(1, 100)
            .Select(i => TestDataMother.SimpleUserRequest())
            .ToList();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = requests.Select(request => engine.ExecuteAsync(request));
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        Assert.That(results.All(r => r.IsSuccess), Is.True);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10000)); // 10초 이내
        
        Console.WriteLine($"Processed {requests.Count} requests in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / requests.Count}ms per request");
    }

    [Test]
    public async Task MemoryUsage_ShouldNotExceedThreshold()
    {
        // Arrange
        var engine = CreateOrchestrationEngine();
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var request = TestDataMother.SimpleUserRequest();
            await engine.ExecuteAsync(request);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.That(memoryIncrease, Is.LessThan(50 * 1024 * 1024)); // 50MB 이하
        
        Console.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024}MB");
    }
}
```

## 테스트 커버리지 및 품질 메트릭

### 1. 커버리지 목표
- **라인 커버리지**: 최소 80%
- **브랜치 커버리지**: 최소 70%
- **메서드 커버리지**: 최소 90%

### 2. 품질 게이트
```xml
<!-- .runsettings 파일 -->
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage" uri="datacollector://Microsoft/CodeCoverage/2.0">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*AIAgentFramework\.Core\.dll$</ModulePath>
                <ModulePath>.*AIAgentFramework\.LLM\.dll$</ModulePath>
                <ModulePath>.*AIAgentFramework\.Tools\.dll$</ModulePath>
              </Include>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### 3. 정적 분석 도구 설정
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)analyzers.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="7.0.4" PrivateAssets="all" />
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.12.0.78982" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

## 테스트 자동화 및 CI/CD

### 1. GitHub Actions 워크플로우
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run Unit Tests
      run: dotnet test --no-build --configuration Release --logger trx --collect:"XPlat Code Coverage"
    
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:"Html;Cobertura"
    
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage/Cobertura.xml
```

이러한 테스트 전략과 품질 관리 가이드를 따라 신뢰할 수 있고 유지보수 가능한 AI 에이전트 프레임워크를 구축하세요.