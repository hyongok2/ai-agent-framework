# Plan 5: DI 컨테이너 설정 및 통합 테스트

## 📋 개요

**목표**: 전체 인프라의 통합 동작 검증  
**예상 소요 시간**: 1일 (8시간)  
**의존성**: Plan 4 (BaseLLMFunction 추상 클래스) 완료

## 🎯 구체적 목표

1. ✅ **완전한 DI 컨테이너 설정** 완성
2. ✅ **Host 프로젝트** 구축 및 통합
3. ✅ **포괄적인 통합 테스트** 완성
4. ✅ **문서화 및 샘플** 제공

## 🏗️ 작업 단계

### **Task 5.1: Host 프로젝트 생성 및 DI 설정** (3시간)

#### **AIAgent.Host 프로젝트 생성**
```bash
# Host 프로젝트 생성 (Console App)
dotnet new console -n AIAgent.Host -o src/AIAgent.Host --framework net8.0
dotnet sln add src/AIAgent.Host

# 필요한 패키지 참조 추가
dotnet add src/AIAgent.Host reference src/AIAgent.Core
dotnet add src/AIAgent.Host reference src/AIAgent.Common
dotnet add src/AIAgent.Host reference src/AIAgent.LLM

# NuGet 패키지 추가
dotnet add src/AIAgent.Host package Microsoft.Extensions.Hosting
dotnet add src/AIAgent.Host package Microsoft.Extensions.DependencyInjection
dotnet add src/AIAgent.Host package Microsoft.Extensions.Configuration
dotnet add src/AIAgent.Host package Microsoft.Extensions.Configuration.Json
dotnet add src/AIAgent.Host package Serilog.Extensions.Hosting
dotnet add src/AIAgent.Host package Serilog.Sinks.Console
dotnet add src/AIAgent.Host package Serilog.Sinks.File
dotnet add src/AIAgent.Host package Serilog.Formatting.Compact
```

#### **AIAgent.Host 프로젝트 구조**
```
src/AIAgent.Host/
├── Program.cs                      # 진입점
├── HostedServices/
│   └── AgentBackgroundService.cs   # 백그라운드 서비스
├── Configuration/
│   ├── ServiceConfiguration.cs     # 서비스 구성
│   └── LoggingConfiguration.cs     # 로깅 구성
├── Examples/
│   ├── BasicUsageExample.cs        # 기본 사용법 예시
│   └── PlannerFunctionExample.cs   # PlannerFunction 예시
├── appsettings.json                # 기본 설정
├── appsettings.Development.json    # 개발 설정
└── appsettings.Production.json     # 운영 설정
```

#### **Program.cs 구현**
```csharp
using AIAgent.Common.Configuration;
using AIAgent.Common.Logging;
using AIAgent.Host.Configuration;
using AIAgent.Host.HostedServices;
using AIAgent.LLM.Extensions;
using Serilog;

namespace AIAgent.Host;

/// <summary>
/// AI Agent 호스트 애플리케이션의 진입점입니다.
/// </summary>
public static class Program
{
    /// <summary>
    /// 애플리케이션을 시작합니다.
    /// </summary>
    /// <param name="args">명령줄 인수</param>
    /// <returns>종료 코드</returns>
    public static async Task<int> Main(string[] args)
    {
        // 초기 로깅 설정
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting AI Agent Host...");

            // Host Builder 구성
            var builder = Host.CreateApplicationBuilder(args);

            // 설정 구성
            ConfigureConfiguration(builder);

            // 서비스 구성
            ConfigureServices(builder.Services, builder.Configuration);

            // 로깅 구성
            ConfigureLogging(builder);

            // 호스팅 서비스 추가
            builder.Services.AddHostedService<AgentBackgroundService>();

            // Host 빌드
            using var host = builder.Build();

            // LLM 시스템 초기화
            InitializeLLMSystem(host.Services);

            Log.Information("AI Agent Host started successfully");

            // 애플리케이션 실행
            await host.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "AI Agent Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// 설정을 구성합니다.
    /// </summary>
    private static void ConfigureConfiguration(HostApplicationBuilder builder)
    {
        builder.Configuration.Sources.Clear();

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args: Environment.GetCommandLineArgs());
    }

    /// <summary>
    /// 서비스를 구성합니다.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 설정 바인딩
        services.Configure<AgentSettings>(configuration.GetSection("Agent"));
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();

        // 공통 서비스
        services.AddSingleton<IStructuredLogger, StructuredLogger>();

        // LLM 시스템
        services.AddLLMSystem(configuration);

        // 예제 서비스들
        services.AddTransient<BasicUsageExample>();
        services.AddTransient<PlannerFunctionExample>();

        // 헬스체크
        services.AddHealthChecks()
            .AddCheck<AgentHealthCheck>("agent_health");

        // 메트릭스 (선택사항)
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
    }

    /// <summary>
    /// 로깅을 구성합니다.
    /// </summary>
    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.WithCorrelationId()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(
                    path: "logs/aiagent-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    formatter: new CompactJsonFormatter());
        });
    }

    /// <summary>
    /// LLM 시스템을 초기화합니다.
    /// </summary>
    private static void InitializeLLMSystem(IServiceProvider serviceProvider)
    {
        try
        {
            var registry = serviceProvider.InitializeLLMSystem();
            Log.Information("LLM System initialized with {FunctionCount} functions", registry.Count);

            // 등록된 기능들 로깅
            foreach (var function in registry.GetAll())
            {
                Log.Information("Registered LLM Function: {Role} - {Description}", 
                    function.Role, function.Description);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to initialize LLM System");
            throw;
        }
    }
}
```

#### **ServiceConfiguration.cs 구현**
```csharp
namespace AIAgent.Host.Configuration;

/// <summary>
/// 서비스 구성을 담당하는 클래스입니다.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// 핵심 서비스들을 구성합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">설정</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 설정 모델들
        services.Configure<AgentSettings>(configuration.GetSection("Agent"));
        services.Configure<LLMProviderSettings>(configuration.GetSection("Agent:LLMProvider"));
        services.Configure<LoggingSettings>(configuration.GetSection("Logging"));

        // Configuration Manager
        services.AddSingleton<IConfigurationManager>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            return new AIAgent.Common.Configuration.ConfigurationManager(config);
        });

        return services;
    }

    /// <summary>
    /// 유틸리티 서비스들을 구성합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddUtilityServices(this IServiceCollection services)
    {
        // 로깅
        services.AddSingleton<IStructuredLogger, StructuredLogger>();

        // JSON 직렬화
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();

        // 검증
        services.AddTransient(typeof(IValidator<>), typeof(ValidatorBase<>));

        // 메트릭스 수집
        services.AddSingleton<IMetricsCollector, MetricsCollector>();

        return services;
    }

    /// <summary>
    /// 개발 환경을 위한 서비스들을 구성합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddDevelopmentServices(this IServiceCollection services)
    {
        // Mock LLM Provider (실제 API 호출 없이 테스트용)
        services.AddSingleton<ILLMProvider, MockLLMProvider>();

        // Mock Prompt Manager
        services.AddSingleton<IPromptManager, MockPromptManager>();

        // 개발용 헬스체크
        services.AddHealthChecks()
            .AddCheck<DevelopmentHealthCheck>("development_health");

        return services;
    }
}
```

#### **appsettings.json 구현**
```json
{
  "Agent": {
    "Name": "AI Agent Framework",
    "Version": "0.1.0",
    "DefaultTimeoutSeconds": 30,
    "MaxConcurrentExecutions": 10,
    "MaxConversationHistory": 50,
    "LLMProvider": {
      "DefaultProvider": "OpenAI",
      "Providers": {
        "OpenAI": {
          "Enabled": true,
          "Model": "gpt-4",
          "ApiKey": "${OPENAI_API_KEY}",
          "MaxTokens": 4096,
          "Temperature": 0.7,
          "TimeoutSeconds": 30,
          "MaxRetries": 3
        },
        "Mock": {
          "Enabled": true,
          "Model": "mock-gpt-4",
          "MaxTokens": 4096,
          "Temperature": 0.7,
          "TimeoutSeconds": 1,
          "MaxRetries": 0
        }
      }
    },
    "Tools": {
      "PluginPath": "./plugins",
      "MCPEndpoints": []
    },
    "Logging": {
      "EnableStructuredLogging": true,
      "LogLevel": "Information",
      "EnablePerformanceLogging": true,
      "EnableUserActivityLogging": false
    },
    "Performance": {
      "EnableCaching": true,
      "CacheTTLSeconds": 300,
      "MaxCacheSize": 1000,
      "EnableMetrics": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "AIAgent": "Debug"
    }
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "AIAgent": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/aiagent-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"],
    "Properties": {
      "Application": "AI Agent Framework"
    }
  }
}
```

### **Task 5.2: Mock 구현체들** (2시간)

#### **MockLLMProvider.cs 구현**
```csharp
namespace AIAgent.Host.Services;

/// <summary>
/// 개발 및 테스트용 Mock LLM Provider입니다.
/// </summary>
public sealed class MockLLMProvider : ILLMProvider
{
    private readonly ILogger<MockLLMProvider> _logger;
    private readonly Random _random = new();

    public MockLLMProvider(ILogger<MockLLMProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Provider의 이름입니다.
    /// </summary>
    public string Name => "Mock";

    /// <summary>
    /// 지원하는 모델 목록입니다.
    /// </summary>
    public IEnumerable<string> SupportedModels => new[] { "mock-gpt-4", "mock-gpt-3.5-turbo", "mock-claude" };

    /// <summary>
    /// 현재 활성화된 모델입니다.
    /// </summary>
    public string CurrentModel { get; private set; } = "mock-gpt-4";

    /// <summary>
    /// Provider가 현재 사용 가능한지 확인합니다.
    /// </summary>
    public bool IsAvailable => true;

    /// <summary>
    /// LLM 호출을 시뮬레이션합니다.
    /// </summary>
    /// <param name="request">LLM 요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>모의 LLM 응답</returns>
    public async Task<LLMResponse> CallAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock LLM call: {Prompt}", request.Prompt?.Truncate(100));

        // 실제 LLM 호출을 시뮬레이션하기 위한 지연
        var delay = _random.Next(500, 2000);
        await Task.Delay(delay, cancellationToken);

        // 프롬프트에 따른 다른 응답 생성
        var response = GenerateMockResponse(request);

        return new LLMResponse
        {
            Content = response,
            Model = CurrentModel,
            TokensUsed = EstimateTokenCount(request.Prompt + response),
            FinishReason = "completed",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = Name,
                ["model"] = CurrentModel,
                ["mock_delay_ms"] = delay
            }
        };
    }

    /// <summary>
    /// 모델을 변경합니다.
    /// </summary>
    /// <param name="modelName">변경할 모델 이름</param>
    public Task SetModelAsync(string modelName)
    {
        if (SupportedModels.Contains(modelName))
        {
            CurrentModel = modelName;
            _logger.LogInformation("Changed model to: {Model}", modelName);
        }
        else
        {
            throw new ArgumentException($"Unsupported model: {modelName}", nameof(modelName));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Provider의 상태를 확인합니다.
    /// </summary>
    /// <returns>항상 정상 상태를 반환</returns>
    public Task<ProviderHealthCheck> CheckHealthAsync()
    {
        return Task.FromResult(new ProviderHealthCheck
        {
            IsHealthy = true,
            ResponseTime = TimeSpan.FromMilliseconds(_random.Next(50, 200)),
            LastChecked = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["provider"] = Name,
                ["model"] = CurrentModel,
                ["status"] = "Mock provider is always healthy"
            }
        });
    }

    /// <summary>
    /// 프롬프트에 따른 모의 응답을 생성합니다.
    /// </summary>
    private string GenerateMockResponse(LLMRequest request)
    {
        var prompt = request.Prompt?.ToLowerInvariant() ?? string.Empty;

        // 계획 수립 요청 감지
        if (prompt.Contains("plan") || prompt.Contains("step") || prompt.Contains("create"))
        {
            return GeneratePlanningResponse(request);
        }

        // 분석 요청 감지
        if (prompt.Contains("analyz") || prompt.Contains("review") || prompt.Contains("examine"))
        {
            return GenerateAnalysisResponse(request);
        }

        // 질문 응답
        if (prompt.Contains("?") || prompt.Contains("what") || prompt.Contains("how") || prompt.Contains("why"))
        {
            return GenerateQuestionResponse(request);
        }

        // 기본 응답
        return GenerateDefaultResponse(request);
    }

    private string GeneratePlanningResponse(LLMRequest request)
    {
        return """
        {
          "plan_id": "mock_plan_001",
          "summary": "Generated mock plan for user request",
          "steps": [
            {
              "order": 1,
              "description": "Analyze user requirements and constraints",
              "type": "Analysis",
              "estimated_duration": "PT5M",
              "dependencies": []
            },
            {
              "order": 2,
              "description": "Research available resources and tools",
              "type": "Research",
              "estimated_duration": "PT10M",
              "dependencies": [1]
            },
            {
              "order": 3,
              "description": "Create detailed implementation strategy",
              "type": "Planning",
              "estimated_duration": "PT15M",
              "dependencies": [2]
            },
            {
              "order": 4,
              "description": "Execute the planned approach",
              "type": "Execution",
              "estimated_duration": "PT30M",
              "dependencies": [3]
            },
            {
              "order": 5,
              "description": "Review and validate results",
              "type": "Validation",
              "estimated_duration": "PT10M",
              "dependencies": [4]
            }
          ],
          "estimated_total_duration": "PT70M",
          "success_criteria": [
            "All requirements are met",
            "Solution is tested and validated",
            "Documentation is complete"
          ],
          "created_at": "2024-01-01T00:00:00Z"
        }
        """;
    }

    private string GenerateAnalysisResponse(LLMRequest request)
    {
        return """
        Based on the mock analysis, I can identify the following key points:

        **Strengths:**
        - Clear structure and organization
        - Good use of established patterns
        - Comprehensive error handling

        **Areas for Improvement:**
        - Consider adding more detailed logging
        - Performance could be optimized in certain areas
        - Documentation could be expanded

        **Recommendations:**
        1. Implement comprehensive monitoring
        2. Add performance benchmarks
        3. Consider implementing caching strategies
        4. Enhance error recovery mechanisms

        **Risk Assessment:** Low to Medium
        - Most components appear stable
        - Some areas may need additional testing

        This is a mock analysis response for testing purposes.
        """;
    }

    private string GenerateQuestionResponse(LLMRequest request)
    {
        var responses = new[]
        {
            "This is a mock response to your question. In a real implementation, I would provide a detailed, accurate answer based on my training data and the specific context of your question.",
            "Thank you for your question. This mock response demonstrates how the system would handle Q&A interactions. The actual implementation would leverage advanced language models to provide helpful and accurate responses.",
            "Your question has been received and processed by the mock LLM provider. In production, this would be answered by a sophisticated language model with access to extensive knowledge."
        };

        return responses[_random.Next(responses.Length)];
    }

    private string GenerateDefaultResponse(LLMRequest request)
    {
        return "This is a mock response from the Mock LLM Provider. In a production environment, this would be replaced by actual responses from language models like GPT-4, Claude, or other LLM providers. The mock provider helps with development and testing without requiring actual API calls.";
    }

    /// <summary>
    /// 대략적인 토큰 수를 추정합니다.
    /// </summary>
    private int EstimateTokenCount(string text)
    {
        // 간단한 토큰 수 추정 (실제로는 더 복잡함)
        return string.IsNullOrEmpty(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
```

#### **MockPromptManager.cs 구현**
```csharp
namespace AIAgent.Host.Services;

/// <summary>
/// 개발 및 테스트용 Mock Prompt Manager입니다.
/// </summary>
public sealed class MockPromptManager : IPromptManager
{
    private readonly ILogger<MockPromptManager> _logger;
    private readonly Dictionary<string, string> _mockPrompts;

    public MockPromptManager(ILogger<MockPromptManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mockPrompts = InitializeMockPrompts();
    }

    /// <summary>
    /// 프롬프트를 비동기적으로 가져옵니다.
    /// </summary>
    /// <param name="promptName">프롬프트 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>프롬프트 내용</returns>
    public Task<string?> GetPromptAsync(string promptName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting mock prompt: {PromptName}", promptName);

        _mockPrompts.TryGetValue(promptName.ToLowerInvariant(), out var prompt);
        return Task.FromResult(prompt);
    }

    /// <summary>
    /// 모든 사용 가능한 프롬프트 이름을 가져옵니다.
    /// </summary>
    /// <returns>프롬프트 이름 목록</returns>
    public Task<IEnumerable<string>> GetAvailablePromptsAsync()
    {
        return Task.FromResult(_mockPrompts.Keys.AsEnumerable());
    }

    /// <summary>
    /// 프롬프트 캐시를 지웁니다.
    /// </summary>
    public Task ClearCacheAsync()
    {
        _logger.LogInformation("Mock prompt cache cleared");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Mock 프롬프트들을 초기화합니다.
    /// </summary>
    private Dictionary<string, string> InitializeMockPrompts()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planner"] = """
                # Planning Assistant

                You are an expert planning assistant. Your task is to analyze the user's request and create a detailed, actionable plan.

                ## Current Context
                - Current Time: {{CURRENT_TIME}}
                - User Message: {{USER_MESSAGE}}
                - Available Tools: {{AVAILABLE_TOOLS}}
                - Available Functions: {{AVAILABLE_FUNCTIONS}}
                - Conversation History: {{CONVERSATION_HISTORY}}

                ## Instructions
                1. Carefully analyze the user's request to understand their goals and constraints
                2. Break down the task into logical, sequential steps
                3. Consider available tools and functions for each step
                4. Estimate time requirements for each step
                5. Identify potential risks or challenges
                6. Provide clear success criteria

                ## Output Format
                Respond with a JSON object containing:
                - plan_id: Unique identifier for this plan
                - summary: Brief description of what the plan accomplishes
                - steps: Array of step objects with order, description, type, estimated_duration, dependencies
                - estimated_total_duration: Total time estimate in ISO 8601 duration format
                - success_criteria: Array of criteria that define success
                - created_at: ISO 8601 timestamp

                Be thorough but concise. Focus on actionable steps that can be executed systematically.
                """,

            ["analyzer"] = """
                # Analysis Assistant

                You are an expert analyst. Examine the provided content thoroughly and provide insights.

                ## Content to Analyze
                {{CONTENT}}

                ## Analysis Focus Areas
                {{FOCUS_AREAS}}

                ## Instructions
                1. Perform comprehensive analysis of the provided content
                2. Identify key patterns, strengths, and areas for improvement
                3. Provide actionable recommendations
                4. Assess risks and opportunities
                5. Support findings with specific examples

                Provide clear, structured analysis with concrete insights and recommendations.
                """,

            ["generator"] = """
                # Content Generator

                You are a creative content generator. Create high-quality content based on the requirements.

                ## Requirements
                {{REQUIREMENTS}}

                ## Content Type
                {{CONTENT_TYPE}}

                ## Style Guidelines
                {{STYLE_GUIDELINES}}

                ## Instructions
                1. Create original, high-quality content that meets all requirements
                2. Follow the specified style guidelines
                3. Ensure content is appropriate for the intended audience
                4. Include relevant examples or illustrations where helpful
                5. Proofread for clarity, accuracy, and engagement

                Generate content that is valuable, engaging, and fit for purpose.
                """
        };
    }
}
```

### **Task 5.3: 통합 테스트 구현** (2.5시간)

#### **통합 테스트 프로젝트 생성**
```bash
# 통합 테스트 프로젝트 생성
dotnet new xunit -n AIAgent.Integration.Tests -o tests/AIAgent.Integration.Tests --framework net8.0
dotnet sln add tests/AIAgent.Integration.Tests

# 프로젝트 참조 추가
dotnet add tests/AIAgent.Integration.Tests reference src/AIAgent.Core
dotnet add tests/AIAgent.Integration.Tests reference src/AIAgent.Common
dotnet add tests/AIAgent.Integration.Tests reference src/AIAgent.LLM
dotnet add tests/AIAgent.Integration.Tests reference src/AIAgent.Host

# 테스트 패키지 추가
dotnet add tests/AIAgent.Integration.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/AIAgent.Integration.Tests package Microsoft.Extensions.Hosting.Testing
dotnet add tests/AIAgent.Integration.Tests package Testcontainers
```

#### **InfrastructureTests.cs 구현**
```csharp
namespace AIAgent.Integration.Tests;

/// <summary>
/// 인프라 구성요소들의 통합 테스트입니다.
/// </summary>
public class InfrastructureTests : IClassFixture<TestHostFixture>
{
    private readonly TestHostFixture _fixture;
    private readonly ITestOutputHelper _output;

    public InfrastructureTests(TestHostFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ConfigurationManager_Should_LoadSettings_Successfully()
    {
        // Arrange
        var configManager = _fixture.GetService<IConfigurationManager>();

        // Act
        var agentSettings = configManager.GetSection<AgentSettings>("Agent");

        // Assert
        agentSettings.Should().NotBeNull();
        agentSettings.Name.Should().NotBeNullOrEmpty();
        agentSettings.Version.Should().NotBeNullOrEmpty();
        agentSettings.DefaultTimeoutSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StructuredLogger_Should_LogWithCorrelation_Successfully()
    {
        // Arrange
        var logger = _fixture.GetService<IStructuredLogger>();
        var testCorrelationId = Guid.NewGuid().ToString("N")[..12];

        // Act
        using var scope = LogCorrelation.SetCorrelationId(testCorrelationId);
        logger.LogInfo(new EventId(1001), "Test structured logging", new { TestProperty = "TestValue" });

        // Assert
        LogCorrelation.CorrelationId.Should().Be(testCorrelationId);
        // Note: 실제 로그 검증은 로그 출력을 캡처하는 추가 로직 필요
    }

    [Fact]
    public async Task ValidationFramework_Should_ValidateObjects_Successfully()
    {
        // Arrange
        var testObject = new TestValidationObject { Name = "Test", Value = 42 };
        var validator = new TestObjectValidator();

        // Act
        var result = await validator.ValidateAsync(testObject);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidationFramework_Should_DetectInvalidObjects_Successfully()
    {
        // Arrange
        var invalidObject = new TestValidationObject { Name = "", Value = -1 };
        var validator = new TestObjectValidator();

        // Act
        var result = await validator.ValidateAsync(invalidObject);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(error => error.Contains("Name"));
        result.Errors.Should().Contain(error => error.Contains("Value"));
    }

    [Theory]
    [InlineData("hello world", "aGVsbG8gd29ybGQ=")]
    [InlineData("AI Agent Framework", "QUkgQWdlbnQgRnJhbWV3b3Jr")]
    public void StringExtensions_ToBase64_Should_EncodeCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToBase64();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("aGVsbG8gd29ybGQ=", "hello world")]
    [InlineData("QUkgQWdlbnQgRnJhbWV3b3Jr", "AI Agent Framework")]
    public void StringExtensions_FromBase64_Should_DecodeCorrectly(string input, string expected)
    {
        // Act
        var result = input.FromBase64();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void StringExtensions_IsNullOrWhiteSpace_Should_DetectEmptyStrings(string input)
    {
        // Act & Assert
        input.IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("  Hello  ")]
    public void StringExtensions_HasValue_Should_DetectNonEmptyStrings(string input)
    {
        // Act & Assert
        input.HasValue().Should().BeTrue();
    }
}

/// <summary>
/// 테스트용 검증 객체입니다.
/// </summary>
internal class TestValidationObject
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// 테스트용 검증자입니다.
/// </summary>
internal class TestObjectValidator : ValidatorBase<TestValidationObject>
{
    public TestObjectValidator()
    {
        AddRule(obj => !string.IsNullOrWhiteSpace(obj.Name), "Name is required");
        AddRule(obj => obj.Value >= 0, "Value must be non-negative");
    }
}
```

#### **LLMFunctionTests.cs 구현**
```csharp
namespace AIAgent.Integration.Tests;

/// <summary>
/// LLM Function 시스템의 통합 테스트입니다.
/// </summary>
public class LLMFunctionTests : IClassFixture<TestHostFixture>
{
    private readonly TestHostFixture _fixture;
    private readonly ITestOutputHelper _output;

    public LLMFunctionTests(TestHostFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task LLMFunctionRegistry_Should_RegisterFunctions_Successfully()
    {
        // Arrange
        var registry = _fixture.GetService<ILLMFunctionRegistry>();

        // Act
        var allFunctions = registry.GetAll();
        var plannerFunction = registry.GetByRole("Planner");

        // Assert
        allFunctions.Should().NotBeEmpty();
        plannerFunction.Should().NotBeNull();
        plannerFunction!.Role.Should().Be("Planner");
        plannerFunction.Description.Should().NotBeNullOrEmpty();
        registry.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PlannerFunction_Should_ExecuteSuccessfully_WithValidInput()
    {
        // Arrange
        var registry = _fixture.GetService<ILLMFunctionRegistry>();
        var plannerFunction = registry.GetByRole("Planner");
        var mockProvider = _fixture.GetService<ILLMProvider>();

        var context = CreateTestLLMContext("Create a plan to build a simple web application", mockProvider);

        // Act
        var result = await plannerFunction!.ExecuteAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.FunctionType.Should().Be("Planner");
        result.Response.Should().NotBeNull();
        result.ExecutedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PlannerFunction_Should_HandleInvalidInput_Gracefully()
    {
        // Arrange
        var registry = _fixture.GetService<ILLMFunctionRegistry>();
        var plannerFunction = registry.GetByRole("Planner");
        var mockProvider = _fixture.GetService<ILLMProvider>();

        var context = CreateTestLLMContext("", mockProvider); // Empty message

        // Act
        var result = await plannerFunction!.ExecuteAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PlannerFunction_Should_RespectCancellation()
    {
        // Arrange
        var registry = _fixture.GetService<ILLMFunctionRegistry>();
        var plannerFunction = registry.GetByRole("Planner");
        var mockProvider = _fixture.GetService<ILLMProvider>();

        var context = CreateTestLLMContext("Create a complex plan", mockProvider);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        cancellationTokenSource.CancelAfter(100); // Cancel after 100ms
        var result = await plannerFunction!.ExecuteAsync(context, cancellationTokenSource.Token);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task LLMFunctionRegistry_Should_SupportGenericRetrieval()
    {
        // Arrange
        var registry = _fixture.GetService<ILLMFunctionRegistry>();

        // Act
        var plannerFunction = registry.GetByType<PlannerFunction>();

        // Assert
        plannerFunction.Should().NotBeNull();
        plannerFunction.Should().BeOfType<PlannerFunction>();
        plannerFunction!.Role.Should().Be("Planner");
    }

    [Fact]
    public async Task LLMFunctionRegistry_Should_FilterAvailableFunctions()
    {
        // Arrange
        var registry = _fixture.GetService<ILLMFunctionRegistry>();
        var mockProvider = _fixture.GetService<ILLMProvider>();
        var context = CreateTestLLMContext("Create a plan for something", mockProvider);

        // Act
        var availableFunctions = registry.GetAvailableFunctions(context);

        // Assert
        availableFunctions.Should().NotBeEmpty();
        availableFunctions.Should().Contain(f => f.Role == "Planner");
    }

    [Fact]
    public async Task MockLLMProvider_Should_ProvideMockResponses()
    {
        // Arrange
        var provider = _fixture.GetService<ILLMProvider>();
        var request = new LLMRequest
        {
            Prompt = "Create a plan to test the system",
            MaxTokens = 1000,
            Temperature = 0.7
        };

        // Act
        var response = await provider.CallAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Content.Should().NotBeNullOrEmpty();
        response.Model.Should().StartWith("mock-");
        response.TokensUsed.Should().BeGreaterThan(0);
        response.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task MockLLMProvider_Should_HandleHealthChecks()
    {
        // Arrange
        var provider = _fixture.GetService<ILLMProvider>();

        // Act
        var healthCheck = await provider.CheckHealthAsync();

        // Assert
        healthCheck.Should().NotBeNull();
        healthCheck.IsHealthy.Should().BeTrue();
        healthCheck.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        healthCheck.LastChecked.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// 테스트용 LLM 컨텍스트를 생성합니다.
    /// </summary>
    private static ILLMContext CreateTestLLMContext(string userMessage, ILLMProvider provider)
    {
        var request = new AgentRequest
        {
            RequestId = Guid.NewGuid().ToString("N")[..12],
            UserMessage = userMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        return new LLMContext
        {
            Request = request,
            LLMProvider = provider,
            CorrelationId = LogCorrelation.GenerateCorrelationId(),
            Variables = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

#### **TestHostFixture.cs 구현**
```csharp
namespace AIAgent.Integration.Tests;

/// <summary>
/// 통합 테스트를 위한 테스트 호스트 픽스처입니다.
/// </summary>
public class TestHostFixture : IDisposable
{
    private readonly IHost _host;
    private bool _disposed;

    public TestHostFixture()
    {
        var builder = Host.CreateApplicationBuilder();

        // 테스트용 설정
        ConfigureTestConfiguration(builder.Configuration);

        // 테스트용 서비스
        ConfigureTestServices(builder.Services, builder.Configuration);

        // 테스트용 로깅
        ConfigureTestLogging(builder);

        _host = builder.Build();

        // LLM 시스템 초기화
        InitializeLLMSystem();
    }

    /// <summary>
    /// 서비스를 가져옵니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>서비스 인스턴스</returns>
    public T GetService<T>() where T : notnull
    {
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// 선택적 서비스를 가져옵니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>서비스 인스턴스 또는 null</returns>
    public T? GetOptionalService<T>() where T : class
    {
        return _host.Services.GetService<T>();
    }

    private void ConfigureTestConfiguration(IConfigurationBuilder configuration)
    {
        configuration.Sources.Clear();

        var testSettings = new Dictionary<string, string?>
        {
            ["Agent:Name"] = "Test AI Agent",
            ["Agent:Version"] = "0.1.0-test",
            ["Agent:DefaultTimeoutSeconds"] = "30",
            ["Agent:MaxConcurrentExecutions"] = "5",
            ["Agent:LLMProvider:DefaultProvider"] = "Mock",
            ["Agent:LLMProvider:Providers:Mock:Enabled"] = "true",
            ["Agent:LLMProvider:Providers:Mock:Model"] = "mock-gpt-4",
            ["Agent:Logging:EnableStructuredLogging"] = "true",
            ["Agent:Logging:LogLevel"] = "Debug",
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:LogLevel:AIAgent"] = "Debug"
        };

        configuration.AddInMemoryCollection(testSettings);
    }

    private void ConfigureTestServices(IServiceCollection services, IConfiguration configuration)
    {
        // 기본 서비스들
        services.Configure<AgentSettings>(configuration.GetSection("Agent"));
        services.AddSingleton<IConfigurationManager, AIAgent.Common.Configuration.ConfigurationManager>();
        services.AddSingleton<IStructuredLogger, StructuredLogger>();

        // LLM 시스템
        services.AddLLMSystem(configuration);

        // Mock 서비스들
        services.AddSingleton<ILLMProvider, MockLLMProvider>();
        services.AddSingleton<IPromptManager, MockPromptManager>();
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();

        // 테스트용 서비스들
        services.AddTransient<TestObjectValidator>();
    }

    private void ConfigureTestLogging(HostApplicationBuilder builder)
    {
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    private void InitializeLLMSystem()
    {
        var registry = _host.Services.InitializeLLMSystem();
        
        // 테스트용 검증
        if (registry.Count == 0)
        {
            throw new InvalidOperationException("No LLM functions were registered during initialization");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _host?.Dispose();
            _disposed = true;
        }
    }
}
```

### **Task 5.4: 문서화 및 샘플** (0.5시간)

#### **BasicUsageExample.cs 구현**
```csharp
namespace AIAgent.Host.Examples;

/// <summary>
/// AI Agent Framework의 기본 사용법을 보여주는 예제입니다.
/// </summary>
public class BasicUsageExample
{
    private readonly ILLMFunctionRegistry _registry;
    private readonly ILLMProvider _provider;
    private readonly ILogger<BasicUsageExample> _logger;

    public BasicUsageExample(
        ILLMFunctionRegistry registry,
        ILLMProvider provider,
        ILogger<BasicUsageExample> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 기본 사용법을 실행합니다.
    /// </summary>
    public async Task RunExampleAsync()
    {
        _logger.LogInformation("Starting Basic Usage Example");

        try
        {
            // 1. 등록된 LLM 기능들 확인
            await ShowRegisteredFunctionsAsync();

            // 2. 간단한 계획 수립 실행
            await ExecutePlanningExampleAsync();

            // 3. LLM Provider 상태 확인
            await CheckProviderHealthAsync();

            _logger.LogInformation("Basic Usage Example completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Basic Usage Example");
            throw;
        }
    }

    /// <summary>
    /// 등록된 LLM 기능들을 보여줍니다.
    /// </summary>
    private async Task ShowRegisteredFunctionsAsync()
    {
        _logger.LogInformation("=== Registered LLM Functions ===");

        var functions = _registry.GetAll();
        foreach (var function in functions)
        {
            _logger.LogInformation("Function: {Role} - {Description} (Priority: {Priority})",
                function.Role, function.Description, function.Priority);
        }

        _logger.LogInformation("Total functions registered: {Count}", _registry.Count);
    }

    /// <summary>
    /// 계획 수립 예제를 실행합니다.
    /// </summary>
    private async Task ExecutePlanningExampleAsync()
    {
        _logger.LogInformation("=== Planning Example ===");

        var plannerFunction = _registry.GetByRole("Planner");
        if (plannerFunction == null)
        {
            _logger.LogWarning("Planner function not found");
            return;
        }

        var context = CreateExampleContext("Create a plan to build a simple REST API for a todo application");

        var result = await plannerFunction.ExecuteAsync(context);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Planning successful:");
            _logger.LogInformation("Function: {FunctionType}", result.FunctionType);
            _logger.LogInformation("Executed at: {ExecutedAt}", result.ExecutedAt);
            
            if (result.Response is PlannerParsedResponse plannerResponse)
            {
                _logger.LogInformation("Plan ID: {PlanId}", plannerResponse.Plan.PlanId);
                _logger.LogInformation("Summary: {Summary}", plannerResponse.Plan.Summary);
                _logger.LogInformation("Steps: {StepCount}", plannerResponse.Plan.Steps?.Count ?? 0);
            }
        }
        else
        {
            _logger.LogWarning("Planning failed: {ErrorMessage}", result.ErrorMessage);
            foreach (var error in result.Errors ?? Enumerable.Empty<string>())
            {
                _logger.LogWarning("Error: {Error}", error);
            }
        }
    }

    /// <summary>
    /// Provider 상태를 확인합니다.
    /// </summary>
    private async Task CheckProviderHealthAsync()
    {
        _logger.LogInformation("=== Provider Health Check ===");

        var healthCheck = await _provider.CheckHealthAsync();

        _logger.LogInformation("Provider: {Name}", _provider.Name);
        _logger.LogInformation("Current Model: {Model}", _provider.CurrentModel);
        _logger.LogInformation("Is Healthy: {IsHealthy}", healthCheck.IsHealthy);
        _logger.LogInformation("Response Time: {ResponseTime}ms", healthCheck.ResponseTime.TotalMilliseconds);
        _logger.LogInformation("Last Checked: {LastChecked}", healthCheck.LastChecked);
    }

    /// <summary>
    /// 예제용 컨텍스트를 생성합니다.
    /// </summary>
    private ILLMContext CreateExampleContext(string userMessage)
    {
        var request = new AgentRequest
        {
            RequestId = Guid.NewGuid().ToString("N")[..12],
            UserMessage = userMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        return new LLMContext
        {
            Request = request,
            LLMProvider = _provider,
            CorrelationId = LogCorrelation.GenerateCorrelationId(),
            Variables = new Dictionary<string, object>
            {
                ["example_mode"] = true,
                ["user_type"] = "demo"
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

## 🔍 검증 기준

### **필수 통과 조건**

#### **1. DI 컨테이너 완성**
- [ ] 모든 서비스가 DI 컨테이너에서 정상 해석
- [ ] 순환 의존성 없음
- [ ] 서비스 생명주기 적절히 구성 (Singleton/Transient/Scoped)
- [ ] 설정 바인딩이 정상 동작

#### **2. Host 애플리케이션 동작**
- [ ] 애플리케이션이 에러 없이 시작
- [ ] LLM 시스템 자동 초기화 성공
- [ ] 로깅이 모든 레벨에서 정상 동작
- [ ] 설정 파일 로드 및 바인딩 성공

#### **3. 통합 테스트 완성**
- [ ] 모든 통합 테스트가 통과
- [ ] End-to-End 시나리오 테스트 성공
- [ ] 예외 상황 처리 테스트 통과
- [ ] 성능 기준선 측정 완료

#### **4. Mock 구현체 동작**
- [ ] MockLLMProvider가 다양한 요청에 적절히 응답
- [ ] MockPromptManager가 모든 프롬프트 제공
- [ ] 실제 API 호출 없이 전체 시스템 동작
- [ ] 개발 환경에서 완전 자급자족

## 📝 완료 체크리스트

### **Host 애플리케이션**
- [ ] Program.cs 완전 구현
- [ ] ServiceConfiguration 완성
- [ ] appsettings.json 구성 완료
- [ ] 백그라운드 서비스 구현 (선택사항)

### **Mock 구현체들**
- [ ] MockLLMProvider 완전 구현
- [ ] MockPromptManager 완전 구현
- [ ] 다양한 시나리오 지원
- [ ] 개발용 헬스체크 구현

### **통합 테스트**
- [ ] InfrastructureTests 완성
- [ ] LLMFunctionTests 완성
- [ ] DIContainerTests 완성
- [ ] TestHostFixture 구현

### **문서화 및 샘플**
- [ ] BasicUsageExample 완성
- [ ] PlannerFunctionExample 구현
- [ ] README 업데이트
- [ ] 개발자 가이드 작성

### **성능 및 품질**
- [ ] 메모리 누수 검증 완료
- [ ] 성능 기준선 측정
- [ ] 코드 커버리지 80% 이상
- [ ] 모든 정적 분석 통과

## 🎯 성공 지표

완료 시 다음이 모두 달성되어야 함:

1. ✅ **완전 동작하는 시스템**: Host 애플리케이션이 에러 없이 실행
2. ✅ **포괄적인 테스트**: 모든 주요 시나리오가 테스트로 검증됨
3. ✅ **개발자 친화적**: Mock 구현체로 실제 API 없이도 개발 가능
4. ✅ **확장 준비 완료**: 다음 단계(Phase 2) 진행을 위한 견고한 기반

---

## 🎉 Phase 1 완성!

이 5단계를 모두 완료하면 **AI Agent Framework의 견고한 기초 인프라**가 완성됩니다:

- ✅ 완전한 프로젝트 구조와 개발 환경
- ✅ 타입 안전한 인터페이스와 모델  
- ✅ 재사용 가능한 공통 인프라
- ✅ 확장 가능한 LLM Function 아키텍처
- ✅ 완전 통합된 DI 시스템

**다음 단계**: Phase 2 - LLM Provider 구현 및 Tool 시스템 구축