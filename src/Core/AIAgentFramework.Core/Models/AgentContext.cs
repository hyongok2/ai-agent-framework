using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// Agent 전체 컨텍스트 구현체
/// - 실행 메타정보 (IExecutionContext)
/// - 동적 변수 저장소 (Step 간 데이터 공유)
/// </summary>
public class AgentContext : IAgentContext
{
    private readonly Dictionary<string, object> _variables = new();

    // IExecutionContext 구현
    public string ExecutionId { get; init; } = Guid.NewGuid().ToString();
    public string? UserId { get; init; }
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Metadata는 Variables의 읽기 전용 뷰로 제공
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata => _variables;

    // IAgentContext 구현
    public Dictionary<string, object> Variables => _variables;

    public void Set(string key, object value)
    {
        _variables[key] = value;
    }

    public T? Get<T>(string key)
    {
        if (!_variables.TryGetValue(key, out var value))
        {
            return default;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        // 문자열로 저장된 경우 변환 시도
        if (value is string str && typeof(T) != typeof(string))
        {
            try
            {
                return (T)Convert.ChangeType(str, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        value = Get<T>(key);
        return value != null;
    }

    public bool Contains(string key)
    {
        return _variables.ContainsKey(key);
    }

    public IEnumerable<string> Keys => _variables.Keys;

    /// <summary>
    /// 기본 생성 (자동 ID 할당)
    /// </summary>
    public static AgentContext Create(string? userId = null)
    {
        return new AgentContext
        {
            UserId = userId
        };
    }

    /// <summary>
    /// 초기 변수와 함께 생성
    /// </summary>
    public static AgentContext Create(Dictionary<string, object> initialValues, string? userId = null)
    {
        var context = new AgentContext
        {
            UserId = userId
        };

        foreach (var kvp in initialValues)
        {
            context.Set(kvp.Key, kvp.Value);
        }

        return context;
    }

    /// <summary>
    /// 명시적 메타정보와 함께 생성
    /// </summary>
    public static AgentContext Create(
        string executionId,
        string sessionId,
        string? userId = null)
    {
        return new AgentContext
        {
            ExecutionId = executionId,
            SessionId = sessionId,
            UserId = userId
        };
    }
}
