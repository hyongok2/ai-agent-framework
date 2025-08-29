namespace AIAgentFramework.Core.Exceptions;

/// <summary>
/// LLM 관련 예외
/// </summary>
public class LLMException : AIAgentFrameworkException
{
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public LLMException()
    {
    }

    /// <summary>
    /// 메시지를 포함한 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    public LLMException(string message) : base(message)
    {
    }

    /// <summary>
    /// 메시지와 내부 예외를 포함한 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    /// <param name="innerException">내부 예외</param>
    public LLMException(string message, Exception innerException) : base(message, innerException)
    {
    }
}