# Plan 2: 핵심 인터페이스 및 모델 정의

## 📋 개요

**목표**: 시스템 전체의 계약(Contract) 완성  
**예상 소요 시간**: 1일 (8시간)  
**의존성**: Plan 1 (프로젝트 기초 설정) 완료

## 🎯 구체적 목표

1. ✅ **완전한 인터페이스 계약** 정의
2. ✅ **타입 안전한 모델** 구현
3. ✅ **확장 가능한 열거형** 설계
4. ✅ **도메인 특화 예외** 체계 구축

## 🏗️ 작업 단계

### **Task 2.1: 핵심 인터페이스 설계** (3시간)

#### **AIAgent.Core/Interfaces/ 구조**
```
src/AIAgent.Core/Interfaces/
├── ILLMFunction.cs           # LLM 기능 기본 계약
├── ITool.cs                  # Tool 기본 계약  
├── ILLMProvider.cs          # LLM Provider 추상화
├── IOrchestrator.cs         # 오케스트레이션 엔진
├── IExecutionContext.cs     # 실행 컨텍스트
├── ILLMFunctionRegistry.cs  # LLM 기능 레지스트리
├── IToolRegistry.cs         # Tool 레지스트리
├── IPromptManager.cs        # 프롬프트 관리
└── IParsedResponse.cs       # 파싱된 응답
```

#### **ILLMFunction.cs 구현**
```csharp
namespace AIAgent.Core.Interfaces;

/// <summary>
/// LLM 기능의 기본 계약을 정의합니다.
/// </summary>
public interface ILLMFunction
{
    /// <summary>
    /// LLM 기능의 역할을 나타냅니다.
    /// </summary>
    string Role { get; }
    
    /// <summary>
    /// LLM 기능에 대한 설명입니다.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 실행 우선순위를 나타냅니다. 낮을수록 높은 우선순위입니다.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// 지원하는 입력 유형을 나타냅니다.
    /// </summary>
    IEnumerable<Type> SupportedInputTypes { get; }
    
    /// <summary>
    /// LLM 기능을 비동기적으로 실행합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>LLM 실행 결과</returns>
    Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 주어진 컨텍스트에서 이 기능이 실행 가능한지 확인합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>실행 가능 여부</returns>
    bool CanExecute(ILLMContext context);
    
    /// <summary>
    /// 실행 전 유효성 검사를 수행합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <returns>검증 결과</returns>
    Task<ValidationResult> ValidateAsync(ILLMContext context);
}
```

#### **ITool.cs 구현**
```csharp
namespace AIAgent.Core.Interfaces;

/// <summary>
/// Tool의 기본 계약을 정의합니다.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Tool의 고유 이름입니다.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Tool에 대한 설명입니다.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Tool의 버전 정보입니다.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Tool의 유형입니다.
    /// </summary>
    ToolType Type { get; }
    
    /// <summary>
    /// Tool의 계약 정보입니다.
    /// </summary>
    IToolContract Contract { get; }
    
    /// <summary>
    /// Tool을 비동기적으로 실행합니다.
    /// </summary>
    /// <param name="input">Tool 입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>Tool 실행 결과</returns>
    Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 주어진 입력으로 이 Tool이 실행 가능한지 확인합니다.
    /// </summary>
    /// <param name="input">Tool 입력</param>
    /// <returns>실행 가능 여부</returns>
    bool CanExecute(IToolInput input);
}
```

#### **ILLMProvider.cs 구현**
```csharp
namespace AIAgent.Core.Interfaces;

/// <summary>
/// LLM Provider의 기본 계약을 정의합니다.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Provider의 이름입니다.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 지원하는 모델 목록입니다.
    /// </summary>
    IEnumerable<string> SupportedModels { get; }
    
    /// <summary>
    /// 현재 활성화된 모델입니다.
    /// </summary>
    string CurrentModel { get; }
    
    /// <summary>
    /// Provider가 현재 사용 가능한지 확인합니다.
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// LLM 호출을 비동기적으로 실행합니다.
    /// </summary>
    /// <param name="request">LLM 요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>LLM 응답</returns>
    Task<LLMResponse> CallAsync(LLMRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모델을 변경합니다.
    /// </summary>
    /// <param name="modelName">변경할 모델 이름</param>
    Task SetModelAsync(string modelName);
    
    /// <summary>
    /// Provider의 상태를 확인합니다.
    /// </summary>
    /// <returns>상태 확인 결과</returns>
    Task<ProviderHealthCheck> CheckHealthAsync();
}
```

#### **IOrchestrator.cs 구현**
```csharp
namespace AIAgent.Core.Interfaces;

/// <summary>
/// 오케스트레이션 엔진의 계약을 정의합니다.
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// 사용자 요청을 처리합니다.
    /// </summary>
    /// <param name="request">사용자 요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리 결과</returns>
    Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 실행 계획을 수립합니다.
    /// </summary>
    /// <param name="request">사용자 요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 계획</returns>
    Task<ExecutionPlan> CreatePlanAsync(AgentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 계획을 실행합니다.
    /// </summary>
    /// <param name="plan">실행 계획</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<ExecutionResult> ExecutePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken = default);
}
```

### **Task 2.2: 데이터 모델 설계** (3시간)

#### **AIAgent.Core/Models/ 구조**
```
src/AIAgent.Core/Models/
├── Requests/
│   ├── AgentRequest.cs           # 사용자 요청
│   ├── LLMRequest.cs            # LLM 요청
│   └── ToolRequest.cs           # Tool 요청
├── Responses/
│   ├── AgentResponse.cs         # 에이전트 응답
│   ├── LLMResponse.cs           # LLM 응답
│   └── ToolResponse.cs          # Tool 응답
├── Context/
│   ├── ExecutionContext.cs      # 실행 컨텍스트
│   ├── LLMContext.cs           # LLM 컨텍스트
│   └── ConversationContext.cs  # 대화 컨텍스트
├── Planning/
│   ├── ExecutionPlan.cs        # 실행 계획
│   ├── ExecutionStep.cs        # 실행 단계
│   └── PlanningResult.cs       # 계획 수립 결과
└── Common/
    ├── ValidationResult.cs      # 검증 결과
    ├── Result.cs               # 일반적인 결과
    └── Metadata.cs             # 메타데이터
```

#### **AgentRequest.cs 구현**
```csharp
namespace AIAgent.Core.Models.Requests;

/// <summary>
/// 사용자의 에이전트 요청을 나타냅니다.
/// </summary>
public record AgentRequest
{
    /// <summary>
    /// 요청의 고유 식별자입니다.
    /// </summary>
    public required string RequestId { get; init; }
    
    /// <summary>
    /// 사용자의 메시지입니다.
    /// </summary>
    public required string UserMessage { get; init; }
    
    /// <summary>
    /// 사용자 컨텍스트입니다.
    /// </summary>
    public UserContext? UserContext { get; init; }
    
    /// <summary>
    /// 대화 이력입니다.
    /// </summary>
    public ConversationHistory? ConversationHistory { get; init; }
    
    /// <summary>
    /// 요청 시각입니다.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 요청에 대한 메타데이터입니다.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// 요청의 우선순위입니다.
    /// </summary>
    public RequestPriority Priority { get; init; } = RequestPriority.Normal;
    
    /// <summary>
    /// 타임아웃 설정입니다.
    /// </summary>
    public TimeSpan? Timeout { get; init; }
}
```

#### **ExecutionContext.cs 구현**
```csharp
namespace AIAgent.Core.Models.Context;

/// <summary>
/// 실행 컨텍스트 정보를 나타냅니다.
/// </summary>
public record ExecutionContext
{
    /// <summary>
    /// 실행 세션의 고유 식별자입니다.
    /// </summary>
    public required string SessionId { get; init; }
    
    /// <summary>
    /// 현재 실행 중인 요청입니다.
    /// </summary>
    public required AgentRequest Request { get; init; }
    
    /// <summary>
    /// 현재 실행 단계입니다.
    /// </summary>
    public int CurrentStep { get; init; }
    
    /// <summary>
    /// 전체 실행 단계 수입니다.
    /// </summary>
    public int TotalSteps { get; init; }
    
    /// <summary>
    /// 실행 시작 시각입니다.
    /// </summary>
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 취소 토큰입니다.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
    
    /// <summary>
    /// 실행 상태입니다.
    /// </summary>
    public ExecutionStatus Status { get; init; } = ExecutionStatus.Running;
    
    /// <summary>
    /// 이전 단계들의 결과입니다.
    /// </summary>
    public IReadOnlyList<StepResult> PreviousResults { get; init; } = Array.Empty<StepResult>();
    
    /// <summary>
    /// 공유 변수들입니다.
    /// </summary>
    public IReadOnlyDictionary<string, object> Variables { get; init; } = 
        new Dictionary<string, object>();
}
```

#### **Result.cs 구현 (Result Pattern)**
```csharp
namespace AIAgent.Core.Models.Common;

/// <summary>
/// 일반적인 결과를 나타내는 제네릭 클래스입니다.
/// </summary>
/// <typeparam name="T">결과 데이터의 타입</typeparam>
public record Result<T>
{
    /// <summary>
    /// 성공 여부를 나타냅니다.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 실패 여부를 나타냅니다.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// 결과 데이터입니다. 성공한 경우에만 유효합니다.
    /// </summary>
    public T? Data { get; init; }
    
    /// <summary>
    /// 오류 메시지입니다. 실패한 경우에만 유효합니다.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 오류 코드입니다.
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// 상세 오류 정보입니다.
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <param name="data">결과 데이터</param>
    /// <returns>성공 결과</returns>
    public static Result<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };
    
    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <param name="exception">예외</param>
    /// <returns>실패 결과</returns>
    public static Result<T> Failure(string errorMessage, string? errorCode = null, Exception? exception = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode,
        Exception = exception
    };
}

/// <summary>
/// 데이터가 없는 결과를 나타내는 클래스입니다.
/// </summary>
public record Result
{
    /// <summary>
    /// 성공 여부를 나타냅니다.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 실패 여부를 나타냅니다.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// 오류 메시지입니다.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 오류 코드입니다.
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// 상세 오류 정보입니다.
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    public static Result Success() => new() { IsSuccess = true };
    
    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <param name="exception">예외</param>
    /// <returns>실패 결과</returns>
    public static Result Failure(string errorMessage, string? errorCode = null, Exception? exception = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode,
        Exception = exception
    };
}
```

### **Task 2.3: 열거형 정의** (1시간)

#### **AIAgent.Core/Enums/ 구현**
```csharp
// ExecutionStatus.cs
namespace AIAgent.Core.Enums;

/// <summary>
/// 실행 상태를 나타냅니다.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// 대기 중
    /// </summary>
    Pending,
    
    /// <summary>
    /// 실행 중
    /// </summary>
    Running,
    
    /// <summary>
    /// 성공적으로 완료됨
    /// </summary>
    Completed,
    
    /// <summary>
    /// 실패함
    /// </summary>
    Failed,
    
    /// <summary>
    /// 취소됨
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// 타임아웃됨
    /// </summary>
    TimedOut
}

// LLMFunctionType.cs
namespace AIAgent.Core.Enums;

/// <summary>
/// LLM 기능의 유형을 나타냅니다.
/// </summary>
public enum LLMFunctionType
{
    /// <summary>
    /// 계획 수립자
    /// </summary>
    Planner,
    
    /// <summary>
    /// 분석자
    /// </summary>
    Analyzer,
    
    /// <summary>
    /// 생성자
    /// </summary>
    Generator,
    
    /// <summary>
    /// 요약자
    /// </summary>
    Summarizer,
    
    /// <summary>
    /// 평가자
    /// </summary>
    Evaluator,
    
    /// <summary>
    /// 재작성자
    /// </summary>
    Rewriter,
    
    /// <summary>
    /// 설명자
    /// </summary>
    Explainer,
    
    /// <summary>
    /// 추론자
    /// </summary>
    Reasoner,
    
    /// <summary>
    /// 변환자
    /// </summary>
    Converter,
    
    /// <summary>
    /// 시각화자
    /// </summary>
    Visualizer,
    
    /// <summary>
    /// 도구 파라미터 설정자
    /// </summary>
    ToolParameterSetter,
    
    /// <summary>
    /// 대화 관리자
    /// </summary>
    DialogueManager,
    
    /// <summary>
    /// 지식 검색자
    /// </summary>
    KnowledgeRetriever,
    
    /// <summary>
    /// 메타 관리자
    /// </summary>
    MetaManager
}

// ToolType.cs
namespace AIAgent.Core.Enums;

/// <summary>
/// Tool의 유형을 나타냅니다.
/// </summary>
public enum ToolType
{
    /// <summary>
    /// 내장 도구
    /// </summary>
    BuiltIn,
    
    /// <summary>
    /// 플러그인 도구
    /// </summary>
    PlugIn,
    
    /// <summary>
    /// MCP 도구
    /// </summary>
    MCP
}
```

### **Task 2.4: 예외 체계 구축** (1시간)

#### **AIAgent.Core/Exceptions/ 구현**
```csharp
// AgentException.cs
namespace AIAgent.Core.Exceptions;

/// <summary>
/// AI Agent 시스템의 기본 예외 클래스입니다.
/// </summary>
public abstract class AgentException : Exception
{
    /// <summary>
    /// 오류 코드입니다.
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// 오류가 발생한 컴포넌트입니다.
    /// </summary>
    public string Component { get; }
    
    /// <summary>
    /// 생성자입니다.
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <param name="component">컴포넌트 이름</param>
    protected AgentException(string message, string errorCode, string component) 
        : base(message)
    {
        ErrorCode = errorCode;
        Component = component;
    }
    
    /// <summary>
    /// 생성자입니다.
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="innerException">내부 예외</param>
    /// <param name="errorCode">오류 코드</param>
    /// <param name="component">컴포넌트 이름</param>
    protected AgentException(string message, Exception innerException, string errorCode, string component) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Component = component;
    }
}

// LLMProviderException.cs
namespace AIAgent.Core.Exceptions;

/// <summary>
/// LLM Provider 관련 예외입니다.
/// </summary>
public sealed class LLMProviderException : AgentException
{
    /// <summary>
    /// Provider 이름입니다.
    /// </summary>
    public string ProviderName { get; }
    
    /// <summary>
    /// 생성자입니다.
    /// </summary>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="message">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    public LLMProviderException(string providerName, string message, string errorCode = "LLM_PROVIDER_ERROR")
        : base(message, errorCode, "LLMProvider")
    {
        ProviderName = providerName;
    }
    
    /// <summary>
    /// 생성자입니다.
    /// </summary>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="message">오류 메시지</param>
    /// <param name="innerException">내부 예외</param>
    /// <param name="errorCode">오류 코드</param>
    public LLMProviderException(string providerName, string message, Exception innerException, string errorCode = "LLM_PROVIDER_ERROR")
        : base(message, innerException, errorCode, "LLMProvider")
    {
        ProviderName = providerName;
    }
}
```

## 🔍 검증 기준

### **필수 통과 조건**

#### **1. 컴파일 성공**
- [ ] 모든 인터페이스와 모델이 에러 없이 컴파일
- [ ] Nullable Reference Types 경고 0건
- [ ] XML 문서화 커버리지 100%

#### **2. 인터페이스 일관성**
- [ ] 모든 인터페이스가 동일한 패턴 준수
- [ ] 비동기 메서드에 CancellationToken 포함
- [ ] 적절한 반환 타입 사용 (Result Pattern)

#### **3. 모델 검증**
- [ ] Records 사용으로 불변성 확보
- [ ] 필수 속성은 required 키워드 사용
- [ ] 적절한 기본값 설정

#### **4. 단위 테스트**
- [ ] 모든 모델에 대한 기본 테스트 작성
- [ ] Result Pattern 동작 검증
- [ ] 예외 클래스 테스트

## 📝 완료 체크리스트

### **인터페이스**
- [ ] ILLMFunction 완전 정의
- [ ] ITool 완전 정의
- [ ] ILLMProvider 완전 정의
- [ ] IOrchestrator 완전 정의
- [ ] 모든 지원 인터페이스 정의

### **모델**
- [ ] 요청/응답 모델 완성
- [ ] 컨텍스트 모델 완성
- [ ] Result Pattern 구현
- [ ] ValidationResult 구현

### **열거형**
- [ ] ExecutionStatus 정의
- [ ] LLMFunctionType 정의
- [ ] ToolType 정의
- [ ] 기타 필요한 열거형 정의

### **예외**
- [ ] AgentException 기본 클래스
- [ ] 도메인별 예외 클래스
- [ ] 적절한 예외 계층 구조

## 🎯 성공 지표

완료 시 다음이 모두 달성되어야 함:

1. ✅ **완전한 계약 정의**: 모든 주요 컴포넌트의 인터페이스 완성
2. ✅ **타입 안전성**: Nullable Reference Types와 Records 활용
3. ✅ **확장성**: 새로운 기능 추가 시 기존 계약 수정 불필요
4. ✅ **일관성**: 모든 인터페이스가 동일한 패턴과 규칙 준수

---

**다음 계획**: [Plan 3: 공통 인프라 구축](plan3.md)