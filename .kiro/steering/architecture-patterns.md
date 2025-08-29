---
inclusion: always
---

# 아키텍처 패턴 및 설계 원칙 가이드

## 적용할 주요 디자인 패턴

### 1. Factory Pattern
LLM Provider와 Tool 생성에 사용

```csharp
public interface ILLMProviderFactory
{
    ILLMProvider CreateProvider(string providerType);
}

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public LLMProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public ILLMProvider CreateProvider(string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "openai" => _serviceProvider.GetRequiredService<OpenAIProvider>(),
            "claude" => _serviceProvider.GetRequiredService<ClaudeProvider>(),
            "local" => _serviceProvider.GetRequiredService<LocalLLMProvider>(),
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };
    }
}
```

### 2. Strategy Pattern
다양한 LLM 모델과 프롬프트 전략 선택

```csharp
public interface ILLMStrategy
{
    Task<string> GenerateAsync(string prompt, LLMParameters parameters);
    bool CanHandle(string modelType);
}

public class GPTStrategy : ILLMStrategy
{
    public async Task<string> GenerateAsync(string prompt, LLMParameters parameters)
    {
        // GPT 특화 로직
        return await CallGPTAsync(prompt, parameters);
    }

    public bool CanHandle(string modelType) => modelType.StartsWith("gpt");
}

public class LLMContext
{
    private readonly List<ILLMStrategy> _strategies;

    public LLMContext(IEnumerable<ILLMStrategy> strategies)
    {
        _strategies = strategies.ToList();
    }

    public async Task<string> ExecuteAsync(string modelType, string prompt, LLMParameters parameters)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(modelType))
            ?? throw new NotSupportedException($"No strategy found for model type: {modelType}");

        return await strategy.GenerateAsync(prompt, parameters);
    }
}
```

### 3. Command Pattern
LLM 기능과 Tool 실행을 명령 객체로 캡슐화

```csharp
public interface ICommand
{
    Task<IExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default);
    string CommandId { get; }
    string CommandType { get; }
}

public class LLMFunctionCommand : ICommand
{
    private readonly ILLMFunction _function;
    private readonly ILLMContext _context;

    public string CommandId { get; } = Guid.NewGuid().ToString();
    public string CommandType => "LLM_FUNCTION";

    public LLMFunctionCommand(ILLMFunction function, ILLMContext context)
    {
        _function = function;
        _context = context;
    }

    public async Task<IExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _function.ExecuteAsync(_context);
        return new ExecutionResult
        {
            CommandId = CommandId,
            IsSuccess = result.IsSuccess,
            Data = result.Data,
            ExecutedAt = DateTime.UtcNow
        };
    }
}

public class ToolCommand : ICommand
{
    private readonly ITool _tool;
    private readonly IToolInput _input;

    public string CommandId { get; } = Guid.NewGuid().ToString();
    public string CommandType => "TOOL";

    public ToolCommand(ITool tool, IToolInput input)
    {
        _tool = tool;
        _input = input;
    }

    public async Task<IExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _tool.ExecuteAsync(_input);
        return new ExecutionResult
        {
            CommandId = CommandId,
            IsSuccess = result.IsSuccess,
            Data = result.Data,
            ExecutedAt = DateTime.UtcNow
        };
    }
}
```

### 4. Chain of Responsibility Pattern
오케스트레이션의 [계획-실행] 연쇄 구조

```csharp
public abstract class OrchestrationHandler
{
    protected OrchestrationHandler _nextHandler;

    public OrchestrationHandler SetNext(OrchestrationHandler handler)
    {
        _nextHandler = handler;
        return handler;
    }

    public virtual async Task<IOrchestrationResult> HandleAsync(IOrchestrationContext context)
    {
        if (_nextHandler != null)
        {
            return await _nextHandler.HandleAsync(context);
        }

        return new OrchestrationResult { IsSuccess = true, IsCompleted = true };
    }
}

public class PlanningHandler : OrchestrationHandler
{
    private readonly ILLMSystem _llmSystem;

    public PlanningHandler(ILLMSystem llmSystem)
    {
        _llmSystem = llmSystem;
    }

    public override async Task<IOrchestrationResult> HandleAsync(IOrchestrationContext context)
    {
        // 계획 수립
        var planResult = await _llmSystem.ExecutePlannerAsync(context);
        context.CurrentPlan = planResult;

        // 다음 핸들러로 전달
        return await base.HandleAsync(context);
    }
}

public class ExecutionHandler : OrchestrationHandler
{
    private readonly IToolSystem _toolSystem;
    private readonly ILLMSystem _llmSystem;

    public ExecutionHandler(IToolSystem toolSystem, ILLMSystem llmSystem)
    {
        _toolSystem = toolSystem;
        _llmSystem = llmSystem;
    }

    public override async Task<IOrchestrationResult> HandleAsync(IOrchestrationContext context)
    {
        // 계획된 작업 실행
        foreach (var action in context.CurrentPlan.Actions)
        {
            if (action.Type == "LLM")
            {
                var result = await _llmSystem.ExecuteFunctionAsync(action);
                context.AddExecutionStep(result);
            }
            else if (action.Type == "TOOL")
            {
                var result = await _toolSystem.ExecuteToolAsync(action);
                context.AddExecutionStep(result);
            }
        }

        // 완료 확인 또는 다음 핸들러로 전달
        if (context.IsCompleted)
        {
            return new OrchestrationResult { IsSuccess = true, IsCompleted = true };
        }

        return await base.HandleAsync(context);
    }
}
```

### 5. Observer Pattern
실행 상태 모니터링 및 이벤트 처리

```csharp
public interface IOrchestrationObserver
{
    Task OnExecutionStartedAsync(IOrchestrationContext context);
    Task OnStepCompletedAsync(IExecutionStep step);
    Task OnExecutionCompletedAsync(IOrchestrationResult result);
    Task OnErrorOccurredAsync(Exception exception, IOrchestrationContext context);
}

public class OrchestrationSubject
{
    private readonly List<IOrchestrationObserver> _observers = new();

    public void Subscribe(IOrchestrationObserver observer)
    {
        _observers.Add(observer);
    }

    public void Unsubscribe(IOrchestrationObserver observer)
    {
        _observers.Remove(observer);
    }

    protected async Task NotifyExecutionStartedAsync(IOrchestrationContext context)
    {
        foreach (var observer in _observers)
        {
            await observer.OnExecutionStartedAsync(context);
        }
    }

    protected async Task NotifyStepCompletedAsync(IExecutionStep step)
    {
        foreach (var observer in _observers)
        {
            await observer.OnStepCompletedAsync(step);
        }
    }
}

public class LoggingObserver : IOrchestrationObserver
{
    private readonly ILogger<LoggingObserver> _logger;

    public LoggingObserver(ILogger<LoggingObserver> logger)
    {
        _logger = logger;
    }

    public Task OnExecutionStartedAsync(IOrchestrationContext context)
    {
        _logger.LogInformation("Orchestration started: {SessionId}", context.SessionId);
        return Task.CompletedTask;
    }

    public Task OnStepCompletedAsync(IExecutionStep step)
    {
        _logger.LogInformation("Step completed: {StepId} - {StepType} - {Duration}ms", 
            step.StepId, step.StepType, step.Duration.TotalMilliseconds);
        return Task.CompletedTask;
    }

    public Task OnExecutionCompletedAsync(IOrchestrationResult result)
    {
        _logger.LogInformation("Orchestration completed: {SessionId} - Success: {IsSuccess}", 
            result.SessionId, result.IsSuccess);
        return Task.CompletedTask;
    }

    public Task OnErrorOccurredAsync(Exception exception, IOrchestrationContext context)
    {
        _logger.LogError(exception, "Error occurred in orchestration: {SessionId}", context.SessionId);
        return Task.CompletedTask;
    }
}
```

### 6. Adapter Pattern
다양한 LLM API를 통일된 인터페이스로 추상화

```csharp
public class OpenAIAdapter : ILLMProvider
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIAdapter> _logger;

    public OpenAIAdapter(OpenAIClient client, ILogger<OpenAIAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        try
        {
            var request = new ChatCompletionsOptions
            {
                Messages = { new ChatMessage(ChatRole.User, prompt) },
                MaxTokens = 1000
            };

            var response = await _client.GetChatCompletionsAsync("gpt-4", request);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed");
            throw new LLMException("OpenAI API call failed", ex);
        }
    }
}

public class ClaudeAdapter : ILLMProvider
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeAdapter> _logger;

    public ClaudeAdapter(AnthropicClient client, ILogger<ClaudeAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        try
        {
            var request = new MessageRequest
            {
                Model = "claude-3-sonnet-20240229",
                MaxTokens = 1000,
                Messages = new[] { new Message { Role = "user", Content = prompt } }
            };

            var response = await _client.CreateMessageAsync(request);
            return response.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed");
            throw new LLMException("Claude API call failed", ex);
        }
    }
}
```

### 7. Template Method Pattern
프롬프트 구조의 공통 골격과 가변 부분 분리

```csharp
public abstract class LLMFunctionBase : ILLMFunction
{
    protected readonly IPromptManager _promptManager;
    protected readonly ILLMProvider _provider;
    protected readonly ILogger _logger;

    public abstract string Role { get; }
    public abstract string Description { get; }

    protected LLMFunctionBase(IPromptManager promptManager, ILLMProvider provider, ILogger logger)
    {
        _promptManager = promptManager;
        _provider = provider;
        _logger = logger;
    }

    // Template Method
    public async Task<ILLMResult> ExecuteAsync(ILLMContext context)
    {
        try
        {
            // 1. 전처리 (공통)
            await PreProcessAsync(context);

            // 2. 프롬프트 준비 (가변)
            var prompt = await PreparePromptAsync(context);

            // 3. LLM 호출 (공통)
            var response = await _provider.GenerateAsync(prompt);

            // 4. 응답 처리 (가변)
            var result = await ProcessResponseAsync(response, context);

            // 5. 후처리 (공통)
            await PostProcessAsync(result, context);

            return result;
        }
        catch (Exception ex)
        {
            return await HandleErrorAsync(ex, context);
        }
    }

    // 공통 메서드
    protected virtual async Task PreProcessAsync(ILLMContext context)
    {
        _logger.LogDebug("Starting LLM function: {Role}", Role);
    }

    protected virtual async Task PostProcessAsync(ILLMResult result, ILLMContext context)
    {
        _logger.LogDebug("Completed LLM function: {Role}, Success: {IsSuccess}", Role, result.IsSuccess);
    }

    protected virtual async Task<ILLMResult> HandleErrorAsync(Exception ex, ILLMContext context)
    {
        _logger.LogError(ex, "Error in LLM function: {Role}", Role);
        return new LLMResult { IsSuccess = false, ErrorMessage = ex.Message };
    }

    // 추상 메서드 (하위 클래스에서 구현)
    protected abstract Task<string> PreparePromptAsync(ILLMContext context);
    protected abstract Task<ILLMResult> ProcessResponseAsync(string response, ILLMContext context);
}

// 구체적인 구현
public class PlannerFunction : LLMFunctionBase
{
    public override string Role => "planner";
    public override string Description => "사용자 요구 분석 및 실행 계획 수립";

    public PlannerFunction(IPromptManager promptManager, ILLMProvider provider, ILogger<PlannerFunction> logger)
        : base(promptManager, provider, logger) { }

    protected override async Task<string> PreparePromptAsync(ILLMContext context)
    {
        var parameters = new Dictionary<string, object>
        {
            ["user_request"] = context.Parameters["user_request"],
            ["available_tools"] = context.Parameters["available_tools"],
            ["context_history"] = context.Parameters["context_history"]
        };

        return await _promptManager.LoadPromptAsync(Role, parameters);
    }

    protected override async Task<ILLMResult> ProcessResponseAsync(string response, ILLMContext context)
    {
        try
        {
            var planData = JsonConvert.DeserializeObject<PlanResponse>(response);
            return new LLMResult
            {
                IsSuccess = true,
                Data = planData,
                IsCompleted = planData.IsCompleted,
                NextActions = planData.Actions
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse planner response");
            return new LLMResult { IsSuccess = false, ErrorMessage = "Invalid JSON response" };
        }
    }
}
```

## 레이어드 아키텍처 구조

```
AIAgentFramework/
├── AIAgentFramework.Core/              # 핵심 인터페이스 및 모델
│   ├── Interfaces/
│   ├── Models/
│   └── Exceptions/
├── AIAgentFramework.LLM/               # LLM 시스템 구현
│   ├── Functions/
│   ├── Providers/
│   └── Prompts/
├── AIAgentFramework.Tools/             # 도구 시스템 구현
│   ├── BuiltIn/
│   ├── PlugIn/
│   └── MCP/
├── AIAgentFramework.Registry/          # 레지스트리 시스템
├── AIAgentFramework.Configuration/     # 설정 관리
├── AIAgentFramework.Web/              # Web API
├── AIAgentFramework.Console/          # Console App
└── AIAgentFramework.Tests/            # 테스트
```

## 의존성 방향 규칙

1. **상위 레이어는 하위 레이어에 의존할 수 있음**
2. **하위 레이어는 상위 레이어에 의존하면 안됨**
3. **모든 레이어는 Core에 의존할 수 있음**
4. **구체 구현은 인터페이스에 의존해야 함**

```csharp
// ✅ 올바른 의존성 방향
namespace AIAgentFramework.Web.Controllers
{
    public class OrchestrationController : ControllerBase
    {
        private readonly IOrchestrationEngine _engine; // Core 인터페이스에 의존

        public OrchestrationController(IOrchestrationEngine engine)
        {
            _engine = engine;
        }
    }
}

// ❌ 잘못된 의존성 방향
namespace AIAgentFramework.Core.Interfaces
{
    public interface IOrchestrationEngine
    {
        // Web 레이어의 클래스를 참조하면 안됨
        Task<ActionResult> ExecuteAsync(HttpRequest request); // ❌
    }
}
```

이러한 아키텍처 패턴과 설계 원칙을 따라 확장 가능하고 유지보수 가능한 AI 에이전트 프레임워크를 구축하세요.