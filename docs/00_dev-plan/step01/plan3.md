# Plan 3: 공통 인프라 구축

## 📋 개요

**목표**: 전체 시스템이 의존할 공통 기능 완성  
**예상 소요 시간**: 1일 (8시간)  
**의존성**: Plan 2 (핵심 인터페이스 및 모델) 완료

## 🎯 구체적 목표

1. ✅ **설정 관리 시스템** 완성
2. ✅ **구조화된 로깅 시스템** 구축  
3. ✅ **입력 검증 프레임워크** 구현
4. ✅ **유용한 확장 메서드** 라이브러리 완성

## 🏗️ 작업 단계

### **Task 3.1: 설정 관리 시스템** (2시간)

#### **AIAgent.Common/Configuration/ 구조**
```
src/AIAgent.Common/Configuration/
├── IConfigurationManager.cs      # 설정 관리 인터페이스
├── ConfigurationManager.cs       # 설정 관리 구현
├── ConfigurationExtensions.cs    # IConfiguration 확장
├── AgentSettings.cs              # 메인 설정 모델
├── LLMProviderSettings.cs        # LLM Provider 설정
├── ToolSettings.cs               # Tool 설정
└── ValidationSettings.cs         # 검증 설정
```

#### **IConfigurationManager.cs 구현**
```csharp
namespace AIAgent.Common.Configuration;

/// <summary>
/// 설정 관리자 인터페이스입니다.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// 설정값을 가져옵니다.
    /// </summary>
    /// <typeparam name="T">설정값 타입</typeparam>
    /// <param name="key">설정 키</param>
    /// <returns>설정값</returns>
    T GetValue<T>(string key);
    
    /// <summary>
    /// 설정값을 가져옵니다. 없으면 기본값을 반환합니다.
    /// </summary>
    /// <typeparam name="T">설정값 타입</typeparam>
    /// <param name="key">설정 키</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>설정값 또는 기본값</returns>
    T GetValue<T>(string key, T defaultValue);
    
    /// <summary>
    /// 설정 섹션을 바인딩합니다.
    /// </summary>
    /// <typeparam name="T">바인딩할 타입</typeparam>
    /// <param name="sectionName">섹션 이름</param>
    /// <returns>바인딩된 설정 객체</returns>
    T GetSection<T>(string sectionName) where T : class, new();
    
    /// <summary>
    /// 설정이 변경되었는지 확인합니다.
    /// </summary>
    /// <param name="key">설정 키</param>
    /// <returns>변경 여부</returns>
    bool HasChanged(string key);
    
    /// <summary>
    /// 설정 변경을 모니터링합니다.
    /// </summary>
    /// <typeparam name="T">설정값 타입</typeparam>
    /// <param name="sectionName">섹션 이름</param>
    /// <param name="onChanged">변경 시 호출될 콜백</param>
    /// <returns>변경 모니터링 구독</returns>
    IDisposable MonitorChanges<T>(string sectionName, Action<T> onChanged) where T : class, new();
}
```

#### **AgentSettings.cs 구현**
```csharp
namespace AIAgent.Common.Configuration;

/// <summary>
/// AI Agent의 메인 설정을 나타냅니다.
/// </summary>
public record AgentSettings
{
    /// <summary>
    /// 에이전트 이름입니다.
    /// </summary>
    public string Name { get; init; } = "AI Agent";
    
    /// <summary>
    /// 에이전트 버전입니다.
    /// </summary>
    public string Version { get; init; } = "1.0.0";
    
    /// <summary>
    /// 기본 타임아웃 시간(초)입니다.
    /// </summary>
    public int DefaultTimeoutSeconds { get; init; } = 30;
    
    /// <summary>
    /// 최대 동시 실행 수입니다.
    /// </summary>
    public int MaxConcurrentExecutions { get; init; } = 10;
    
    /// <summary>
    /// 대화 이력 최대 보관 수입니다.
    /// </summary>
    public int MaxConversationHistory { get; init; } = 50;
    
    /// <summary>
    /// LLM Provider 설정입니다.
    /// </summary>
    public LLMProviderSettings LLMProvider { get; init; } = new();
    
    /// <summary>
    /// Tool 설정입니다.
    /// </summary>
    public ToolSettings Tools { get; init; } = new();
    
    /// <summary>
    /// 로깅 설정입니다.
    /// </summary>
    public LoggingSettings Logging { get; init; } = new();
    
    /// <summary>
    /// 성능 설정입니다.
    /// </summary>
    public PerformanceSettings Performance { get; init; } = new();
}

/// <summary>
/// LLM Provider 설정을 나타냅니다.
/// </summary>
public record LLMProviderSettings
{
    /// <summary>
    /// 기본 Provider 이름입니다.
    /// </summary>
    public string DefaultProvider { get; init; } = "OpenAI";
    
    /// <summary>
    /// Provider별 설정입니다.
    /// </summary>
    public Dictionary<string, ProviderConfig> Providers { get; init; } = new()
    {
        ["OpenAI"] = new ProviderConfig
        {
            Enabled = true,
            Model = "gpt-4",
            MaxTokens = 4096,
            Temperature = 0.7
        }
    };
}

/// <summary>
/// Provider 개별 설정을 나타냅니다.
/// </summary>
public record ProviderConfig
{
    /// <summary>
    /// 사용 여부입니다.
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// 모델 이름입니다.
    /// </summary>
    public string Model { get; init; } = string.Empty;
    
    /// <summary>
    /// API 키입니다.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;
    
    /// <summary>
    /// 최대 토큰 수입니다.
    /// </summary>
    public int MaxTokens { get; init; } = 4096;
    
    /// <summary>
    /// Temperature 설정입니다.
    /// </summary>
    public double Temperature { get; init; } = 0.7;
    
    /// <summary>
    /// 요청 타임아웃(초)입니다.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
    
    /// <summary>
    /// 재시도 횟수입니다.
    /// </summary>
    public int MaxRetries { get; init; } = 3;
}
```

#### **ConfigurationExtensions.cs 구현**
```csharp
namespace AIAgent.Common.Configuration;

/// <summary>
/// IConfiguration에 대한 확장 메서드를 제공합니다.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 안전하게 설정값을 가져옵니다.
    /// </summary>
    /// <typeparam name="T">설정값 타입</typeparam>
    /// <param name="configuration">설정 인스턴스</param>
    /// <param name="key">설정 키</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>설정값 또는 기본값</returns>
    public static T GetValueSafe<T>(this IConfiguration configuration, string key, T defaultValue)
    {
        try
        {
            var value = configuration.GetValue<T>(key);
            return value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// 필수 설정값을 가져옵니다. 없으면 예외를 발생시킵니다.
    /// </summary>
    /// <param name="configuration">설정 인스턴스</param>
    /// <param name="key">설정 키</param>
    /// <returns>설정값</returns>
    /// <exception cref="ConfigurationException">필수 설정이 없을 때</exception>
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            throw new ConfigurationException($"Required configuration value '{key}' is missing or empty.");
        }
        return value;
    }
    
    /// <summary>
    /// 설정 섹션을 검증하여 바인딩합니다.
    /// </summary>
    /// <typeparam name="T">바인딩할 타입</typeparam>
    /// <param name="configuration">설정 인스턴스</param>
    /// <param name="sectionName">섹션 이름</param>
    /// <returns>바인딩된 설정 객체</returns>
    public static T GetValidatedSection<T>(this IConfiguration configuration, string sectionName)
        where T : class, new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        
        // 검증 수행
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(section);
        
        if (!Validator.TryValidateObject(section, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new ConfigurationException($"Configuration section '{sectionName}' is invalid: {errors}");
        }
        
        return section;
    }
}
```

### **Task 3.2: 구조화된 로깅 시스템** (2시간)

#### **AIAgent.Common/Logging/ 구조**
```
src/AIAgent.Common/Logging/
├── IStructuredLogger.cs          # 구조화된 로깅 인터페이스
├── StructuredLogger.cs           # 구조화된 로깅 구현
├── LoggingExtensions.cs          # ILogger 확장
├── LogCorrelation.cs            # 상관관계 ID 관리
├── LogEventIds.cs               # 이벤트 ID 정의
└── LoggingSettings.cs           # 로깅 설정
```

#### **IStructuredLogger.cs 구현**
```csharp
namespace AIAgent.Common.Logging;

/// <summary>
/// 구조화된 로깅 인터페이스입니다.
/// </summary>
public interface IStructuredLogger
{
    /// <summary>
    /// 정보 로그를 기록합니다.
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    /// <param name="message">메시지</param>
    /// <param name="properties">추가 속성</param>
    void LogInfo(EventId eventId, string message, object? properties = null);
    
    /// <summary>
    /// 경고 로그를 기록합니다.
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    /// <param name="message">메시지</param>
    /// <param name="properties">추가 속성</param>
    void LogWarning(EventId eventId, string message, object? properties = null);
    
    /// <summary>
    /// 오류 로그를 기록합니다.
    /// </summary>
    /// <param name="eventId">이벤트 ID</param>
    /// <param name="exception">예외</param>
    /// <param name="message">메시지</param>
    /// <param name="properties">추가 속성</param>
    void LogError(EventId eventId, Exception exception, string message, object? properties = null);
    
    /// <summary>
    /// 성능 로그를 기록합니다.
    /// </summary>
    /// <param name="operation">작업 이름</param>
    /// <param name="duration">실행 시간</param>
    /// <param name="properties">추가 속성</param>
    void LogPerformance(string operation, TimeSpan duration, object? properties = null);
    
    /// <summary>
    /// 사용자 활동 로그를 기록합니다.
    /// </summary>
    /// <param name="userId">사용자 ID</param>
    /// <param name="action">활동</param>
    /// <param name="properties">추가 속성</param>
    void LogUserActivity(string userId, string action, object? properties = null);
    
    /// <summary>
    /// LLM 호출 로그를 기록합니다.
    /// </summary>
    /// <param name="provider">Provider 이름</param>
    /// <param name="model">모델 이름</param>
    /// <param name="tokenCount">토큰 수</param>
    /// <param name="duration">응답 시간</param>
    /// <param name="properties">추가 속성</param>
    void LogLLMCall(string provider, string model, int tokenCount, TimeSpan duration, object? properties = null);
}
```

#### **LogCorrelation.cs 구현**
```csharp
namespace AIAgent.Common.Logging;

/// <summary>
/// 로그 상관관계 ID를 관리합니다.
/// </summary>
public static class LogCorrelation
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    
    /// <summary>
    /// 현재 상관관계 ID를 가져옵니다.
    /// </summary>
    public static string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();
    
    /// <summary>
    /// 상관관계 ID를 설정합니다.
    /// </summary>
    /// <param name="correlationId">설정할 상관관계 ID</param>
    /// <returns>이전 상관관계 ID를 복원하는 Disposable</returns>
    public static IDisposable SetCorrelationId(string correlationId)
    {
        var previous = _correlationId.Value;
        _correlationId.Value = correlationId;
        
        return new CorrelationScope(previous);
    }
    
    /// <summary>
    /// 새로운 상관관계 ID를 생성합니다.
    /// </summary>
    /// <returns>새로운 상관관계 ID</returns>
    public static string GenerateCorrelationId()
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        _correlationId.Value = id;
        return id;
    }
    
    private sealed class CorrelationScope : IDisposable
    {
        private readonly string? _previousId;
        private bool _disposed;
        
        public CorrelationScope(string? previousId)
        {
            _previousId = previousId;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _correlationId.Value = _previousId;
                _disposed = true;
            }
        }
    }
}
```

#### **LoggingExtensions.cs 구현**
```csharp
namespace AIAgent.Common.Logging;

/// <summary>
/// ILogger에 대한 확장 메서드를 제공합니다.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// 상관관계 ID가 포함된 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거</param>
    /// <param name="logLevel">로그 레벨</param>
    /// <param name="eventId">이벤트 ID</param>
    /// <param name="message">메시지</param>
    /// <param name="properties">추가 속성</param>
    public static void LogWithCorrelation(this ILogger logger, LogLevel logLevel, EventId eventId, 
        string message, object? properties = null)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = LogCorrelation.CorrelationId,
            ["Timestamp"] = DateTimeOffset.UtcNow
        });
        
        if (properties != null)
        {
            logger.Log(logLevel, eventId, "{Message} {@Properties}", message, properties);
        }
        else
        {
            logger.Log(logLevel, eventId, "{Message}", message);
        }
    }
    
    /// <summary>
    /// 메서드 실행을 측정하고 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="action">실행할 작업</param>
    public static void LogMethodExecution(this ILogger logger, string methodName, Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogWithCorrelation(LogLevel.Debug, LogEventIds.MethodStart, 
                "Starting method execution", new { Method = methodName });
            
            action();
            
            stopwatch.Stop();
            logger.LogWithCorrelation(LogLevel.Debug, LogEventIds.MethodComplete,
                "Method execution completed", new 
                { 
                    Method = methodName, 
                    Duration = stopwatch.ElapsedMilliseconds 
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWithCorrelation(LogLevel.Error, LogEventIds.MethodError,
                "Method execution failed", new 
                { 
                    Method = methodName, 
                    Duration = stopwatch.ElapsedMilliseconds,
                    Exception = ex.Message 
                });
            throw;
        }
    }
    
    /// <summary>
    /// 비동기 메서드 실행을 측정하고 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="func">실행할 비동기 작업</param>
    public static async Task<T> LogMethodExecutionAsync<T>(this ILogger logger, string methodName, Func<Task<T>> func)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogWithCorrelation(LogLevel.Debug, LogEventIds.MethodStart, 
                "Starting async method execution", new { Method = methodName });
            
            var result = await func();
            
            stopwatch.Stop();
            logger.LogWithCorrelation(LogLevel.Debug, LogEventIds.MethodComplete,
                "Async method execution completed", new 
                { 
                    Method = methodName, 
                    Duration = stopwatch.ElapsedMilliseconds 
                });
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWithCorrelation(LogLevel.Error, LogEventIds.MethodError,
                "Async method execution failed", new 
                { 
                    Method = methodName, 
                    Duration = stopwatch.ElapsedMilliseconds,
                    Exception = ex.Message 
                });
            throw;
        }
    }
}
```

### **Task 3.3: 입력 검증 프레임워크** (2시간)

#### **AIAgent.Common/Validation/ 구조**
```
src/AIAgent.Common/Validation/
├── IValidator.cs                 # 검증 인터페이스
├── ValidationResult.cs           # 검증 결과
├── ValidationRule.cs             # 검증 규칙 기본 클래스
├── ValidatorBase.cs              # 검증자 기본 클래스
├── InputValidationExtensions.cs  # 입력 검증 확장
├── Rules/                        # 기본 검증 규칙들
│   ├── RequiredRule.cs
│   ├── StringLengthRule.cs
│   ├── EmailRule.cs
│   └── JsonRule.cs
└── Attributes/                   # 검증 어트리뷰트들
    ├── ValidateInputAttribute.cs
    └── RequiredIfAttribute.cs
```

#### **IValidator.cs & ValidationResult.cs 구현**
```csharp
// IValidator.cs
namespace AIAgent.Common.Validation;

/// <summary>
/// 검증자 인터페이스입니다.
/// </summary>
/// <typeparam name="T">검증할 객체의 타입</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// 객체를 검증합니다.
    /// </summary>
    /// <param name="item">검증할 객체</param>
    /// <returns>검증 결과</returns>
    ValidationResult Validate(T item);
    
    /// <summary>
    /// 객체를 비동기적으로 검증합니다.
    /// </summary>
    /// <param name="item">검증할 객체</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    Task<ValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default);
}

// ValidationResult.cs
namespace AIAgent.Common.Validation;

/// <summary>
/// 검증 결과를 나타냅니다.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// 검증 성공 여부입니다.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// 검증 실패 여부입니다.
    /// </summary>
    public bool IsInvalid => !IsValid;
    
    /// <summary>
    /// 오류 메시지 목록입니다.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 경고 메시지 목록입니다.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="errors">오류 메시지 목록</param>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
    
    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="errors">오류 메시지 목록</param>
    public static ValidationResult Failure(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
    
    /// <summary>
    /// 두 검증 결과를 결합합니다.
    /// </summary>
    /// <param name="other">다른 검증 결과</param>
    /// <returns>결합된 검증 결과</returns>
    public ValidationResult Combine(ValidationResult other)
    {
        if (IsValid && other.IsValid)
            return Success();
        
        var combinedErrors = Errors.Concat(other.Errors).ToList();
        var combinedWarnings = Warnings.Concat(other.Warnings).ToList();
        
        return new ValidationResult
        {
            IsValid = false,
            Errors = combinedErrors,
            Warnings = combinedWarnings
        };
    }
}
```

#### **ValidatorBase.cs 구현**
```csharp
namespace AIAgent.Common.Validation;

/// <summary>
/// 검증자의 기본 클래스입니다.
/// </summary>
/// <typeparam name="T">검증할 객체의 타입</typeparam>
public abstract class ValidatorBase<T> : IValidator<T>
{
    private readonly List<ValidationRule<T>> _rules = new();
    
    /// <summary>
    /// 검증 규칙을 추가합니다.
    /// </summary>
    /// <param name="rule">검증 규칙</param>
    protected void AddRule(ValidationRule<T> rule)
    {
        _rules.Add(rule);
    }
    
    /// <summary>
    /// 검증 규칙을 추가합니다.
    /// </summary>
    /// <param name="predicate">검증 조건</param>
    /// <param name="errorMessage">오류 메시지</param>
    protected void AddRule(Func<T, bool> predicate, string errorMessage)
    {
        _rules.Add(new ValidationRule<T>(predicate, errorMessage));
    }
    
    /// <summary>
    /// 비동기 검증 규칙을 추가합니다.
    /// </summary>
    /// <param name="predicate">비동기 검증 조건</param>
    /// <param name="errorMessage">오류 메시지</param>
    protected void AddAsyncRule(Func<T, Task<bool>> predicate, string errorMessage)
    {
        _rules.Add(new AsyncValidationRule<T>(predicate, errorMessage));
    }
    
    /// <summary>
    /// 객체를 검증합니다.
    /// </summary>
    /// <param name="item">검증할 객체</param>
    /// <returns>검증 결과</returns>
    public virtual ValidationResult Validate(T item)
    {
        var errors = new List<string>();
        
        foreach (var rule in _rules)
        {
            if (!rule.Validate(item))
            {
                errors.Add(rule.ErrorMessage);
            }
        }
        
        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }
    
    /// <summary>
    /// 객체를 비동기적으로 검증합니다.
    /// </summary>
    /// <param name="item">검증할 객체</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    public virtual async Task<ValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        
        foreach (var rule in _rules)
        {
            var isValid = rule is AsyncValidationRule<T> asyncRule
                ? await asyncRule.ValidateAsync(item, cancellationToken)
                : rule.Validate(item);
            
            if (!isValid)
            {
                errors.Add(rule.ErrorMessage);
            }
        }
        
        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }
}
```

### **Task 3.4: 유용한 확장 메서드** (2시간)

#### **AIAgent.Common/Extensions/ 구조**
```
src/AIAgent.Common/Extensions/
├── StringExtensions.cs           # 문자열 확장
├── EnumExtensions.cs            # 열거형 확장  
├── CollectionExtensions.cs      # 컬렉션 확장
├── DateTimeExtensions.cs        # 날짜/시간 확장
├── TaskExtensions.cs            # Task 확장
├── ResultExtensions.cs          # Result 패턴 확장
└── JsonExtensions.cs            # JSON 확장
```

#### **StringExtensions.cs 구현**
```csharp
namespace AIAgent.Common.Extensions;

/// <summary>
/// 문자열에 대한 확장 메서드를 제공합니다.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 문자열이 null이거나 공백인지 확인합니다.
    /// </summary>
    /// <param name="value">확인할 문자열</param>
    /// <returns>null이거나 공백이면 true</returns>
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);
    
    /// <summary>
    /// 문자열이 null이 아니고 공백이 아닌지 확인합니다.
    /// </summary>
    /// <param name="value">확인할 문자열</param>
    /// <returns>값이 있으면 true</returns>
    public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value);
    
    /// <summary>
    /// 문자열을 지정된 길이로 자릅니다.
    /// </summary>
    /// <param name="value">자를 문자열</param>
    /// <param name="maxLength">최대 길이</param>
    /// <param name="appendEllipsis">말줄임표 추가 여부</param>
    /// <returns>잘린 문자열</returns>
    public static string Truncate(this string? value, int maxLength, bool appendEllipsis = true)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;
        
        var truncated = value[..maxLength];
        return appendEllipsis ? truncated + "..." : truncated;
    }
    
    /// <summary>
    /// Base64로 인코딩합니다.
    /// </summary>
    /// <param name="value">인코딩할 문자열</param>
    /// <returns>Base64 인코딩된 문자열</returns>
    public static string ToBase64(this string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }
    
    /// <summary>
    /// Base64에서 디코딩합니다.
    /// </summary>
    /// <param name="base64Value">Base64 문자열</param>
    /// <returns>디코딩된 문자열</returns>
    public static string FromBase64(this string base64Value)
    {
        var bytes = Convert.FromBase64String(base64Value);
        return Encoding.UTF8.GetString(bytes);
    }
    
    /// <summary>
    /// 유효한 JSON인지 확인합니다.
    /// </summary>
    /// <param name="value">확인할 문자열</param>
    /// <returns>유효한 JSON이면 true</returns>
    public static bool IsValidJson(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// 문자열에서 민감한 정보를 마스킹합니다.
    /// </summary>
    /// <param name="value">마스킹할 문자열</param>
    /// <param name="visibleCharacters">보여줄 문자 수</param>
    /// <param name="maskCharacter">마스킹 문자</param>
    /// <returns>마스킹된 문자열</returns>
    public static string MaskSensitiveData(this string? value, int visibleCharacters = 4, char maskCharacter = '*')
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleCharacters)
            return value ?? string.Empty;
        
        var visiblePart = value[..visibleCharacters];
        var maskedPart = new string(maskCharacter, value.Length - visibleCharacters);
        
        return visiblePart + maskedPart;
    }
}
```

#### **ResultExtensions.cs 구현**
```csharp
namespace AIAgent.Common.Extensions;

/// <summary>
/// Result 패턴에 대한 확장 메서드를 제공합니다.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// 성공 시에만 지정된 함수를 적용합니다.
    /// </summary>
    /// <typeparam name="T">원래 결과 타입</typeparam>
    /// <typeparam name="U">새로운 결과 타입</typeparam>
    /// <param name="result">원래 결과</param>
    /// <param name="func">적용할 함수</param>
    /// <returns>변환된 결과</returns>
    public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> func)
    {
        return result.IsSuccess && result.Data != null
            ? Result<U>.Success(func(result.Data))
            : Result<U>.Failure(result.ErrorMessage ?? "Unknown error", result.ErrorCode, result.Exception);
    }
    
    /// <summary>
    /// 성공 시에만 지정된 비동기 함수를 적용합니다.
    /// </summary>
    /// <typeparam name="T">원래 결과 타입</typeparam>
    /// <typeparam name="U">새로운 결과 타입</typeparam>
    /// <param name="result">원래 결과</param>
    /// <param name="func">적용할 비동기 함수</param>
    /// <returns>변환된 결과</returns>
    public static async Task<Result<U>> MapAsync<T, U>(this Result<T> result, Func<T, Task<U>> func)
    {
        if (!result.IsSuccess || result.Data == null)
        {
            return Result<U>.Failure(result.ErrorMessage ?? "Unknown error", result.ErrorCode, result.Exception);
        }
        
        try
        {
            var newValue = await func(result.Data);
            return Result<U>.Success(newValue);
        }
        catch (Exception ex)
        {
            return Result<U>.Failure(ex.Message, "MAPPING_ERROR", ex);
        }
    }
    
    /// <summary>
    /// 성공 시에만 지정된 함수를 체이닝합니다.
    /// </summary>
    /// <typeparam name="T">원래 결과 타입</typeparam>
    /// <typeparam name="U">새로운 결과 타입</typeparam>
    /// <param name="result">원래 결과</param>
    /// <param name="func">체이닝할 함수</param>
    /// <returns>체이닝된 결과</returns>
    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> func)
    {
        return result.IsSuccess && result.Data != null
            ? func(result.Data)
            : Result<U>.Failure(result.ErrorMessage ?? "Unknown error", result.ErrorCode, result.Exception);
    }
    
    /// <summary>
    /// 결과를 Option으로 변환합니다.
    /// </summary>
    /// <typeparam name="T">결과 타입</typeparam>
    /// <param name="result">변환할 결과</param>
    /// <returns>Option 값</returns>
    public static T? ToOption<T>(this Result<T> result) where T : class
    {
        return result.IsSuccess ? result.Data : null;
    }
    
    /// <summary>
    /// 실패 시 기본값을 반환합니다.
    /// </summary>
    /// <typeparam name="T">결과 타입</typeparam>
    /// <param name="result">확인할 결과</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>성공 시 결과값, 실패 시 기본값</returns>
    public static T GetValueOrDefault<T>(this Result<T> result, T defaultValue)
    {
        return result.IsSuccess && result.Data != null ? result.Data : defaultValue;
    }
}
```

## 🔍 검증 기준

### **필수 통과 조건**

#### **1. 기능 완성도**
- [ ] 모든 설정 클래스가 완전히 구현됨
- [ ] 로깅 시스템이 모든 레벨에서 동작
- [ ] 검증 프레임워크가 동기/비동기 모두 지원
- [ ] 확장 메서드들이 모든 케이스에서 동작

#### **2. 단위 테스트**
- [ ] 설정 관리자 테스트 (바인딩, 검증, 모니터링)
- [ ] 로깅 시스템 테스트 (구조화 로깅, 상관관계 ID)
- [ ] 검증 프레임워크 테스트 (규칙 조합, 비동기 검증)
- [ ] 확장 메서드 테스트 (경계값, 예외 상황)

#### **3. 성능 검증**
- [ ] 설정 로드 시간 < 100ms
- [ ] 로깅 오버헤드 < 1ms per log
- [ ] 검증 처리 속도 < 10ms per validation
- [ ] 메모리 누수 없음 확인

## 📝 완료 체크리스트

### **설정 관리**
- [ ] IConfigurationManager 인터페이스 완성
- [ ] ConfigurationManager 구현 완료
- [ ] AgentSettings 모델 완성
- [ ] 설정 변경 모니터링 구현

### **로깅 시스템**  
- [ ] 구조화된 로깅 인터페이스 완성
- [ ] 상관관계 ID 관리 구현
- [ ] 성능 측정 로깅 구현
- [ ] 로깅 확장 메서드 완성

### **검증 프레임워크**
- [ ] 검증자 인터페이스 완성
- [ ] ValidationResult 패턴 구현
- [ ] 기본 검증 규칙들 구현
- [ ] 비동기 검증 지원

### **확장 메서드**
- [ ] 문자열 확장 메서드 완성
- [ ] Result 패턴 확장 완성
- [ ] 컬렉션 확장 메서드 완성
- [ ] JSON 처리 확장 완성

## 🎯 성공 지표

완료 시 다음이 모두 달성되어야 함:

1. ✅ **완전한 공통 인프라**: 모든 상위 레이어가 의존할 수 있는 견고한 기반
2. ✅ **구조화된 로깅**: 상관관계 추적 가능한 로깅 시스템
3. ✅ **유연한 설정 관리**: Hot-reload와 검증이 가능한 설정 시스템  
4. ✅ **재사용 가능한 유틸리티**: 개발 생산성을 높이는 확장 메서드들

---

**다음 계획**: [Plan 4: BaseLLMFunction 추상 클래스 설계](plan4.md)