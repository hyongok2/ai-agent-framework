# Development Guide

Universal development methodology and code quality guidelines for all projects.

## Core Development Principles

### Code Quality Standards

#### C# Conventions & Clean Code Principles
- **C# Conventions**: Microsoft C# 코딩 규칙 준수 (PascalCase for public members, camelCase for private fields, etc.)
- **Clean Code**: 의미 있는 변수명, 함수명 사용. 코드 자체가 문서가 되도록 작성
- **Single Responsibility Principle (SRP)**: 클래스와 메서드는 단일 책임만 가져야 함
- **Minimal Nesting**: 조건문/반복문 중첩 깊이 최소화 (Early Return, Guard Clauses 활용)
- **No Temporary Code**: 임시 코드 작성 금지 (`return true;`, `throw new NotImplementedException();` 등)
- **Complete Implementation**: 모든 코드는 완성된 형태로 작성. 의미 있는 로직과 예외 처리 포함

#### Modern Language Features
- **Latest Language Features**: 최신 언어 기능 적극 활용 (C# 11+, Pattern Matching, Records, Init-only properties)
- **Nullable Reference Types**: 활성화하여 null 안전성 확보
- **Expression-bodied Members**: 간단한 메서드/속성은 expression body 사용
- **Resource Management**: using 선언, IDisposable 적절한 활용

## Architecture Principles

### SOLID Principles
- **Single Responsibility**: 각 클래스/메서드는 하나의 명확한 책임만 가짐
- **Open/Closed**: 확장에는 열려있고 수정에는 닫혀있는 설계
- **Liskov Substitution**: 파생 클래스는 기반 클래스를 완전히 대체 가능해야 함
- **Interface Segregation**: 작은 단위의 인터페이스 정의 (클라이언트는 불필요한 인터페이스에 의존하지 않음)
- **Dependency Inversion**: 추상화에 의존하고 구체화에 의존하지 않음

### Design Patterns for Extensibility
- **Strategy Pattern**: 알고리즘/행동 변경 시 사용
- **Factory Pattern**: 객체 생성 로직 확장 시 사용
- **Registry Pattern**: 동적 등록/발견이 필요한 요소에 사용
- **Plugin Architecture**: 메타데이터 기반 자동 발견
- **Command Pattern**: 실행 가능한 작업들의 확장
- **Chain of Responsibility**: 처리 단계의 유연한 확장

### Dependency Injection
- **DI 컨테이너**: 의존성 주입 컨테이너 기반 관리
- **Constructor Injection**: 생성자를 통한 의존성 주입 우선
- **Interface-based Dependencies**: 구체 클래스보다 인터페이스에 의존
- **Service Lifetime**: Singleton, Scoped, Transient 적절한 선택

## File Organization

### One Class Per File Rule
- **한 파일 = 한 클래스**: 모든 클래스는 독립된 파일에 작성
- **파일명 = 클래스명**: `UserService.cs` → `public class UserService`
- **인터페이스 별도 파일**: `IUserService.cs` → `public interface IUserService`
- **예외**: 단순 DTO, Enum, 밀접한 관련이 있는 작은 Helper 클래스만 예외적으로 허용

### Folder Structure Guidelines
- **의미적 일관성**: 폴더명은 포함된 클래스들의 책임/도메인을 명확히 표현
- **적절한 그룹핑**: 관련 기능별로 논리적 그룹화 (5-15개 파일이 적절)
- **과도한 계층화 방지**: 최대 3-4단계 깊이 유지 (`src/Project/Domain/Feature/` 수준)
- **수평적 분리 우선**: 깊이보다는 폭으로 확장
- **명명 일관성**: 비슷한 역할의 폴더는 동일한 명명 패턴 유지

### Examples of Good vs Bad Structure
✅ **Good Structure:**
```
Services/
├── User/
│   ├── UserService.cs
│   ├── IUserService.cs
│   └── UserValidator.cs
├── Payment/
│   ├── PaymentService.cs
│   └── PaymentProcessor.cs
└── Notification/
    ├── EmailService.cs
    └── SmsService.cs
```

❌ **Bad Structure:**
```
Services/
├── Core/
│   └── Base/
│       └── Abstract/
│           └── Foundation/
│               └── BaseService.cs  # Too deep!
└── AllServices.cs  # Multiple classes in one file!
```

## Elegant Extensibility Guidelines

### 🚫 Avoid Anti-Patterns
```csharp
// ❌ BAD: Hard-coded switch statements
public void ProcessType(string type)
{
    switch (type)
    {
        case "TypeA": /* logic */; break;
        case "TypeB": /* logic */; break;
        case "TypeC": /* logic */; break;
        // Adding new type requires modifying this code!
    }
}

// ❌ BAD: If-else chains
public IProcessor CreateProcessor(string processorType)
{
    if (processorType == "Fast") return new FastProcessor();
    else if (processorType == "Secure") return new SecureProcessor();
    else if (processorType == "Reliable") return new ReliableProcessor();
    // Adding new processor requires modifying this method!
}
```

### ✅ Use Elegant Patterns
```csharp
// ✅ GOOD: Registry Pattern
public interface IProcessorRegistry
{
    void Register<T>() where T : IProcessor;
    IProcessor Create(string processorType);
    IEnumerable<IProcessor> GetAll();
}

// ✅ GOOD: Factory with Auto-Discovery
public class ProcessorFactory
{
    private readonly Dictionary<string, Func<IProcessor>> _processors;
    
    public ProcessorFactory()
    {
        // Auto-discover processors via reflection/attributes
        _processors = DiscoverProcessors();
    }
    
    public IProcessor Create(string processorType) =>
        _processors.TryGetValue(processorType, out var factory) 
            ? factory() 
            : throw new UnsupportedProcessorException(processorType);
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

### Extensibility Implementation Guidelines

1. **Attribute-Based Discovery**: Use attributes for metadata-driven extension
```csharp
[Processor("Fast", Priority = 100)]
public class FastProcessor : IProcessor { }

[Service("UserService", Lifetime = ServiceLifetime.Scoped)]
public class UserService : IUserService { }
```

2. **Interface-Based Contracts**: Define clear contracts for extensions
```csharp
public interface IProcessor
{
    string Name { get; }
    string Description { get; }
    int Priority { get; }
    Task<ProcessResult> ProcessAsync(ProcessContext context);
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

## Error Handling & Code Quality

### Exception Handling
- **Custom Exceptions**: 도메인별 예외 클래스 정의. 의미 있는 에러 메시지 제공
- **No Exception Swallowing**: 예외를 무시하지 않음. 적절한 처리 또는 재전파
- **Retry Policies**: 일시적 실패에 대한 지수 백오프를 통한 재시도
- **Fallback Strategies**: 장애 시 명확한 대체 방안 구현
- **Input Validation**: 모든 외부 입력에 대한 철저한 검증

### Code Quality Enforcement
- **Static Analysis**: 코드 분석 도구 사용 (SonarQube, StyleCop, ESLint 등)
- **Code Reviews**: 모든 코드 변경사항에 대한 리뷰 필수
- **Performance Considerations**: 메모리 누수, 불필요한 할당 최소화
- **Resource Management**: 리소스 해제 패턴 준수 (using, IDisposable)
- **Thread Safety**: 멀티스레드 환경에서의 안전성 고려
- **Immutable Objects**: 가능한 한 불변 객체 사용 (Records, readonly fields)

## Iterative Development & Refactoring

### Development Approach
- **Function First**: 기능 우선 구현으로 빠른 동작하는 버전 확보
- **Refactor Early & Often**: 어느 정도 구현되면 즉시 리팩토링 시작
- **Incremental Improvement**: 작은 단위로 지속적인 코드 품질 개선
- **Technical Debt Management**: 의식적으로 기술 부채를 관리하고 해결

### Refactoring Strategy
1. **Green Phase**: 기능이 동작하는 코드 작성 (테스트 통과)
2. **Refactor Phase**: 코드 구조 개선 (기능 변경 없이)
3. **Validate Phase**: 리팩토링 후 테스트 재실행 확인
4. **Repeat**: 지속적인 개선 사이클 반복

### When to Refactor
- **Feature Complete**: 기본 기능 구현 완료 시점
- **Code Smell Detection**: 코드 냄새 감지 즉시
- **Performance Issues**: 성능 문제 발견 시
- **Extensibility Needs**: 확장성 개선 필요 시점
- **Regular Intervals**: 정기적인 코드 품질 점검 시

### Refactoring Example
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

### Continuous Improvement Practices
- **Daily Refactoring**: 매일 작은 개선사항 적용
- **Code Review Focus**: 리뷰 시 리팩토링 기회 적극 발굴
- **Technical Debt Tracking**: 기술 부채 항목 추적 및 관리
- **Refactoring Sprint**: 주기적인 리팩토링 전용 스프린트 실시

## Testing Strategy

### Testing Pyramid
- **Unit Tests**: 개별 컴포넌트 테스트 (빠르고 많은 수)
- **Integration Tests**: 컴포넌트 간 상호작용 테스트 (중간)
- **E2E Tests**: 전체 시스템 테스트 (느리고 적은 수)

### Testing Best Practices
- **Test-Driven Development**: 가능한 한 테스트 우선 개발
- **Meaningful Test Names**: 테스트 이름이 의도를 명확히 표현
- **Arrange-Act-Assert**: 테스트 구조 일관성 유지
- **Test Coverage**: 80% 이상 목표, 중요한 비즈니스 로직 우선
- **Mock Frameworks**: 외부 의존성 격리 (Moq, NSubstitute 등)

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

## Code Review Checklist

### File & Structure Organization
- [ ] 한 파일에 한 개의 클래스만 정의되어 있는가?
- [ ] 파일명이 클래스명과 정확히 일치하는가?
- [ ] 폴더 구조가 의미적으로 일관되고 논리적인가?
- [ ] 폴더 깊이가 4단계를 초과하지 않는가?
- [ ] 한 폴더 내 파일 수가 적절한가? (5-15개 권장)

### Code Quality
- [ ] 모든 메서드가 완전히 구현되어 있는가?
- [ ] 예외 상황이 적절히 처리되고 있는가?
- [ ] 중첩 깊이가 3단계 이하인가?
- [ ] 각 클래스/메서드가 단일 책임을 가지는가?
- [ ] 확장 가능한 구조로 설계되어 있는가?
- [ ] 의미 있는 변수명과 메서드명을 사용하는가?
- [ ] null 안전성이 보장되는가?
- [ ] 리소스가 적절히 해제되는가?

### Architectural Compliance
- [ ] 레이어 간 의존성이 올바른 방향인가?
- [ ] 순환 참조가 없는가?
- [ ] 인터페이스와 구현체가 적절히 분리되어 있는가?
- [ ] DI 컨테이너를 통한 의존성 주입이 가능한가?

### Extensibility Check
- [ ] 새로운 기능 추가 시 기존 코드 수정이 불필요한가?
- [ ] Switch-case나 if-else 체인으로 확장 처리하지 않았는가?
- [ ] Registry, Factory, Strategy 등 우아한 패턴을 사용했는가?
- [ ] Attribute 기반 자동 발견이 구현되어 있는가?
- [ ] 확장점이 명확한 인터페이스로 정의되어 있는가?
- [ ] 플러그인이나 외부 확장이 가능한 구조인가?

### Refactoring & Improvement
- [ ] 기능 구현 완료 후 리팩토링을 수행했는가?
- [ ] 코드 냄새(Code Smell)가 제거되었는가?
- [ ] 리팩토링 후 모든 테스트가 통과하는가?
- [ ] 기술 부채가 문서화되고 관리되고 있는가?
- [ ] 정기적인 코드 품질 개선 계획이 있는가?
- [ ] 성능 최적화가 필요한 부분이 식별되었는가?

## Performance & Security

### Performance Guidelines
- **Async/Await**: 모든 I/O 작업은 비동기 처리
- **Memory Management**: 불필요한 객체 할당 최소화
- **Caching Strategy**: 적절한 캐싱 전략 적용
- **Database Optimization**: N+1 쿼리 문제 방지, 인덱싱 고려
- **Profiling**: 성능 측정 도구 활용한 최적화

### Security Best Practices
- **Input Validation**: 모든 외부 입력 검증
- **SQL Injection Prevention**: 파라미터화 쿼리 사용
- **XSS Protection**: 출력 인코딩, CSP 헤더
- **Authentication & Authorization**: 적절한 인증/인가 체계
- **Sensitive Data**: 민감 정보 로깅/저장 방지
- **HTTPS**: 모든 통신에 HTTPS 사용

## Documentation

### Code Documentation
- **XML Documentation**: Public API에 대한 XML 문서화
- **README Files**: 프로젝트 설정 및 사용법
- **Architecture Documentation**: 주요 설계 결정사항 기록
- **API Documentation**: REST API 문서화 (OpenAPI/Swagger)

### Comments Guidelines
- **What vs Why**: 코드가 무엇을 하는지보다 왜 하는지 설명
- **Self-Documenting Code**: 코드 자체가 문서가 되도록 작성
- **Update Comments**: 코드 변경 시 주석도 함께 업데이트
- **Avoid Obvious Comments**: 자명한 내용은 주석으로 작성하지 않음

---

## Summary

이 가이드는 모든 프로젝트에서 공통으로 적용할 수 있는 개발 방법론과 코드 품질 기준을 제시합니다. 

**핵심 원칙**:
1. **완성된 코드만 작성** (임시 코드 금지)
2. **기능 구현 → 즉시 리팩토링** (점진적 개선)
3. **우아한 확장성** (조건문 분기 대신 패턴 활용)
4. **단일 책임 원칙** (SRP 엄격 준수)
5. **한 파일 = 한 클래스** (명확한 구조)

이러한 원칙들을 준수하여 **유지보수 가능하고 확장 가능한 고품질 코드**를 작성하는 것이 목표입니다.