# Plan 4: BaseLLMFunction 추상 클래스 설계

## 📋 개요

**목표**: 14가지 LLM 기능의 공통 기반 완성  
**예상 소요 시간**: 1일 (8시간)  
**의존성**: Plan 3 (공통 인프라) 완료

## 🎯 구체적 목표

1. ✅ **Template Method 패턴** 완벽 구현
2. ✅ **LLM Function Registry** 시스템 완성
3. ✅ **첫 번째 구체 구현** (PlannerFunction) 완성
4. ✅ **우아한 확장성** 확보 (No Switch-Case)

## 🏗️ 작업 단계

### **Task 4.1: BaseLLMFunction 설계** (3시간)

#### **AIAgent.LLM 프로젝트 생성**
```bash
# LLM 프로젝트 생성
dotnet new classlib -n AIAgent.LLM -o src/AIAgent.LLM --framework net8.0
dotnet sln add src/AIAgent.LLM

# 테스트 프로젝트 생성
dotnet new xunit -n AIAgent.LLM.Tests -o tests/AIAgent.LLM.Tests --framework net8.0
dotnet sln add tests/AIAgent.LLM.Tests

# 프로젝트 참조 설정
dotnet add src/AIAgent.LLM reference src/AIAgent.Core
dotnet add src/AIAgent.LLM reference src/AIAgent.Common
dotnet add tests/AIAgent.LLM.Tests reference src/AIAgent.LLM
```

#### **AIAgent.LLM 프로젝트 구조**
```
src/AIAgent.LLM/
├── Functions/                     # LLM 기능 구현
│   ├── Base/
│   │   ├── BaseLLMFunction.cs     # 기본 추상 클래스
│   │   ├── LLMFunctionAttribute.cs # 자동 발견용 Attribute
│   │   └── LLMFunctionMetadata.cs # 메타데이터 클래스
│   ├── Planning/
│   │   └── PlannerFunction.cs     # 첫 번째 구체 구현
│   └── (추후 확장...)
├── Registry/
│   ├── ILLMFunctionRegistry.cs    # Registry 인터페이스
│   ├── LLMFunctionRegistry.cs     # Registry 구현
│   └── FunctionDiscoveryService.cs # 자동 발견 서비스
├── Providers/                     # LLM Provider (향후)
└── Parsers/                       # 응답 파싱 (향후)
```

#### **BaseLLMFunction.cs 구현**
```csharp
namespace AIAgent.LLM.Functions.Base;

/// <summary>
/// 모든 LLM 기능의 기본 추상 클래스입니다.
/// Template Method 패턴을 구현합니다.
/// </summary>
public abstract class BaseLLMFunction : ILLMFunction
{
    private readonly ILogger<BaseLLMFunction> _logger;
    private readonly IStructuredLogger _structuredLogger;
    
    protected BaseLLMFunction(ILogger<BaseLLMFunction> logger, IStructuredLogger structuredLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _structuredLogger = structuredLogger ?? throw new ArgumentNullException(nameof(structuredLogger));
    }
    
    #region ILLMFunction Implementation
    
    /// <summary>
    /// LLM 기능의 역할을 나타냅니다.
    /// </summary>
    public abstract string Role { get; }
    
    /// <summary>
    /// LLM 기능에 대한 설명입니다.
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// 실행 우선순위를 나타냅니다.
    /// </summary>
    public virtual int Priority => 100;
    
    /// <summary>
    /// 지원하는 입력 유형을 나타냅니다.
    /// </summary>
    public abstract IEnumerable<Type> SupportedInputTypes { get; }
    
    #endregion
    
    #region Template Method Pattern
    
    /// <summary>
    /// LLM 기능을 실행합니다. (Template Method)
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    public async Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default)
    {
        using var correlationScope = LogCorrelation.SetCorrelationId(context.CorrelationId ?? LogCorrelation.GenerateCorrelationId());
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _structuredLogger.LogInfo(LogEventIds.LLMFunctionStart, 
                $"Starting {Role} function execution", 
                new { Role, FunctionType = GetType().Name });
            
            // 1. 사전 검증
            var validationResult = await ValidateInputAsync(context, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return CreateErrorResult("Input validation failed", validationResult.Errors);
            }
            
            // 2. 실행 가능성 확인
            if (!CanExecute(context))
            {
                return CreateErrorResult("Function cannot execute with current context", 
                    new[] { "Execution conditions not met" });
            }
            
            // 3. 프롬프트 준비
            var prompt = await PreparePromptAsync(context, cancellationToken);
            if (prompt == null)
            {
                return CreateErrorResult("Failed to prepare prompt", 
                    new[] { "Prompt preparation returned null" });
            }
            
            // 4. LLM 호출
            var llmResponse = await CallLLMAsync(prompt, context, cancellationToken);
            if (string.IsNullOrEmpty(llmResponse))
            {
                return CreateErrorResult("Empty response from LLM provider", 
                    new[] { "LLM provider returned empty or null response" });
            }
            
            // 5. 응답 파싱
            var parsedResponse = await ParseResponseAsync(llmResponse, context, cancellationToken);
            if (parsedResponse == null)
            {
                return CreateErrorResult("Failed to parse LLM response", 
                    new[] { "Response parsing returned null" });
            }
            
            // 6. 응답 검증
            var responseValidation = await ValidateResponseAsync(parsedResponse, context, cancellationToken);
            if (!responseValidation.IsSuccess)
            {
                return CreateErrorResult("Response validation failed", responseValidation.Errors);
            }
            
            // 7. 후처리
            var finalResult = await PostProcessAsync(parsedResponse, context, cancellationToken);
            
            stopwatch.Stop();
            
            _structuredLogger.LogPerformance($"{Role}_execution", stopwatch.Elapsed,
                new { Role, Success = true, Duration = stopwatch.ElapsedMilliseconds });
            
            return finalResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("LLM function execution was cancelled: {Role}", Role);
            return CreateErrorResult("Operation was cancelled", new[] { "Execution was cancelled by user request" });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _structuredLogger.LogError(LogEventIds.LLMFunctionError, ex, 
                $"Error executing {Role} function",
                new { Role, Duration = stopwatch.ElapsedMilliseconds, Exception = ex.GetType().Name });
            
            return CreateErrorResult($"Unexpected error in {Role} function: {ex.Message}", 
                new[] { ex.Message });
        }
    }
    
    #endregion
    
    #region Abstract Methods (Extension Points)
    
    /// <summary>
    /// 프롬프트를 준비합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>준비된 프롬프트</returns>
    protected abstract Task<LLMPrompt?> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken);
    
    /// <summary>
    /// LLM 응답을 파싱합니다.
    /// </summary>
    /// <param name="llmResponse">LLM 원시 응답</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>파싱된 응답</returns>
    protected abstract Task<IParsedResponse?> ParseResponseAsync(string llmResponse, ILLMContext context, CancellationToken cancellationToken);
    
    #endregion
    
    #region Virtual Methods (Override 가능한 확장점)
    
    /// <summary>
    /// 입력 컨텍스트의 유효성을 검사합니다.
    /// </summary>
    /// <param name="context">검증할 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    protected virtual Task<ValidationResult> ValidateInputAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        
        if (context == null)
            errors.Add("Context is null");
        
        if (context?.Request == null)
            errors.Add("Request is null");
        
        if (string.IsNullOrWhiteSpace(context?.Request?.UserMessage))
            errors.Add("User message is empty");
        
        return Task.FromResult(errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors));
    }
    
    /// <summary>
    /// 주어진 컨텍스트에서 이 기능이 실행 가능한지 확인합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>실행 가능 여부</returns>
    public virtual bool CanExecute(ILLMContext context)
    {
        return context?.Request != null && 
               !string.IsNullOrWhiteSpace(context.Request.UserMessage);
    }
    
    /// <summary>
    /// 전체 실행에 대한 유효성을 검사합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>검증 결과</returns>
    public virtual Task<ValidationResult> ValidateAsync(ILLMContext context)
    {
        return ValidateInputAsync(context, default);
    }
    
    /// <summary>
    /// 파싱된 응답의 유효성을 검사합니다.
    /// </summary>
    /// <param name="parsedResponse">파싱된 응답</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    protected virtual Task<ValidationResult> ValidateResponseAsync(IParsedResponse parsedResponse, ILLMContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(parsedResponse != null 
            ? ValidationResult.Success() 
            : ValidationResult.Failure("Parsed response is null"));
    }
    
    /// <summary>
    /// 응답을 후처리합니다.
    /// </summary>
    /// <param name="parsedResponse">파싱된 응답</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>최종 결과</returns>
    protected virtual Task<ILLMResult> PostProcessAsync(IParsedResponse parsedResponse, ILLMContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(CreateSuccessResult(parsedResponse));
    }
    
    /// <summary>
    /// LLM을 호출합니다.
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>LLM 응답</returns>
    protected virtual async Task<string> CallLLMAsync(LLMPrompt prompt, ILLMContext context, CancellationToken cancellationToken)
    {
        // 기본적으로는 컨텍스트에서 LLM Provider를 가져와서 호출
        if (context.LLMProvider == null)
            throw new LLMProviderException("No LLM Provider available", "No LLM Provider configured in context", "NO_PROVIDER");
        
        var request = new LLMRequest
        {
            Prompt = prompt.Content,
            MaxTokens = prompt.MaxTokens,
            Temperature = prompt.Temperature,
            SystemMessage = prompt.SystemMessage
        };
        
        var response = await context.LLMProvider.CallAsync(request, cancellationToken);
        return response.Content;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <param name="parsedResponse">파싱된 응답</param>
    /// <returns>성공 결과</returns>
    protected ILLMResult CreateSuccessResult(IParsedResponse parsedResponse)
    {
        return new LLMResult
        {
            IsSuccess = true,
            FunctionType = Role,
            Response = parsedResponse,
            ExecutedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["function_type"] = GetType().Name,
                ["role"] = Role
            }
        };
    }
    
    /// <summary>
    /// 오류 결과를 생성합니다.
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errors">상세 오류 목록</param>
    /// <returns>오류 결과</returns>
    protected ILLMResult CreateErrorResult(string errorMessage, IEnumerable<string> errors)
    {
        return new LLMResult
        {
            IsSuccess = false,
            FunctionType = Role,
            ErrorMessage = errorMessage,
            Errors = errors.ToList(),
            ExecutedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["function_type"] = GetType().Name,
                ["role"] = Role,
                ["error_category"] = "execution_error"
            }
        };
    }
    
    #endregion
}
```

#### **LLMFunctionAttribute.cs 구현**
```csharp
namespace AIAgent.LLM.Functions.Base;

/// <summary>
/// LLM 기능 자동 발견을 위한 어트리뷰트입니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LLMFunctionAttribute : Attribute
{
    /// <summary>
    /// 기능의 역할입니다.
    /// </summary>
    public string Role { get; }
    
    /// <summary>
    /// 실행 우선순위입니다. 낮을수록 높은 우선순위입니다.
    /// </summary>
    public int Priority { get; set; } = 100;
    
    /// <summary>
    /// 기능 활성화 여부입니다.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 기능의 카테고리입니다.
    /// </summary>
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// 기능에 필요한 권한입니다.
    /// </summary>
    public string[]? RequiredPermissions { get; set; }
    
    /// <summary>
    /// 생성자입니다.
    /// </summary>
    /// <param name="role">기능의 역할</param>
    public LLMFunctionAttribute(string role)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }
}
```

### **Task 4.2: LLM Function Registry 시스템** (2.5시간)

#### **ILLMFunctionRegistry.cs 구현**
```csharp
namespace AIAgent.LLM.Registry;

/// <summary>
/// LLM 기능 레지스트리 인터페이스입니다.
/// </summary>
public interface ILLMFunctionRegistry
{
    /// <summary>
    /// 모든 등록된 LLM 기능을 가져옵니다.
    /// </summary>
    /// <returns>등록된 LLM 기능 목록</returns>
    IEnumerable<ILLMFunction> GetAll();
    
    /// <summary>
    /// 역할로 LLM 기능을 가져옵니다.
    /// </summary>
    /// <param name="role">역할</param>
    /// <returns>해당 역할의 LLM 기능</returns>
    ILLMFunction? GetByRole(string role);
    
    /// <summary>
    /// 타입으로 LLM 기능을 가져옵니다.
    /// </summary>
    /// <typeparam name="T">LLM 기능 타입</typeparam>
    /// <returns>해당 타입의 LLM 기능</returns>
    T? GetByType<T>() where T : class, ILLMFunction;
    
    /// <summary>
    /// LLM 기능을 등록합니다.
    /// </summary>
    /// <param name="function">등록할 LLM 기능</param>
    void Register(ILLMFunction function);
    
    /// <summary>
    /// 타입을 기반으로 LLM 기능을 등록합니다.
    /// </summary>
    /// <typeparam name="T">LLM 기능 타입</typeparam>
    void Register<T>() where T : class, ILLMFunction;
    
    /// <summary>
    /// 어셈블리에서 LLM 기능들을 자동 발견하여 등록합니다.
    /// </summary>
    /// <param name="assembly">대상 어셈블리</param>
    /// <returns>등록된 기능 수</returns>
    int RegisterFromAssembly(Assembly assembly);
    
    /// <summary>
    /// LLM 기능의 등록을 해제합니다.
    /// </summary>
    /// <param name="role">해제할 기능의 역할</param>
    /// <returns>해제 성공 여부</returns>
    bool Unregister(string role);
    
    /// <summary>
    /// 등록된 기능 수를 가져옵니다.
    /// </summary>
    int Count { get; }
    
    /// <summary>
    /// 특정 역할의 기능이 등록되어 있는지 확인합니다.
    /// </summary>
    /// <param name="role">확인할 역할</param>
    /// <returns>등록 여부</returns>
    bool IsRegistered(string role);
    
    /// <summary>
    /// 주어진 컨텍스트에 대해 실행 가능한 기능들을 가져옵니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>실행 가능한 기능들</returns>
    IEnumerable<ILLMFunction> GetAvailableFunctions(ILLMContext context);
}
```

#### **LLMFunctionRegistry.cs 구현**
```csharp
namespace AIAgent.LLM.Registry;

/// <summary>
/// LLM 기능 레지스트리 구현체입니다.
/// </summary>
public sealed class LLMFunctionRegistry : ILLMFunctionRegistry
{
    private readonly ConcurrentDictionary<string, ILLMFunction> _functions;
    private readonly ConcurrentDictionary<Type, ILLMFunction> _functionsByType;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMFunctionRegistry> _logger;
    
    public LLMFunctionRegistry(IServiceProvider serviceProvider, ILogger<LLMFunctionRegistry> logger)
    {
        _functions = new ConcurrentDictionary<string, ILLMFunction>(StringComparer.OrdinalIgnoreCase);
        _functionsByType = new ConcurrentDictionary<Type, ILLMFunction>();
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 등록된 기능 수입니다.
    /// </summary>
    public int Count => _functions.Count;
    
    /// <summary>
    /// 모든 등록된 LLM 기능을 가져옵니다.
    /// </summary>
    public IEnumerable<ILLMFunction> GetAll()
    {
        return _functions.Values.OrderBy(f => f.Priority).ToList();
    }
    
    /// <summary>
    /// 역할로 LLM 기능을 가져옵니다.
    /// </summary>
    public ILLMFunction? GetByRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;
        
        _functions.TryGetValue(role, out var function);
        return function;
    }
    
    /// <summary>
    /// 타입으로 LLM 기능을 가져옵니다.
    /// </summary>
    public T? GetByType<T>() where T : class, ILLMFunction
    {
        _functionsByType.TryGetValue(typeof(T), out var function);
        return function as T;
    }
    
    /// <summary>
    /// LLM 기능을 등록합니다.
    /// </summary>
    public void Register(ILLMFunction function)
    {
        if (function == null)
            throw new ArgumentNullException(nameof(function));
        
        var role = function.Role;
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Function role cannot be null or empty", nameof(function));
        
        if (_functions.TryAdd(role, function))
        {
            _functionsByType.TryAdd(function.GetType(), function);
            _logger.LogInformation("Registered LLM function: {Role} ({Type})", role, function.GetType().Name);
        }
        else
        {
            _logger.LogWarning("LLM function with role '{Role}' is already registered", role);
        }
    }
    
    /// <summary>
    /// 타입을 기반으로 LLM 기능을 등록합니다.
    /// </summary>
    public void Register<T>() where T : class, ILLMFunction
    {
        var function = _serviceProvider.GetService<T>();
        if (function != null)
        {
            Register(function);
        }
        else
        {
            _logger.LogError("Failed to create instance of LLM function: {Type}", typeof(T).Name);
        }
    }
    
    /// <summary>
    /// 어셈블리에서 LLM 기능들을 자동 발견하여 등록합니다.
    /// </summary>
    public int RegisterFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        
        var registeredCount = 0;
        var functionTypes = assembly.GetTypes()
            .Where(type => !type.IsAbstract && 
                          !type.IsInterface && 
                          typeof(ILLMFunction).IsAssignableFrom(type) &&
                          type.GetCustomAttribute<LLMFunctionAttribute>() != null)
            .ToList();
        
        foreach (var functionType in functionTypes)
        {
            try
            {
                var attribute = functionType.GetCustomAttribute<LLMFunctionAttribute>();
                if (attribute?.Enabled == false)
                {
                    _logger.LogInformation("Skipping disabled LLM function: {Type}", functionType.Name);
                    continue;
                }
                
                var function = _serviceProvider.GetService(functionType) as ILLMFunction;
                if (function != null)
                {
                    Register(function);
                    registeredCount++;
                }
                else
                {
                    _logger.LogWarning("Failed to create instance of LLM function: {Type}. Ensure it's registered in DI container.", functionType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering LLM function: {Type}", functionType.Name);
            }
        }
        
        _logger.LogInformation("Registered {Count} LLM functions from assembly: {Assembly}", 
            registeredCount, assembly.GetName().Name);
        
        return registeredCount;
    }
    
    /// <summary>
    /// LLM 기능의 등록을 해제합니다.
    /// </summary>
    public bool Unregister(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;
        
        if (_functions.TryRemove(role, out var function))
        {
            _functionsByType.TryRemove(function.GetType(), out _);
            _logger.LogInformation("Unregistered LLM function: {Role}", role);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 특정 역할의 기능이 등록되어 있는지 확인합니다.
    /// </summary>
    public bool IsRegistered(string role)
    {
        return !string.IsNullOrWhiteSpace(role) && _functions.ContainsKey(role);
    }
    
    /// <summary>
    /// 주어진 컨텍스트에 대해 실행 가능한 기능들을 가져옵니다.
    /// </summary>
    public IEnumerable<ILLMFunction> GetAvailableFunctions(ILLMContext context)
    {
        return _functions.Values
            .Where(function => function.CanExecute(context))
            .OrderBy(function => function.Priority)
            .ToList();
    }
}
```

### **Task 4.3: PlannerFunction 구체 구현** (2시간)

#### **PlannerFunction.cs 구현**
```csharp
namespace AIAgent.LLM.Functions.Planning;

/// <summary>
/// 계획 수립을 담당하는 LLM 기능입니다.
/// </summary>
[LLMFunction("Planner", Priority = 10, Category = "Planning")]
public sealed class PlannerFunction : BaseLLMFunction
{
    private readonly IPromptManager _promptManager;
    private readonly IJsonSerializer _jsonSerializer;
    
    public PlannerFunction(
        ILogger<PlannerFunction> logger,
        IStructuredLogger structuredLogger,
        IPromptManager promptManager,
        IJsonSerializer jsonSerializer) : base(logger, structuredLogger)
    {
        _promptManager = promptManager ?? throw new ArgumentNullException(nameof(promptManager));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
    }
    
    /// <summary>
    /// LLM 기능의 역할입니다.
    /// </summary>
    public override string Role => "Planner";
    
    /// <summary>
    /// LLM 기능에 대한 설명입니다.
    /// </summary>
    public override string Description => "사용자 요구사항을 분석하고 단계별 실행 계획을 수립합니다.";
    
    /// <summary>
    /// 실행 우선순위입니다.
    /// </summary>
    public override int Priority => 10; // 높은 우선순위
    
    /// <summary>
    /// 지원하는 입력 유형입니다.
    /// </summary>
    public override IEnumerable<Type> SupportedInputTypes => new[]
    {
        typeof(string),           // 단순 텍스트 요청
        typeof(AgentRequest),     // 구조화된 요청
        typeof(PlanningRequest)   // 계획 수립 전용 요청
    };
    
    /// <summary>
    /// 주어진 컨텍스트에서 이 기능이 실행 가능한지 확인합니다.
    /// </summary>
    public override bool CanExecute(ILLMContext context)
    {
        if (!base.CanExecute(context))
            return false;
        
        // 계획 수립이 필요한 키워드들 확인
        var userMessage = context.Request?.UserMessage?.ToLowerInvariant() ?? string.Empty;
        var planningKeywords = new[] { "plan", "step", "how", "create", "build", "implement", "design", "strategy" };
        
        return planningKeywords.Any(keyword => userMessage.Contains(keyword));
    }
    
    /// <summary>
    /// 프롬프트를 준비합니다.
    /// </summary>
    protected override async Task<LLMPrompt?> PreparePromptAsync(ILLMContext context, CancellationToken cancellationToken)
    {
        try
        {
            var userMessage = context.Request?.UserMessage ?? string.Empty;
            var conversationHistory = context.Request?.ConversationHistory;
            
            // 프롬프트 템플릿 로드
            var promptTemplate = await _promptManager.GetPromptAsync("planner", cancellationToken);
            if (string.IsNullOrEmpty(promptTemplate))
            {
                throw new InvalidOperationException("Planner prompt template not found");
            }
            
            // 사용 가능한 도구 및 기능 목록 구성
            var availableTools = GetAvailableToolsDescription(context);
            var availableFunctions = GetAvailableFunctionsDescription(context);
            
            // 프롬프트 변수 치환
            var prompt = promptTemplate
                .Replace("{{USER_MESSAGE}}", userMessage)
                .Replace("{{AVAILABLE_TOOLS}}", availableTools)
                .Replace("{{AVAILABLE_FUNCTIONS}}", availableFunctions)
                .Replace("{{CURRENT_TIME}}", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                .Replace("{{CONVERSATION_HISTORY}}", FormatConversationHistory(conversationHistory));
            
            return new LLMPrompt
            {
                Content = prompt,
                SystemMessage = "You are an expert planning assistant. Create detailed, actionable plans.",
                MaxTokens = 2048,
                Temperature = 0.3 // 계획은 일관성이 중요하므로 낮은 temperature
            };
        }
        catch (Exception ex)
        {
            throw new LLMFunctionException("Failed to prepare planner prompt", ex, "PROMPT_PREPARATION_ERROR");
        }
    }
    
    /// <summary>
    /// LLM 응답을 파싱합니다.
    /// </summary>
    protected override async Task<IParsedResponse?> ParseResponseAsync(string llmResponse, ILLMContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
            return null;
        
        try
        {
            // JSON 형태의 응답 파싱 시도
            if (llmResponse.Trim().StartsWith("{") && llmResponse.Trim().EndsWith("}"))
            {
                var planningResult = await _jsonSerializer.DeserializeAsync<PlanningResult>(llmResponse, cancellationToken);
                if (planningResult != null)
                {
                    return new PlannerParsedResponse
                    {
                        Plan = planningResult,
                        RawResponse = llmResponse,
                        ParsedAt = DateTimeOffset.UtcNow
                    };
                }
            }
            
            // JSON 파싱 실패 시 텍스트 기반 파싱
            var textBasedPlan = ParseTextBasedResponse(llmResponse);
            return new PlannerParsedResponse
            {
                Plan = textBasedPlan,
                RawResponse = llmResponse,
                ParsedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            throw new LLMFunctionException("Failed to parse planner response", ex, "RESPONSE_PARSING_ERROR");
        }
    }
    
    /// <summary>
    /// 파싱된 응답의 유효성을 검사합니다.
    /// </summary>
    protected override Task<ValidationResult> ValidateResponseAsync(IParsedResponse parsedResponse, ILLMContext context, CancellationToken cancellationToken)
    {
        if (parsedResponse is not PlannerParsedResponse plannerResponse)
            return Task.FromResult(ValidationResult.Failure("Invalid response type for planner"));
        
        var errors = new List<string>();
        
        if (plannerResponse.Plan == null)
            errors.Add("Plan is null");
        
        if (plannerResponse.Plan?.Steps == null || !plannerResponse.Plan.Steps.Any())
            errors.Add("Plan must contain at least one step");
        
        // 단계 순서 검증
        if (plannerResponse.Plan?.Steps != null)
        {
            var steps = plannerResponse.Plan.Steps.ToList();
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].Order != i + 1)
                    errors.Add($"Step order mismatch at position {i + 1}");
                
                if (string.IsNullOrWhiteSpace(steps[i].Description))
                    errors.Add($"Step {i + 1} must have a description");
            }
        }
        
        return Task.FromResult(errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors));
    }
    
    #region Private Helper Methods
    
    private string GetAvailableToolsDescription(ILLMContext context)
    {
        // 실제 구현에서는 context에서 사용 가능한 도구 목록을 가져옴
        return "- File operations (read, write, search)\n- Web search\n- Data analysis\n- Communication tools";
    }
    
    private string GetAvailableFunctionsDescription(ILLMContext context)
    {
        // 실제 구현에서는 registry에서 사용 가능한 LLM 기능들을 가져옴
        return "- Analysis and interpretation\n- Content generation\n- Summarization\n- Evaluation and validation";
    }
    
    private string FormatConversationHistory(ConversationHistory? history)
    {
        if (history?.Messages == null || !history.Messages.Any())
            return "No previous conversation";
        
        var formatted = new StringBuilder();
        foreach (var message in history.Messages.TakeLast(5)) // 최근 5개 메시지만
        {
            formatted.AppendLine($"[{message.Timestamp:HH:mm}] {message.Role}: {message.Content.Truncate(100)}");
        }
        
        return formatted.ToString();
    }
    
    private PlanningResult ParseTextBasedResponse(string response)
    {
        // 간단한 텍스트 기반 파싱 구현
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var steps = new List<ExecutionStep>();
        
        var stepNumber = 1;
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•") || 
                char.IsDigit(trimmedLine.FirstOrDefault()))
            {
                // 단계로 인식되는 라인
                var description = trimmedLine.TrimStart('-', '•', ' ').Trim();
                if (char.IsDigit(description.FirstOrDefault()))
                {
                    var dotIndex = description.IndexOf('.');
                    if (dotIndex > 0)
                        description = description[(dotIndex + 1)..].Trim();
                }
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    steps.Add(new ExecutionStep
                    {
                        Order = stepNumber++,
                        Description = description,
                        Type = "General",
                        EstimatedDuration = TimeSpan.FromMinutes(5) // 기본값
                    });
                }
            }
        }
        
        return new PlanningResult
        {
            PlanId = Guid.NewGuid().ToString("N")[..12],
            Summary = "Generated plan from user request",
            Steps = steps,
            EstimatedTotalDuration = TimeSpan.FromMinutes(steps.Count * 5),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    
    #endregion
}

/// <summary>
/// Planner 전용 파싱된 응답입니다.
/// </summary>
internal sealed class PlannerParsedResponse : IParsedResponse
{
    public required PlanningResult Plan { get; init; }
    public required string RawResponse { get; init; }
    public DateTimeOffset ParsedAt { get; init; }
}
```

### **Task 4.4: DI 통합 및 검증** (0.5시간)

#### **ServiceCollectionExtensions.cs 구현**
```csharp
namespace AIAgent.LLM.Extensions;

/// <summary>
/// LLM 시스템을 위한 DI 확장 메서드입니다.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// LLM 시스템 서비스들을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">설정</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddLLMSystem(this IServiceCollection services, IConfiguration configuration)
    {
        // Registry 등록
        services.AddSingleton<ILLMFunctionRegistry, LLMFunctionRegistry>();
        
        // LLM Functions 등록
        services.AddTransient<PlannerFunction>();
        
        // Prompt Manager 등록 (향후 구현)
        // services.AddSingleton<IPromptManager, PromptManager>();
        
        // JSON Serializer 등록 (향후 구현)
        // services.AddSingleton<IJsonSerializer, JsonSerializer>();
        
        return services;
    }
    
    /// <summary>
    /// LLM 시스템을 초기화합니다.
    /// </summary>
    /// <param name="serviceProvider">서비스 프로바이더</param>
    /// <returns>초기화된 Registry</returns>
    public static ILLMFunctionRegistry InitializeLLMSystem(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<ILLMFunctionRegistry>();
        var currentAssembly = Assembly.GetExecutingAssembly();
        
        // 현재 어셈블리에서 LLM Functions 자동 등록
        registry.RegisterFromAssembly(currentAssembly);
        
        return registry;
    }
}
```

## 🔍 검증 기준

### **필수 통과 조건**

#### **1. Template Method 패턴 완성**
- [ ] BaseLLMFunction의 모든 확장 포인트 정의 완료
- [ ] 실행 흐름이 Template Method 패턴을 정확히 구현
- [ ] 예외 처리와 로깅이 모든 단계에서 적용
- [ ] 취소 토큰이 모든 비동기 작업에서 지원

#### **2. Registry 시스템 동작**
- [ ] 자동 발견 메커니즘이 Attribute 기반으로 정상 동작
- [ ] DI 컨테이너와의 통합이 완벽히 작동
- [ ] 등록/해제/조회 기능이 모두 정상 동작
- [ ] 스레드 안전성 확보 (ConcurrentDictionary 사용)

#### **3. PlannerFunction 완성**
- [ ] 실제 사용자 요청을 받아서 계획을 생성
- [ ] JSON과 텍스트 기반 응답 파싱 모두 지원
- [ ] 계획의 유효성 검사가 완전히 구현
- [ ] 오류 상황에서 의미 있는 예외 발생

#### **4. 우아한 확장성**
- [ ] 새로운 LLM Function 추가 시 기존 코드 수정 불필요
- [ ] Switch-case나 if-else 체인 사용하지 않음
- [ ] Attribute 기반 자동 발견으로 완전 자동화
- [ ] DI 컨테이너를 통한 의존성 주입 완벽 지원

## 📝 완료 체크리스트

### **BaseLLMFunction**
- [ ] Template Method 패턴 완전 구현
- [ ] 모든 확장 포인트 정의 완료
- [ ] 로깅 및 성능 측정 통합
- [ ] 예외 처리 표준화

### **Registry 시스템**
- [ ] ILLMFunctionRegistry 인터페이스 완성
- [ ] LLMFunctionRegistry 구현 완료
- [ ] FunctionDiscoveryService 구현
- [ ] 자동 발견 메커니즘 구현

### **PlannerFunction**
- [ ] 완전한 구체 구현 완료
- [ ] 프롬프트 준비 로직 구현
- [ ] 응답 파싱 로직 구현 (JSON + 텍스트)
- [ ] 계획 유효성 검사 구현

### **통합 및 DI**
- [ ] ServiceCollection 확장 구현
- [ ] DI 컨테이너 통합 완료
- [ ] 자동 초기화 메커니즘 구현
- [ ] 단위 테스트 작성 완료

## 🎯 성공 지표

완료 시 다음이 모두 달성되어야 함:

1. ✅ **완전 동작하는 PlannerFunction**: 실제 요청을 받아 계획 생성
2. ✅ **확장 가능한 Registry**: 새 기능 추가 시 자동 발견 및 등록
3. ✅ **견고한 Template Method**: 모든 LLM 기능의 공통 기반 제공
4. ✅ **우아한 확장성**: Switch-case 없는 완전 자동화된 확장

---

**다음 계획**: [Plan 5: DI 컨테이너 설정 및 통합 테스트](plan5.md)