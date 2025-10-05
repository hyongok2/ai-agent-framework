using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Extensions;

/// <summary>
/// ILLMContext 확장 메서드
/// </summary>
public static class LLMContextExtensions
{
    /// <summary>
    /// Parameters에서 타입 안전하게 값을 가져옵니다.
    /// </summary>
    public static T? Get<T>(this ILLMContext context, string key)
    {
        if (context.Parameters.TryGetValue(key, out var value))
        {
            if (value is T typed)
            {
                return typed;
            }

            // 타입 변환 시도
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    /// <summary>
    /// Parameters에서 값을 가져오거나 기본값을 반환합니다.
    /// </summary>
    public static T GetOrDefault<T>(this ILLMContext context, string key, T defaultValue)
    {
        var value = context.Get<T>(key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Parameters에 값이 존재하는지 확인합니다.
    /// </summary>
    public static bool Contains(this ILLMContext context, string key)
    {
        return context.Parameters.ContainsKey(key);
    }
}
