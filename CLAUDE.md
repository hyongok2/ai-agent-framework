# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an AI Agent Framework built in C# that implements a modular LLM orchestration system. The framework is designed with extensibility in mind, featuring a **Plan-Execute** orchestration pattern where LLM planning functions coordinate the execution of various tools and specialized LLM functions.

### Core Architecture

The system follows a layered architecture with clear separation of concerns:

**주요 구성요소:**
- **Orchestration Engine**: 전체 실행 흐름을 관리하는 핵심 엔진 (사용자 입력 → LLM Plan → 기능 실행 → 반복)
- **LLM System**: 14가지 역할 기반 LLM 기능 (Planner, Analyzer, Generator, Summarizer 등)
- **Tool System**: 3가지 유형의 도구 (Built-In Tools, Plug-In Tools, MCP Tools)
- **Common Infrastructure**: 공통 유틸리티, 로깅, 설정 관리

## Development Commands

Since this project is in the early planning phase, there are no build commands yet. The codebase currently contains only documentation and design specifications.

**Current Status**: Documentation and planning phase - no executable code yet

## Project Structure (Planned)

Based on the design documents, the intended structure is:

```
AIAgent/
├── src/
│   ├── AIAgent.Core/                    # 핵심 인터페이스 및 모델
│   │   ├── Interfaces/                  # 핵심 인터페이스들
│   │   ├── Models/                      # 데이터 모델
│   │   │   ├── Requests/                # 요청 관련 모델
│   │   │   ├── Responses/               # 응답 관련 모델
│   │   │   └── Context/                 # 컨텍스트 관련 모델
│   │   ├── Enums/                       # 열거형
│   │   └── Exceptions/                  # 커스텀 예외
│   ├── AIAgent.Orchestration/           # 오케스트레이션 엔진
│   │   ├── Engine/                      # 오케스트레이션 엔진 구현
│   │   ├── Planners/                    # 계획 수립 관련
│   │   ├── Executors/                   # 실행 관련
│   │   └── Context/                     # 실행 컨텍스트 관리
│   ├── AIAgent.LLM/                     # LLM 시스템
│   │   ├── Providers/                   # LLM Provider 구현
│   │   │   ├── OpenAI/                  # OpenAI 관련
│   │   │   ├── Claude/                  # Claude 관련 (향후)
│   │   │   └── Local/                   # Local LLM 관련 (향후)
│   │   ├── Functions/                   # LLM 기능 구현 (14가지 역할)
│   │   │   ├── Planning/                # Planner, MetaManager
│   │   │   ├── Analysis/                # Analyzer, Evaluator
│   │   │   ├── Generation/              # Generator, Rewriter
│   │   │   ├── Communication/           # Explainer, DialogueManager
│   │   │   └── Utilities/               # Converter, Visualizer 등
│   │   ├── Prompts/                     # 프롬프트 관리
│   │   └── Parsers/                     # 응답 파싱
│   ├── AIAgent.Tools/                   # Tool 시스템
│   │   ├── BuiltIn/                     # 내장 도구
│   │   │   ├── Embedding/               # 임베딩 관련
│   │   │   └── VectorDb/                # 벡터 DB 관련
│   │   ├── PlugIn/                      # 플러그인 도구
│   │   │   ├── Registry/                # 플러그인 등록/관리
│   │   │   ├── Loaders/                 # 플러그인 로더
│   │   │   └── Contracts/               # 플러그인 계약
│   │   ├── MCP/                         # MCP 도구
│   │   │   ├── Client/                  # MCP 클라이언트
│   │   │   ├── Protocols/               # 프로토콜 구현
│   │   │   └── Adapters/                # MCP 어댑터
│   │   └── Registry/                    # 통합 도구 레지스트리
│   ├── AIAgent.Common/                  # 공통 유틸리티
│   │   ├── Configuration/               # 설정 관리
│   │   ├── Logging/                     # 로깅
│   │   ├── Caching/                     # 캐싱
│   │   ├── Validation/                  # 입력 검증
│   │   ├── Serialization/               # 직렬화
│   │   └── Extensions/                  # 확장 메서드
│   └── AIAgent.Host/                    # 호스팅 및 진입점
│       ├── Controllers/                 # API 컨트롤러 (Web 인터페이스)
│       ├── Services/                    # 애플리케이션 서비스
│       ├── Middleware/                  # 미들웨어
│       └── Configuration/               # 호스팅 설정
├── tests/
│   ├── AIAgent.Core.Tests/
│   ├── AIAgent.Orchestration.Tests/
│   ├── AIAgent.LLM.Tests/
│   ├── AIAgent.Tools.Tests/
│   ├── AIAgent.Common.Tests/
│   ├── AIAgent.Host.Tests/
│   └── AIAgent.Integration.Tests/       # 통합 테스트
├── configs/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   └── prompts/
│       ├── system/                      # 시스템 프롬프트
│       ├── functions/                   # 기능별 프롬프트
│       │   ├── planning/
│       │   ├── analysis/
│       │   ├── generation/
│       │   └── communication/
│       └── templates/                   # 공통 템플릿
└── docs/
    ├── 00_dev-plan/                     # 개발 계획 문서들
    ├── api/                             # API 문서
    └── architecture/                    # 아키텍처 문서
```

### File Organization Principles

#### One Class Per File Rule
- **한 파일 = 한 클래스**: 모든 클래스는 독립된 파일에 작성
- **파일명 = 클래스명**: `UserService.cs` → `public class UserService`
- **인터페이스 별도 파일**: `IUserService.cs` → `public interface IUserService`
- **예외**: 단순 DTO, Enum, 밀접한 관련이 있는 작은 Helper 클래스만 예외적으로 허용

#### Folder Structure Guidelines
- **의미적 일관성**: 폴더명은 포함된 클래스들의 책임/도메인을 명확히 표현
- **적절한 그룹핑**: 관련 기능별로 논리적 그룹화 (5-10개 파일이 적절)
- **과도한 계층화 방지**: 최대 3-4단계 깊이 유지 (`src/Project/Domain/Feature/` 수준)
- **확장성 고려**: 새로운 기능 추가 시 기존 구조를 깨지 않도록

#### Layering Best Practices
- **도메인별 분리**: 각 도메인(LLM, Tools, Orchestration)은 독립적인 레이어
- **수평적 분리 우선**: 깊이보다는 폭으로 확장 (Planning/, Analysis/, Generation/)
- **파일 수 제한**: 한 폴더에 15개 이상 파일 시 하위 폴더 분리 검토
- **명명 일관성**: 비슷한 역할의 폴더는 동일한 명명 패턴 유지

#### Examples of Good vs Bad Structure
✅ **Good Structure:**
```
Functions/
├── Planning/
│   ├── PlannerFunction.cs
│   └── MetaManagerFunction.cs
├── Analysis/
│   ├── AnalyzerFunction.cs
│   └── EvaluatorFunction.cs
└── Generation/
    ├── GeneratorFunction.cs
    └── RewriterFunction.cs
```

❌ **Bad Structure:**
```
Functions/
├── Core/
│   └── Base/
│       └── Abstract/
│           └── Foundation/
│               └── BaseLLMFunction.cs  # Too deep!
└── AllFunctions.cs  # Multiple classes in one file!
```

## Key Design Principles

### Orchestration Pattern
- **고정된 오케스트레이션**: [계획-실행] 흐름은 변경되지 않음
- **LLM Plan 중심**: Plan LLM이 모든 실행 결정을 담당
- **단계별 실행**: 계획 수립 → 실행 → 결과 평가 → 다음 단계 결정

### LLM Functions (14가지 역할)
1. **Planner/Orchestrator**: 전체 실행 계획 수립
2. **Interpreter/Analyzer**: 입력 분석 및 해석
3. **Summarizer**: 정보 요약
4. **Generator**: 새 콘텐츠 생성
5. **Evaluator/Critic**: 품질 평가
6. **Rewriter/Refiner**: 콘텐츠 개선
7. **Explainer/Tutor**: 개념 설명
8. **Reasoner/Inference Engine**: 논리적 추론
9. **Converter/Translator**: 형식/언어 변환
10. **Visualizer**: 텍스트 기반 시각화
11. **Tool Parameter Setter**: 도구 파라미터 설정
12. **Dialogue Manager**: 대화 흐름 관리
13. **Knowledge Retriever**: 정보 검색
14. **Meta-Manager**: 실행 최적화

### Tool System Architecture
- **Built-In Tools**: 시스템 필수 기능 (임베딩 캐싱, Vector DB 등)
- **Plug-In Tools**: 도메인 특화 확장 (DLL, Reflection, Attribute 기반)
- **MCP Tools**: 표준 프로토콜 기반 확장

### Extensibility Strategy
5가지 튜닝 요소를 통한 특화된 에이전트 개발:
1. **도구(Tools) 확장**
2. **LLM 모델 전환**
3. **프롬프트 관리**
4. **사용자 인터페이스 다양화**
5. **LLM 기능 확장**

## Implementation Guidelines

### Base Class Pattern
- **BaseLLMFunction**: 모든 LLM 기능의 기본 클래스 (Template Method 패턴)
- **ITool Interface**: 모든 도구가 구현해야 하는 공통 인터페이스
- **Registry Pattern**: 도구와 LLM 기능의 중앙 집중식 관리

### Response Structure
- **JSON 기반 응답**: 모든 LLM 응답은 구조화된 JSON
- **표준화된 형식**: status, result, next_step, metadata 포함
- **타입 안전성**: 강타입 DTO 사용

### Prompt Management
- **파일 기반 관리**: configs/prompts/ 디렉토리
- **치환 시스템**: `{{variable_name}}` 형식
- **TTL 캐싱**: 성능 최적화

## Development Guidelines

### Coding Standards

#### C# Conventions & Clean Code Principles
- **C# Conventions**: Microsoft C# 코딩 규칙 준수 (PascalCase for public members, camelCase for private fields, etc.)
- **Clean Code**: 의미 있는 변수명, 함수명 사용. 코드 자체가 문서가 되도록 작성
- **Single Responsibility Principle (SRP)**: 클래스와 메서드는 단일 책임만 가져야 함
- **Minimal Nesting**: 조건문/반복문 중첩 깊이 최소화 (Early Return, Guard Clauses 활용)
- **No Temporary Code**: 임시 코드 작성 금지 (`return true;`, `throw new NotImplementedException();` 등)
- **Complete Implementation**: 모든 코드는 완성된 형태로 작성. 의미 있는 로직과 예외 처리 포함

#### Modern C# Features
- **C# 11+ Features**: 최신 C# 언어 기능 적극 활용 (Pattern Matching, Records, Init-only properties)
- **Nullable Reference Types**: 활성화하여 null 안전성 확보
- **Expression-bodied Members**: 간단한 메서드/속성은 expression body 사용
- **Using Declarations**: 리소스 자동 정리

#### Architecture Principles
- **Dependency Injection**: DI 컨테이너 기반 의존성 관리
- **Interface Segregation**: 작은 단위의 인터페이스 정의 (ISP 준수)
- **Open/Closed Principle**: 확장에는 열려있고 수정에는 닫혀있는 설계
- **Extensible Design**: 미래 확장을 고려한 설계 (Strategy Pattern, Factory Pattern 등 활용)
- **Elegant Extensibility**: 확장 가능한 요소는 우아한 방식으로 처리
- **Async/Await**: 모든 I/O 작업은 비동기 처리

#### Extensibility Design Patterns
- **Strategy Pattern**: 알고리즘/행동 변경 시 사용
- **Factory Pattern**: 객체 생성 로직 확장 시 사용
- **Registry Pattern**: 동적 등록/발견이 필요한 요소에 사용
- **Plugin Architecture**: 메타데이터 기반 자동 발견
- **Command Pattern**: 실행 가능한 작업들의 확장
- **Chain of Responsibility**: 처리 단계의 유연한 확장

### Testing Strategy
- **xUnit Framework**: 단위 테스트
- **Moq**: Mock 객체 생성
- **FluentAssertions**: 가독성 높은 assertion
- **Test Coverage**: 80% 이상 목표

### Error Handling & Code Quality
- **Custom Exceptions**: 도메인별 예외 클래스 정의. 의미 있는 에러 메시지 제공
- **No Exception Swallowing**: 예외를 무시하지 않음. 적절한 처리 또는 재전파
- **Retry Policies**: LLM 호출 실패 시 지수 백오프를 통한 재시도
- **Fallback Strategies**: Provider 장애 시 명확한 대체 방안 구현
- **Structured Logging**: Serilog 기반 구조화된 로깅. 성능/보안에 민감한 정보 제외
- **Input Validation**: 모든 외부 입력에 대한 철저한 검증
- **Immutable Objects**: 가능한 한 불변 객체 사용 (Records, readonly fields)

### Code Quality Enforcement
- **Static Analysis**: 코드 분석 도구 사용 (SonarQube, StyleCop 등)
- **Code Reviews**: 모든 코드 변경사항에 대한 리뷰 필수
- **Performance Considerations**: 메모리 누수, 불필요한 할당 최소화
- **Resource Management**: IDisposable 구현 객체의 적절한 해제
- **Thread Safety**: 멀티스레드 환경에서의 안전성 고려

### Iterative Development & Refactoring

#### Development Approach
- **Function First**: 기능 우선 구현으로 빠른 동작하는 버전 확보
- **Refactor Early & Often**: 어느 정도 구현되면 즉시 리팩토링 시작
- **Incremental Improvement**: 작은 단위로 지속적인 코드 품질 개선
- **Technical Debt Management**: 의식적으로 기술 부채를 관리하고 해결

#### Refactoring Strategy
1. **Green Phase**: 기능이 동작하는 코드 작성 (테스트 통과)
2. **Refactor Phase**: 코드 구조 개선 (기능 변경 없이)
3. **Validate Phase**: 리팩토링 후 테스트 재실행 확인
4. **Repeat**: 지속적인 개선 사이클 반복

#### When to Refactor
- **Feature Complete**: 기본 기능 구현 완료 시점
- **Code Smell Detection**: 코드 냄새 감지 즉시
- **Performance Issues**: 성능 문제 발견 시
- **Extensibility Needs**: 확장성 개선 필요 시점
- **Regular Intervals**: 정기적인 코드 품질 점검 시

#### Refactoring Guidelines
```csharp
// Phase 1: Make it work (기능 구현)
public class UserService
{
    public User CreateUser(string name, string email)
    {
        // Basic implementation that works
        var user = new User { Name = name, Email = email };
        Database.Save(user);
        EmailService.SendWelcomeEmail(email);
        return user;
    }
}

// Phase 2: Make it better (리팩토링)
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IUserValidator _validator;

    public UserService(IUserRepository userRepository, 
                      IEmailService emailService, 
                      IUserValidator validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsSuccess)
            return Result<User>.Failure(validationResult.Errors);

        var user = new User(request.Name, request.Email);
        
        await _userRepository.SaveAsync(user);
        await _emailService.SendWelcomeEmailAsync(user.Email);
        
        return Result<User>.Success(user);
    }
}
```

#### Refactoring Checklist
- [ ] 기능이 정상적으로 동작하는가?
- [ ] 테스트가 모두 통과하는가?
- [ ] 의존성 주입이 적용되었는가?
- [ ] 단일 책임 원칙을 준수하는가?
- [ ] 예외 처리가 적절한가?
- [ ] 확장 가능한 구조인가?
- [ ] 성능상 문제가 없는가?
- [ ] 코드 가독성이 향상되었는가?

#### Continuous Improvement Practices
- **Daily Refactoring**: 매일 작은 개선사항 적용
- **Code Review Focus**: 리뷰 시 리팩토링 기회 적극 발굴
- **Technical Debt Tracking**: 기술 부채 항목 추적 및 관리
- **Refactoring Sprint**: 주기적인 리팩토링 전용 스프린트 실시

## Configuration Management

### Settings Structure
```json
{
  "LLMProviders": {
    "OpenAI": {
      "ApiKey": "...",
      "Model": "gpt-4",
      "Temperature": 0.7
    }
  },
  "Tools": {
    "PluginPath": "./plugins",
    "MCPEndpoints": []
  }
}
```

### Prompt Templates
- **위치**: configs/prompts/functions/
- **명명 규칙**: {function-name}.md
- **변수 치환**: `{{variable_name}}` 패턴

## Development Phases

### Phase 1: 기초 인프라 구축 (4.5일 예상)
- 프로젝트 구조 및 인터페이스 정의
- BaseLLMFunction 추상 클래스 구현
- 공통 유틸리티 구축

### Phase 2: LLM 추상화 계층 (6일 예상)
- OpenAI Provider 구현
- 프롬프트 관리 시스템
- PlannerFunction 첫 구현

### Phase 3: Tool 시스템 (예정)
- Built-In Tools 구현
- Plug-In Tools 아키텍처
- MCP Tools 통합

## Code Quality Standards

### Mandatory Code Quality Rules
1. **No Placeholder Code**: 절대로 임시/플레이스홀더 코드 작성 금지
   - ❌ `return true;` (임시 반환)
   - ❌ `throw new NotImplementedException();`
   - ❌ `// TODO: implement later`
   - ✅ 완전하고 의미 있는 구현만 작성

2. **Control Flow Optimization**: 제어 흐름 최적화
   - ❌ 깊은 중첩 구조 (`if` 안의 `if` 안의 `for`)
   - ✅ Early return, Guard clauses 활용
   - ✅ 조건문/반복문 중첩 깊이 3단계 이하 유지

3. **Single Responsibility**: 단일 책임 원칙 엄격 준수
   - 각 클래스/메서드는 하나의 명확한 책임만 가짐
   - 메서드 길이는 20줄 이하 권장
   - 클래스는 한 가지 변경 이유만 가져야 함

4. **Extensibility First**: 확장성 우선 설계
   - 새로운 요구사항에 기존 코드 수정 최소화
   - Interface/Abstract class 활용한 확장점 제공
   - Open/Closed Principle 엄격 준수

### Elegant Extensibility Guidelines

#### 🚫 Avoid Anti-Patterns
```csharp
// ❌ BAD: Hard-coded switch statements
public void ProcessFunction(string functionType)
{
    switch (functionType)
    {
        case "Planner": /* logic */; break;
        case "Analyzer": /* logic */; break;
        case "Generator": /* logic */; break;
        // Adding new function requires modifying this code!
    }
}

// ❌ BAD: If-else chains
public ILLMProvider CreateProvider(string providerType)
{
    if (providerType == "OpenAI") return new OpenAIProvider();
    else if (providerType == "Claude") return new ClaudeProvider();
    else if (providerType == "Local") return new LocalProvider();
    // Adding new provider requires modifying this method!
}
```

#### ✅ Use Elegant Patterns
```csharp
// ✅ GOOD: Registry Pattern
public interface IFunctionRegistry
{
    void Register<T>() where T : ILLMFunction;
    ILLMFunction Create(string functionType);
    IEnumerable<ILLMFunction> GetAll();
}

// ✅ GOOD: Factory with Auto-Discovery
public class LLMProviderFactory
{
    private readonly Dictionary<string, Func<ILLMProvider>> _providers;
    
    public LLMProviderFactory()
    {
        // Auto-discover providers via reflection/attributes
        _providers = DiscoverProviders();
    }
    
    public ILLMProvider Create(string providerType) =>
        _providers.TryGetValue(providerType, out var factory) 
            ? factory() 
            : throw new UnsupportedProviderException(providerType);
}

// ✅ GOOD: Strategy Pattern
public interface IExecutionStrategy
{
    bool CanHandle(ExecutionContext context);
    Task<ExecutionResult> ExecuteAsync(ExecutionContext context);
}

public class StrategyExecutor
{
    private readonly IEnumerable<IExecutionStrategy> _strategies;
    
    public async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(context))
            ?? throw new NoSuitableStrategyException();
        
        return await strategy.ExecuteAsync(context);
    }
}
```

#### Extensibility Implementation Guidelines

1. **Attribute-Based Discovery**: Use attributes for metadata-driven extension
```csharp
[LLMFunction("Planner", Priority = 100)]
public class PlannerFunction : BaseLLMFunction { }

[Tool("FileReader", Category = "IO")]
public class FileReaderTool : ITool { }
```

2. **Interface-Based Contracts**: Define clear contracts for extensions
```csharp
public interface ILLMFunction
{
    string Role { get; }
    string Description { get; }
    int Priority { get; }
    Task<ILLMResult> ExecuteAsync(ILLMContext context);
}
```

3. **Composition Over Inheritance**: Prefer composition for flexible extension
```csharp
public class CompositeValidator : IValidator
{
    private readonly IEnumerable<IValidator> _validators;
    
    public ValidationResult Validate(object input) =>
        _validators.Aggregate(ValidationResult.Success, 
            (result, validator) => result.Combine(validator.Validate(input)));
}
```

4. **Event-Driven Extensions**: Use events for loose coupling
```csharp
public class OrchestrationEngine
{
    public event EventHandler<StepCompletedEventArgs> StepCompleted;
    public event EventHandler<ExecutionStartedEventArgs> ExecutionStarted;
    
    protected virtual void OnStepCompleted(StepCompletedEventArgs args) =>
        StepCompleted?.Invoke(this, args);
}
```

### Code Review Checklist

#### File & Structure Organization
- [ ] 한 파일에 한 개의 클래스만 정의되어 있는가?
- [ ] 파일명이 클래스명과 정확히 일치하는가?
- [ ] 폴더 구조가 의미적으로 일관되고 논리적인가?
- [ ] 폴더 깊이가 4단계를 초과하지 않는가?
- [ ] 한 폴더 내 파일 수가 적절한가? (5-15개 권장)

#### Code Quality
- [ ] 모든 메서드가 완전히 구현되어 있는가?
- [ ] 예외 상황이 적절히 처리되고 있는가?
- [ ] 중첩 깊이가 3단계 이하인가?
- [ ] 각 클래스/메서드가 단일 책임을 가지는가?
- [ ] 확장 가능한 구조로 설계되어 있는가?
- [ ] 의미 있는 변수명과 메서드명을 사용하는가?
- [ ] null 안전성이 보장되는가?
- [ ] 리소스가 적절히 해제되는가?

#### Architectural Compliance
- [ ] 레이어 간 의존성이 올바른 방향인가?
- [ ] 순환 참조가 없는가?
- [ ] 인터페이스와 구현체가 적절히 분리되어 있는가?
- [ ] DI 컨테이너를 통한 의존성 주입이 가능한가?

#### Extensibility Check
- [ ] 새로운 기능 추가 시 기존 코드 수정이 불필요한가?
- [ ] Switch-case나 if-else 체인으로 확장 처리하지 않았는가?
- [ ] Registry, Factory, Strategy 등 우아한 패턴을 사용했는가?
- [ ] Attribute 기반 자동 발견이 구현되어 있는가?
- [ ] 확장점이 명확한 인터페이스로 정의되어 있는가?
- [ ] 플러그인이나 외부 확장이 가능한 구조인가?

#### Refactoring & Improvement
- [ ] 기능 구현 완료 후 리팩토링을 수행했는가?
- [ ] 코드 냄새(Code Smell)가 제거되었는가?
- [ ] 리팩토링 후 모든 테스트가 통과하는가?
- [ ] 기술 부채가 문서화되고 관리되고 있는가?
- [ ] 정기적인 코드 품질 개선 계획이 있는가?
- [ ] 성능 최적화가 필요한 부분이 식별되었는가?

## Important Notes

- **현재 상태**: 문서화 및 설계 단계, 실행 가능한 코드 없음
- **언어**: 설계 문서는 한국어, 코드는 영어로 작성 예정
- **아키텍처 우선**: 확장성과 유지보수성을 위한 견고한 아키텍처 설계 중점
- **표준 준수**: MCP(Model Context Protocol) 표준 준수 계획
- **품질 우선**: 완성된 형태의 고품질 코드만 작성
- **점진적 개선**: 기능 구현 후 지속적인 리팩토링을 통한 코드 품질 향상