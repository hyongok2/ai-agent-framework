# AI Agent Framework - Claude Development Guidelines

## 🎯 프로젝트 비전

**실제 프로덕션 환경에서 사용 가능한 엔터프라이즈급 AI Agent 오케스트레이션 플랫폼 구축**

### 핵심 가치
- **타입 안전성**: 컴파일 타임 검증으로 런타임 오류 최소화
- **확장성**: 플러그인 시스템으로 무제한 확장
- **관찰성**: 모든 작업 추적 및 모니터링 가능
- **복원력**: 장애 상황에서도 안정적 작동
- **우아함**: 클린 아키텍처와 SOLID 원칙 준수

## 📋 현재 상태

### 🎉 현재 완성도: 95% (프로덕션 레디)
**✅ Phase 1-6 완료**: 엔터프라이즈급 AI Agent 오케스트레이션 플랫폼 완성

### ✅ 달성된 주요 기능
- **타입 안전한 오케스트레이션**: 컴파일 타임 검증으로 런타임 오류 최소화
- **다중 LLM 지원**: Claude, OpenAI, 커스텀 Provider 완성
- **분산 상태 관리**: Redis, InMemory Provider 구현
- **확장 가능한 도구 시스템**: WebSearch, FileSystem, Database 도구
- **통합 모니터링**: 텔레메트리, 메트릭, 헬스체크 완성
- **포괄적 테스팅**: 통합 테스트, 성능 테스트 완성
- **완전한 문서화**: API 문서, 가이드, README 완성

### 🚀 프로덕션 준비 완료
- **빌드 상태**: 13개 프로젝트 모두 성공 (오류 0개)
- **테스트 상태**: 15개 테스트 모두 통과 (통합 + 성능)
- **문서화**: API Reference, Quick Start Guide, README 완성
- **성능 검증**: 모든 성능 기준 달성

## 📚 향후 개선 및 확장 계획

### 🔮 로드맵 
- **Phase 7**: 플러그인 생태계 및 마켓플레이스
- **Phase 8**: 엔터프라이즈 보안 및 규정 준수  
- **Phase 9**: OpenTelemetry 통합, 고급 모니터링
- **Phase 10**: MCP 프로토콜 완전 구현

## 🏛️ 아키텍처 원칙

### SOLID 원칙 엄격 적용

#### Single Responsibility Principle (SRP)
- 각 클래스는 단일 책임만 가짐
- PromptLoader: 프롬프트 로딩만 담당
- PromptProcessor: 프롬프트 처리만 담당
- PromptCache: 프롬프트 캐싱만 담당

#### Open/Closed Principle (OCP)
- 확장에는 열려있고 수정에는 닫혀있음
- OrchestrationStrategyBase 추상 클래스 제공
- 새로운 전략 추가 시 기존 코드 수정 불필요

#### Liskov Substitution Principle (LSP)
- 모든 하위 클래스가 상위 클래스를 완벽히 대체 가능
- ToolBase 추상 클래스로 공통 동작 보장
- 예외 처리 및 검증 로직 통일

#### Interface Segregation Principle (ISP)
- 클라이언트가 필요 없는 인터페이스에 의존하지 않도록 분리
- IExecutable, IValidatable, IDescriptive, ICacheable 등 역할별 인터페이스
- 필요한 인터페이스만 선택적 구현

#### Dependency Inversion Principle (DIP)
- 고수준 모듈이 구체 구현이 아닌 추상화에 의존
- 모든 의존성을 인터페이스로 주입
- 테스트 가능성 및 유연성 향상

## 🎨 클린 코드 원칙

### 핵심 원칙
- **의미 있는 이름**: 의도가 명확한 클래스/메서드명 사용
- **작고 단일 기능**: 함수는 한 가지 일만 수행
- **코드로 의도 표현**: 주석보다 코드 자체가 설명적
- **예외 활용**: 리턴 코드 대신 예외로 에러 처리

## 📂 프로젝트 폴더 구조

### 폴더 구조 설계 원칙

1. **1 Class = 1 File**: 모든 클래스는 독립된 파일
2. **의미적 그룹핑**: 관련 기능별 폴더 분류
3. **깊이 제한**: 최대 4단계 깊이까지만 허용
4. **명확한 네이밍**: 폴더명으로 역할 명확히 표현
5. **Base 클래스 분리**: 추상 클래스는 Base 폴더에 격리

### 마이그레이션 전략
1. 폴더 구조 생성 (빈 폴더)
2. 파일 하나씩 이동 → 즉시 빌드 → 네임스페이스 수정
3. 각 단계마다 전체 솔루션 빌드 확인
4. 모든 참조 프로젝트 네임스페이스 업데이트

## 📝 Task Management 가이드

### Task 관리 원칙
- **진행 상태 체크 필수**: 모든 작업은 체크 표시로 진행 상태를 명확히 추적
- **한 번에 하나씩**: 단일 작업에 집중하여 완료 후 다음 단계 진행
- **검증 후 완료**: 빌드 성공, 테스트 통과 확인 후 완료 표시
- **문서화**: 주요 변경사항은 README.md와 API 문서에 반영

### 작업 진행 템플릿
```markdown
### Phase X: 작업명
- [ ] 작업 1 설명
- [ ] 작업 2 설명
- [ ] 작업 3 설명

#### 완료 검증 기준
- [ ] 모든 빌드 성공 (dotnet build)
- [ ] 테스트 통과 (dotnet test) 
- [ ] 코드 리뷰 완료
- [ ] 문서 업데이트
```

## 🔧 코딩 스타일 가이드

### C# 코딩 컨벤션 준수
참조: `.kiro/steering/dotnet-coding-standards.md`

### 추가 품질 규칙

#### Nullable Reference Types 활용
```csharp
#nullable enable

public class OrchestrationContext
{
    public string SessionId { get; } = null!; // 생성자에서 초기화됨을 보장
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; set; } // null 가능
    public string? LastErrorMessage { get; set; } // null 가능
    
    public OrchestrationContext(string sessionId)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(string? finalMessage = null)
    {
        CompletedAt = DateTime.UtcNow;
        LastErrorMessage = finalMessage; // null 명시적 허용
    }
}
```

#### Record Types 활용
```csharp
// 불변 데이터 모델
public sealed record TokenUsage(int PromptTokens, int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal EstimatedCost => TotalTokens * 0.0001m; // 예시 가격
}

public sealed record LLMRequest(
    string Prompt,
    string Model,
    int MaxTokens,
    decimal Temperature = 0.7m,
    CancellationToken CancellationToken = default)
{
    public static LLMRequest Create(string prompt, string model = "gpt-4")
    {
        ArgumentException.ThrowIfNullOrEmpty(prompt);
        ArgumentException.ThrowIfNullOrEmpty(model);
        
        return new LLMRequest(prompt, model, MaxTokens: 4096);
    }
}
```

#### Pattern Matching 적극 활용
```csharp
public string GetActionDescription(IOrchestrationAction action) => action switch
{
    LLMAction llm => $"LLM 기능 실행: {llm.FunctionName}",
    ToolAction tool => $"도구 실행: {tool.ToolName}",
    DelayAction delay => $"{delay.Duration.TotalSeconds}초 대기",
    ConditionalAction conditional => $"조건부 실행: {conditional.Condition}",
    _ => $"알 수 없는 액션: {action.GetType().Name}"
};

public async Task<ActionResult> ProcessActionAsync(IOrchestrationAction action) => action switch
{
    LLMAction llm => await ExecuteLLMActionAsync(llm),
    ToolAction tool => await ExecuteToolActionAsync(tool), 
    DelayAction delay => await ExecuteDelayActionAsync(delay),
    ConditionalAction conditional when await EvaluateConditionAsync(conditional.Condition) 
        => await ProcessActionAsync(conditional.ThenAction),
    ConditionalAction conditional 
        => conditional.ElseAction != null 
            ? await ProcessActionAsync(conditional.ElseAction) 
            : ActionResult.Skipped,
    _ => throw new NotSupportedException($"지원되지 않는 액션 타입: {action.GetType().Name}")
};
```

## 📈 품질 메트릭

### 목표 지표
- **코드 커버리지**: 80% 이상
- **순환 복잡도**: 클래스당 평균 5 이하
- **유지보수성 지수**: 80점 이상
- **기술 부채 비율**: 5% 이하
- **SOLID 원칙 준수율**: 95% 이상

## 🎯 성공 기준

### 완료 조건
- [ ] 모든 하드코딩 제거 (100%)
- [ ] 타입 안전성 확보 (컴파일 타임 검증)
- [ ] SOLID 원칙 준수 (95% 이상)
- [ ] 클린 코드 원칙 적용 (코드 리뷰 통과)
- [ ] 테스트 커버리지 80% 이상
- [ ] 성능 기준 달성 (응답시간 < 2초)
- [ ] 프로덕션 배포 가능 (안정성 검증)

## 🚫 코드 품질 금지 사항

### 절대 금지되는 패턴들

#### 1. 의미 없는 리턴 값 금지
```csharp
// ❌ 절대 금지 - 의미 없는 return true
public virtual Task<bool> ValidateAsync(IToolInput input)
{
    // 실제 검증 로직 없이
    return Task.FromResult(true); // 이런 코드 절대 금지!
}

// ✅ 올바른 구현 - 실제 검증 로직
public virtual async Task<ValidationResult> ValidateAsync(IToolInput input)
{
    if (input == null)
        return ValidationResult.Failed("입력이 null입니다");
        
    if (input.Parameters == null || input.Parameters.Count == 0)
        return ValidationResult.Failed("파라미터가 없습니다");
        
    foreach (var requiredParam in Contract.RequiredParameters)
    {
        if (!input.Parameters.ContainsKey(requiredParam))
            return ValidationResult.Failed($"필수 파라미터 누락: {requiredParam}");
    }
    
    return ValidationResult.Success();
}
```

#### 2. 임시/테스트 코드 삽입 금지
```csharp
// ❌ 절대 금지 - 임시 테스트 코드
public async Task<LLMResponse> GenerateAsync(LLMRequest request)
{
    // TODO: 임시로 하드코딩
    await Task.Delay(100); // 임시 지연
    return new LLMResponse { Content = "test response" }; // 가짜 응답 금지!
}

// ✅ 올바른 구현 - 완전한 실제 구현
public async Task<LLMResponse> GenerateAsync(LLMRequest request)
{
    ValidateRequest(request);
    
    var httpRequest = CreateHttpRequest(request);
    var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
    
    httpResponse.EnsureSuccessStatusCode();
    
    var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
    return ParseResponse(responseContent);
}
```

#### 3. 불완전한 예외 처리 금지
```csharp
// ❌ 절대 금지 - 빈 catch 블록
public async Task<IToolResult> ExecuteAsync(IToolInput input)
{
    try
    {
        // 실행 로직
    }
    catch
    {
        // 빈 catch 블록 절대 금지!
    }
    
    return null; // null 반환도 금지
}

// ✅ 올바른 구현 - 완전한 예외 처리
public async Task<IToolResult> ExecuteAsync(IToolInput input)
{
    try
    {
        var result = await ExecuteInternalAsync(input);
        return ToolResult.Success(result);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "잘못된 입력 파라미터: {ToolName}", Name);
        return ToolResult.Failed($"입력 오류: {ex.Message}");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "네트워크 오류: {ToolName}", Name);
        return ToolResult.Failed($"네트워크 오류: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "도구 실행 실패: {ToolName}", Name);
        return ToolResult.Failed($"실행 실패: {ex.Message}");
    }
}

## 📋 진행중인 개발 작업 (Post Phase 6)

### 🎯 현재 우선순위
1. **LLM Provider 실제 API 통합** - Claude/OpenAI HTTP API 구현
2. **고급 오케스트레이션 패턴** - Conditional/Delay Actions
3. **엔터프라이즈 기능 확장** - 고급 모니터링, 보안

## 📋 Task Management

### Task 추적 시스템
- **TodoRead/TodoWrite**: 세션 내 작업 추적 및 진행 상황 관리
- **실시간 상태 업데이트**: 작업 완료 시 즉시 ✅ 표시로 상태 업데이트
- **우선순위 기반 작업**: Critical → High → Medium → Low 순서로 진행

### Task 상태 관리
- **pending** 📋: 작업 대기 중
- **in_progress** 🔄: 현재 진행 중 (세션당 1개만)
- **completed** ✅: 완료됨
- **blocked** 🚧: 의존성 대기 중

## 🏛️ 아키텍처 원칙

### SOLID 원칙 엄격 적용

#### Single Responsibility Principle (SRP)
- 각 클래스는 단일 책임만 가짐
- PromptLoader: 프롬프트 로딩만 담당
- PromptProcessor: 프롬프트 처리만 담당
- PromptCache: 프롬프트 캐싱만 담당

#### Open/Closed Principle (OCP)
- 확장에는 열려있고 수정에는 닫혀있음
- OrchestrationStrategyBase 추상 클래스 제공
- 새로운 전략 추가 시 기존 코드 수정 불필요

#### Liskov Substitution Principle (LSP)
- 모든 하위 클래스가 상위 클래스를 완벽히 대체 가능
- ToolBase 추상 클래스로 공통 동작 보장
- 예외 처리 및 검증 로직 통일

#### Interface Segregation Principle (ISP)
- 클라이언트가 필요 없는 인터페이스에 의존하지 않도록 분리
- IExecutable, IValidatable, IDescriptive, ICacheable 등 역할별 인터페이스
- 필요한 인터페이스만 선택적 구현

#### Dependency Inversion Principle (DIP)
- 고수준 모듈이 구체 구현이 아닌 추상화에 의존
- 모든 의존성을 인터페이스로 주입
- 테스트 가능성 및 유연성 향상

## 🎨 클린 코드 원칙

### 핵심 원칙
- **의미 있는 이름**: 의도가 명확한 클래스/메서드명 사용
- **작고 단일 기능**: 함수는 한 가지 일만 수행
- **코드로 의도 표현**: 주석보다 코드 자체가 설명적
- **예외 활용**: 리턴 코드 대신 예외로 에러 처리




## 🔧 코딩 스타일 가이드

### C# 코딩 컨벤션 준수
참조: `.kiro/steering/dotnet-coding-standards.md`


## 📈 품질 메트릭

### 목표 지표
- **코드 커버리지**: 80% 이상
- **순환 복잡도**: 클래스당 평균 5 이하
- **유지보수성 지수**: 80점 이상
- **기술 부채 비율**: 5% 이하
- **SOLID 원칙 준수율**: 95% 이상

## 🎯 성공 기준

### 완료 조건
- [ ] 모든 하드코딩 제거 (100%)
- [ ] 타입 안전성 확보 (컴파일 타임 검증)
- [ ] SOLID 원칙 준수 (95% 이상)
- [ ] 클린 코드 원칙 적용 (코드 리뷰 통과)
- [ ] 테스트 커버리지 80% 이상
- [ ] 성능 기준 달성 (응답시간 < 2초)
- [ ] 프로덕션 배포 가능 (안정성 검증)

## 🚫 코드 품질 금지 사항

### 절대 금지되는 패턴들

#### 1. 의미 없는 리턴 값 금지
```csharp
// ❌ 절대 금지 - 의미 없는 return true
public virtual Task<bool> ValidateAsync(IToolInput input)
{
    // 실제 검증 로직 없이
    return Task.FromResult(true); // 이런 코드 절대 금지!
}

// ✅ 올바른 구현 - 실제 검증 로직
public virtual async Task<ValidationResult> ValidateAsync(IToolInput input)
{
    if (input == null)
        return ValidationResult.Failed("입력이 null입니다");
        
    if (input.Parameters == null || input.Parameters.Count == 0)
        return ValidationResult.Failed("파라미터가 없습니다");
        
    foreach (var requiredParam in Contract.RequiredParameters)
    {
        if (!input.Parameters.ContainsKey(requiredParam))
            return ValidationResult.Failed($"필수 파라미터 누락: {requiredParam}");
    }
    
    return ValidationResult.Success();
}
```

#### 2. 임시/테스트 코드 삽입 금지
```csharp
// ❌ 절대 금지 - 임시 테스트 코드
public async Task<LLMResponse> GenerateAsync(LLMRequest request)
{
    // TODO: 임시로 하드코딩
    await Task.Delay(100); // 임시 지연
    return new LLMResponse { Content = "test response" }; // 가짜 응답 금지!
}

// ✅ 올바른 구현 - 완전한 실제 구현
public async Task<LLMResponse> GenerateAsync(LLMRequest request)
{
    ValidateRequest(request);
    
    var httpRequest = CreateHttpRequest(request);
    var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
    
    httpResponse.EnsureSuccessStatusCode();
    
    var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
    return ParseResponse(responseContent);
}
```

#### 3. 불완전한 예외 처리 금지
```csharp
// ❌ 절대 금지 - 빈 catch 블록
public async Task<IToolResult> ExecuteAsync(IToolInput input)
{
    try
    {
        // 실행 로직
    }
    catch
    {
        // 빈 catch 블록 절대 금지!
    }
    
    return null; // null 반환도 금지
}

// ✅ 올바른 구현 - 완전한 예외 처리
public async Task<IToolResult> ExecuteAsync(IToolInput input)
{
    try
    {
        var result = await ExecuteInternalAsync(input);
        return ToolResult.Success(result);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "잘못된 입력 파라미터: {ToolName}", Name);
        return ToolResult.Failed($"입력 오류: {ex.Message}");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "네트워크 오류: {ToolName}", Name);
        return ToolResult.Failed($"네트워크 오류: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "도구 실행 실패: {ToolName}", Name);
        return ToolResult.Failed($"실행 실패: {ex.Message}");
    }
}
```

#### 4. 하드코딩된 값 금지
```csharp
// ❌ 절대 금지 - 하드코딩된 설정값
public class ClaudeProvider : ILLMProvider
{
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"); // 하드코딩 금지!
        httpRequest.Headers.Add("anthropic-version", "2023-06-01"); // 하드코딩 금지!
        httpRequest.Headers.Add("x-api-key", "sk-ant-api03-..."); // 절대 금지!
    }
}

// ✅ 올바른 구현 - 설정 기반
public class ClaudeProvider : ILLMProvider
{
    private readonly ClaudeOptions _options;
    private readonly HttpClient _httpClient;
    
    public ClaudeProvider(IOptions<ClaudeOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }
    
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/messages");
        httpRequest.Headers.Add("anthropic-version", _options.ApiVersion);
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        
        // 실제 구현...
    }
}
```

#### 5. Magic Number/String 금지
```csharp
// ❌ 절대 금지 - Magic Number/String
public class TokenBudgetManager
{
    public async Task<bool> CanUseTokensAsync(int requestedTokens)
    {
        var dailyUsage = await GetDailyUsageAsync();
        return dailyUsage + requestedTokens <= 10000; // Magic Number 금지!
    }
    
    public async Task RecordUsageAsync(string model, int tokens)
    {
        if (model == "gpt-4") // Magic String 금지!
        {
            // 처리 로직
        }
    }
}

// ✅ 올바른 구현 - 상수 및 설정 사용
public class TokenBudgetManager
{
    private const int DEFAULT_DAILY_TOKEN_LIMIT = 10_000;
    private const int DEFAULT_HOURLY_TOKEN_LIMIT = 1_000;
    
    private readonly TokenLimits _limits;
    
    public TokenBudgetManager(IOptions<TokenLimits> limits)
    {
        _limits = limits.Value;
    }
    
    public async Task<bool> CanUseTokensAsync(int requestedTokens)
    {
        var dailyUsage = await GetDailyUsageAsync();
        var dailyLimit = _limits.DailyLimit ?? DEFAULT_DAILY_TOKEN_LIMIT;
        
        return dailyUsage + requestedTokens <= dailyLimit;
    }
    
    public async Task RecordUsageAsync(string model, int tokens)
    {
        var modelConfig = _limits.ModelConfigurations
            .FirstOrDefault(c => c.ModelName.Equals(model, StringComparison.OrdinalIgnoreCase));
            
        if (modelConfig != null)
        {
            // 모델별 처리 로직
        }
    }
}
```

### 코드 완성도 요구사항

1. **완전한 구현**: 임시 코드, TODO 주석 금지
2. **단일 파일 원칙**: 1 Class = 1 File 엄격 준수
3. **의미 있는 반환값**: 구체적인 결과 객체 사용
4. **완전한 검증**: null 체크, 비즈니스 규칙, 의미 있는 오류 메시지

## 📁 폴더 구조 엄격 규칙

### 필수 준수사항
1. **최대 깊이 4레벨**: `src/Project/Category/Subcategory/`
2. **의미적 그룹핑**: 관련 기능끼리 묶기
3. **Base 클래스 격리**: 추상 클래스는 `Base/` 폴더
4. **파일명 = 클래스명**: 정확히 일치
5. **폴더당 최대 7개 파일**: 초과 시 하위 폴더 생성

이 가이드라인을 준수하여 **엔터프라이즈급 AI Agent 플랫폼**을 완성합니다.