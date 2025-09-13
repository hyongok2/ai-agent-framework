namespace AIAgentFramework.Core.Common.Exceptions;

/// <summary>
/// AI 에이전트 프레임워크 기본 예외
/// </summary>
public class AIAgentFrameworkException : Exception
{
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public AIAgentFrameworkException()
    {
    }

    /// <summary>
    /// 메시지를 포함한 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    public AIAgentFrameworkException(string message) : base(message)
    {
    }

    /// <summary>
    /// 메시지와 내부 예외를 포함한 생성자
    /// </summary>
    /// <param name="message">예외 메시지</param>
    /// <param name="innerException">내부 예외</param>
    public AIAgentFrameworkException(string message, Exception innerException) : base(message, innerException)
    {
    }
}