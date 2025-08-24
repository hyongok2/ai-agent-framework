namespace Agent.Abstractions.Core.Common.Exceptions;
/// <summary>
/// Agent Framework 기본 예외
/// </summary>
public class AgentException : Exception
{
    public string? ErrorCode { get; }

    public AgentException(string message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public AgentException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}