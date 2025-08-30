namespace AIAgentFramework.LLM.Exceptions;

/// <summary>
/// LLM 관련 기본 예외
/// </summary>
public class LLMException : Exception
{
    /// <summary>
    /// Provider 이름
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// 모델명
    /// </summary>
    public string? Model { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    public LLMException() : base() { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    public LLMException(string message) : base(message) { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="innerException">내부 예외</param>
    public LLMException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="model">모델명</param>
    public LLMException(string message, string? providerName, string? model) : base(message)
    {
        ProviderName = providerName;
        Model = model;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="model">모델명</param>
    /// <param name="innerException">내부 예외</param>
    public LLMException(string message, string? providerName, string? model, Exception innerException) 
        : base(message, innerException)
    {
        ProviderName = providerName;
        Model = model;
    }
}

/// <summary>
/// LLM API 인증 오류
/// </summary>
public class LLMAuthenticationException : LLMException
{
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    public LLMAuthenticationException(string message) : base(message) { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="providerName">Provider 이름</param>
    public LLMAuthenticationException(string message, string providerName) : base(message, providerName, null) { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="innerException">내부 예외</param>
    public LLMAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// LLM API 요청 제한 오류
/// </summary>
public class LLMRateLimitException : LLMException
{
    /// <summary>
    /// 재시도 가능 시간
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    public LLMRateLimitException(string message) : base(message) { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="retryAfter">재시도 가능 시간</param>
    public LLMRateLimitException(string message, TimeSpan retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="retryAfter">재시도 가능 시간</param>
    public LLMRateLimitException(string message, string providerName, TimeSpan? retryAfter = null) 
        : base(message, providerName, null)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// LLM 모델 지원 오류
/// </summary>
public class LLMModelNotSupportedException : LLMException
{
    /// <summary>
    /// 지원되는 모델 목록
    /// </summary>
    public IReadOnlyList<string>? SupportedModels { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="model">요청된 모델</param>
    public LLMModelNotSupportedException(string message, string model) : base(message, null, model) { }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="model">요청된 모델</param>
    /// <param name="supportedModels">지원되는 모델 목록</param>
    public LLMModelNotSupportedException(string message, string providerName, string model, IReadOnlyList<string>? supportedModels = null) 
        : base(message, providerName, model)
    {
        SupportedModels = supportedModels;
    }
}

/// <summary>
/// LLM 응답 파싱 오류
/// </summary>
public class LLMResponseParsingException : LLMException
{
    /// <summary>
    /// 원본 응답
    /// </summary>
    public string? RawResponse { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="rawResponse">원본 응답</param>
    public LLMResponseParsingException(string message, string? rawResponse = null) : base(message)
    {
        RawResponse = rawResponse;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="rawResponse">원본 응답</param>
    /// <param name="innerException">내부 예외</param>
    public LLMResponseParsingException(string message, string? rawResponse, Exception innerException) 
        : base(message, innerException)
    {
        RawResponse = rawResponse;
    }
}

/// <summary>
/// LLM 토큰 제한 오류
/// </summary>
public class LLMTokenLimitException : LLMException
{
    /// <summary>
    /// 요청된 토큰 수
    /// </summary>
    public int RequestedTokens { get; }

    /// <summary>
    /// 최대 토큰 수
    /// </summary>
    public int MaxTokens { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="requestedTokens">요청된 토큰 수</param>
    /// <param name="maxTokens">최대 토큰 수</param>
    public LLMTokenLimitException(string message, int requestedTokens, int maxTokens) : base(message)
    {
        RequestedTokens = requestedTokens;
        MaxTokens = maxTokens;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="providerName">Provider 이름</param>
    /// <param name="model">모델명</param>
    /// <param name="requestedTokens">요청된 토큰 수</param>
    /// <param name="maxTokens">최대 토큰 수</param>
    public LLMTokenLimitException(string message, string providerName, string model, int requestedTokens, int maxTokens) 
        : base(message, providerName, model)
    {
        RequestedTokens = requestedTokens;
        MaxTokens = maxTokens;
    }
}