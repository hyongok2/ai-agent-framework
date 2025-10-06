using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// Agent 글로벌 컨텍스트 구현체
/// </summary>
public class AgentContext : IAgentContext
{
    private readonly Dictionary<string, object> _variables = new();

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
    /// 정적 팩토리 메서드
    /// </summary>
    public static AgentContext Create()
    {
        return new AgentContext();
    }

    /// <summary>
    /// 초기 값으로 생성
    /// </summary>
    public static AgentContext Create(Dictionary<string, object> initialValues)
    {
        var context = new AgentContext();
        foreach (var kvp in initialValues)
        {
            context.Set(kvp.Key, kvp.Value);
        }
        return context;
    }
}
