---
inclusion: always
---

# .NET 코딩 표준 및 SOLID 원칙 가이드

## 코딩 컨벤션

### 네이밍 규칙

#### 클래스, 인터페이스, 메서드, 프로퍼티
```csharp
// ✅ 올바른 예시
public class OrchestrationEngine : IOrchestrationEngine
{
    public string SessionId { get; set; }
    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request)
    {
        // 구현
    }
}

// ❌ 잘못된 예시
public class orchestrationEngine : IorchestrationEngine
{
    public string sessionId { get; set; }
    public async Task<IOrchestrationResult> executeAsync(IUserRequest request)
    {
        // 구현
    }
}
```

#### 필드, 변수, 매개변수
```csharp
// ✅ 올바른 예시
public class LLMProvider
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = CreateRequestBody(prompt);
        // 구현
    }
}

// ❌ 잘못된 예시
public class LLMProvider
{
    private readonly IConfiguration Configuration;
    private readonly HttpClient HttpClient;
    
    public async Task<string> GenerateAsync(string Prompt, CancellationToken CancellationToken = default)
    {
        var RequestBody = CreateRequestBody(Prompt);
        // 구현
    }
}
```

#### 상수 및 열거형
```csharp
// ✅ 올바른 예시
public static class LLMConstants
{
    public const string DEFAULT_MODEL = "gpt-4";
    public const int MAX_RETRY_COUNT = 3;
}

public enum ExecutionStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

// ❌ 잘못된 예시
public static class LLMConstants
{
    public const string default_model = "gpt-4";
    public const int max_retry_count = 3;
}

public enum ExecutionStatus
{
    not_started,
    in_progress,
    completed,
    failed
}
```

### 파일 및 네임스페이스 구조
```csharp
// 파일: AIAgentFramework.Core/Orchestration/IOrchestrationEngine.cs
namespace AIAgentFramework.Core.Orchestration
{
    public interface IOrchestrationEngine
    {
        Task<IOrchestrationResult> ExecuteAsync(IUserRequest request);
    }
}

// 파일: AIAgentFramework.Core/Orchestration/OrchestrationEngine.cs
namespace AIAgentFramework.Core.Orchestration
{
    public class OrchestrationEngine : IOrchestrationEngine
    {
        // 구현
    }
}
```

### 코드 포맷팅
```csharp
// ✅ 올바른 예시
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RegisterTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (string.IsNullOrWhiteSpace(tool.Name))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(tool));

        _tools[tool.Name] = tool;
        _logger.LogInformation("Tool registered: {ToolName}", tool.Name);
    }

    public ITool GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) 
            ? tool 
            : throw new ToolNotFoundException($"Tool '{name}' not found");
    }
}
```

## SOLID 원칙 적용

### 1. Single Responsibility Principle (SRP)
각 클래스는 하나의 책임만 가져야 합니다.

```csharp
// ✅ 올바른 예시 - 각각 단일 책임
public class PromptLoader
{
    public async Task<string> LoadPromptAsync(string promptName)
    {
        // 프롬프트 파일 로드만 담당
    }
}

public class PromptProcessor
{
    public string ProcessTemplate(string template, Dictionary<string, object> parameters)
    {
        // 프롬프트 템플릿 처리만 담당
    }
}

public class PromptCache
{
    public void CachePrompt(string key, string prompt, TimeSpan ttl)
    {
        // 프롬프트 캐싱만 담당
    }
}

// ❌ 잘못된 예시 - 여러 책임을 가짐
public class PromptManager
{
    public async Task<string> LoadPromptAsync(string promptName) { /* 파일 로드 */ }
    public string ProcessTemplate(string template, Dictionary<string, object> parameters) { /* 템플릿 처리 */ }
    public void CachePrompt(string key, string prompt, TimeSpan ttl) { /* 캐싱 */ }
    public void LogPromptUsage(string promptName) { /* 로깅 */ }
    public void ValidatePrompt(string prompt) { /* 검증 */ }
}
```

### 2. Open/Closed Principle (OCP)
확장에는 열려있고 수정에는 닫혀있어야 합니다.

```csharp
// ✅ 올바른 예시 - 새로운 LLM Provider 추가 시 기존 코드 수정 불필요
public abstract class LLMProviderBase : ILLMProvider
{
    protected readonly IConfiguration _configuration;
    protected readonly ILogger _logger;

    protected LLMProviderBase(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public abstract Task<string> GenerateAsync(string prompt);
    
    protected virtual void LogRequest(string prompt)
    {
        _logger.LogDebug("LLM request: {Prompt}", prompt);
    }
}

public class OpenAIProvider : LLMProviderBase
{
    public OpenAIProvider(IConfiguration configuration, ILogger<OpenAIProvider> logger) 
        : base(configuration, logger) { }

    public override async Task<string> GenerateAsync(string prompt)
    {
        LogRequest(prompt);
        // OpenAI 특화 구현
        return await CallOpenAIAsync(prompt);
    }
}

public class ClaudeProvider : LLMProviderBase
{
    public ClaudeProvider(IConfiguration configuration, ILogger<ClaudeProvider> logger) 
        : base(configuration, logger) { }

    public override async Task<string> GenerateAsync(string prompt)
    {
        LogRequest(prompt);
        // Claude 특화 구현
        return await CallClaudeAsync(prompt);
    }
}
```

### 3. Liskov Substitution Principle (LSP)
파생 클래스는 기본 클래스를 대체할 수 있어야 합니다.

```csharp
// ✅ 올바른 예시 - 모든 구현체가 동일한 계약을 준수
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<IToolResult> ExecuteAsync(IToolInput input);
}

public class WebSearchTool : ITool
{
    public string Name => "web_search";
    public string Description => "웹 검색 도구";

    public async Task<IToolResult> ExecuteAsync(IToolInput input)
    {
        // 항상 IToolResult를 반환하고 예외 상황도 적절히 처리
        try
        {
            var result = await PerformWebSearchAsync(input);
            return new ToolResult { IsSuccess = true, Data = result };
        }
        catch (Exception ex)
        {
            return new ToolResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}

public class DatabaseTool : ITool
{
    public string Name => "database_query";
    public string Description => "데이터베이스 쿼리 도구";

    public async Task<IToolResult> ExecuteAsync(IToolInput input)
    {
        // 동일한 계약을 준수
        try
        {
            var result = await ExecuteQueryAsync(input);
            return new ToolResult { IsSuccess = true, Data = result };
        }
        catch (Exception ex)
        {
            return new ToolResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
```

### 4. Interface Segregation Principle (ISP)
클라이언트는 사용하지 않는 인터페이스에 의존하지 않아야 합니다.

```csharp
// ✅ 올바른 예시 - 인터페이스 분리
public interface IToolExecutor
{
    Task<IToolResult> ExecuteAsync(IToolInput input);
}

public interface IToolMetadata
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
}

public interface IToolValidator
{
    bool ValidateInput(IToolInput input);
    bool ValidateContract(IToolContract contract);
}

// 클라이언트는 필요한 인터페이스만 의존
public class ToolExecutionService
{
    private readonly IToolExecutor _executor;
    
    public ToolExecutionService(IToolExecutor executor)
    {
        _executor = executor; // 실행 기능만 필요
    }
}

public class ToolRegistryService
{
    private readonly IToolMetadata _metadata;
    
    public ToolRegistryService(IToolMetadata metadata)
    {
        _metadata = metadata; // 메타데이터만 필요
    }
}

// ❌ 잘못된 예시 - 하나의 큰 인터페이스
public interface ITool
{
    // 실행 관련
    Task<IToolResult> ExecuteAsync(IToolInput input);
    
    // 메타데이터 관련
    string Name { get; }
    string Description { get; }
    string Version { get; }
    
    // 검증 관련
    bool ValidateInput(IToolInput input);
    bool ValidateContract(IToolContract contract);
    
    // 캐싱 관련
    void CacheResult(string key, IToolResult result);
    IToolResult GetCachedResult(string key);
    
    // 로깅 관련
    void LogExecution(IToolInput input, IToolResult result);
}
```

### 5. Dependency Inversion Principle (DIP)
고수준 모듈은 저수준 모듈에 의존하지 않아야 하며, 둘 다 추상화에 의존해야 합니다.

```csharp
// ✅ 올바른 예시 - 추상화에 의존
public class OrchestrationEngine : IOrchestrationEngine
{
    private readonly ILLMSystem _llmSystem;
    private readonly IToolSystem _toolSystem;
    private readonly IRegistry _registry;
    private readonly ILogger<OrchestrationEngine> _logger;

    public OrchestrationEngine(
        ILLMSystem llmSystem,
        IToolSystem toolSystem,
        IRegistry registry,
        ILogger<OrchestrationEngine> logger)
    {
        _llmSystem = llmSystem ?? throw new ArgumentNullException(nameof(llmSystem));
        _toolSystem = toolSystem ?? throw new ArgumentNullException(nameof(toolSystem));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request)
    {
        // 추상화된 인터페이스를 통해 작업 수행
        var planResult = await _llmSystem.ExecutePlannerAsync(request);
        // ...
    }
}

// ❌ 잘못된 예시 - 구체 클래스에 직접 의존
public class OrchestrationEngine : IOrchestrationEngine
{
    private readonly OpenAIProvider _openAIProvider; // 구체 클래스에 의존
    private readonly SqlServerToolRepository _toolRepository; // 구체 클래스에 의존
    private readonly FileSystemLogger _logger; // 구체 클래스에 의존

    public OrchestrationEngine()
    {
        _openAIProvider = new OpenAIProvider(); // 직접 생성
        _toolRepository = new SqlServerToolRepository(); // 직접 생성
        _logger = new FileSystemLogger(); // 직접 생성
    }
}
```

## 의존성 주입 설정

```csharp
// Program.cs 또는 Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // 인터페이스와 구현체 등록
    services.AddScoped<IOrchestrationEngine, OrchestrationEngine>();
    services.AddScoped<ILLMSystem, LLMSystem>();
    services.AddScoped<IToolSystem, ToolSystem>();
    services.AddSingleton<IRegistry, Registry>();
    
    // Factory 패턴 적용
    services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
    
    // 설정 기반 등록
    services.Configure<LLMConfiguration>(Configuration.GetSection("LLM"));
    services.Configure<ToolConfiguration>(Configuration.GetSection("Tools"));
    
    // 로깅
    services.AddLogging(builder => builder.AddConsole().AddDebug());
}
```

## 예외 처리 및 로깅

```csharp
public class LLMFunction : ILLMFunction
{
    private readonly ILLMProvider _provider;
    private readonly ILogger<LLMFunction> _logger;

    public LLMFunction(ILLMProvider provider, ILogger<LLMFunction> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ILLMResult> ExecuteAsync(ILLMContext context)
    {
        try
        {
            _logger.LogInformation("Executing LLM function: {Role}", Role);
            
            var result = await _provider.GenerateAsync(context.Prompt);
            
            _logger.LogInformation("LLM function completed successfully: {Role}", Role);
            return new LLMResult { IsSuccess = true, Data = result };
        }
        catch (LLMException ex)
        {
            _logger.LogError(ex, "LLM specific error in function: {Role}", Role);
            throw; // 특정 예외는 다시 던져서 상위에서 처리
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in LLM function: {Role}", Role);
            return new LLMResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
```

## 비동기 프로그래밍 가이드

```csharp
// ✅ 올바른 비동기 패턴
public class ToolSystem : IToolSystem
{
    public async Task<IToolResult> ExecuteToolAsync(string toolName, IToolInput input, CancellationToken cancellationToken = default)
    {
        var tool = await GetToolAsync(toolName, cancellationToken);
        return await tool.ExecuteAsync(input);
    }

    public async Task<List<IToolResult>> ExecuteToolsAsync(IEnumerable<(string toolName, IToolInput input)> tools, CancellationToken cancellationToken = default)
    {
        var tasks = tools.Select(async t => await ExecuteToolAsync(t.toolName, t.input, cancellationToken));
        return (await Task.WhenAll(tasks)).ToList();
    }
}

// ❌ 잘못된 비동기 패턴
public class ToolSystem : IToolSystem
{
    public IToolResult ExecuteTool(string toolName, IToolInput input) // 동기 메서드
    {
        var tool = GetToolAsync(toolName).Result; // .Result 사용 (데드락 위험)
        return tool.ExecuteAsync(input).GetAwaiter().GetResult(); // GetAwaiter().GetResult() 사용
    }
}
```

이 가이드를 따라 모든 코드를 작성하여 일관성 있고 유지보수 가능한 .NET 애플리케이션을 구축하세요.