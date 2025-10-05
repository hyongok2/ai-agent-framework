using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// 실행 컨텍스트 기본 구현
/// </summary>
public class ExecutionContext : IExecutionContext
{
    public string ExecutionId { get; init; } = Guid.NewGuid().ToString();
    public string? UserId { get; init; }
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    public static ExecutionContext Create(string? userId = null)
    {
        return new ExecutionContext
        {
            UserId = userId
        };
    }
}
