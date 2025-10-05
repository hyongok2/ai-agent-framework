using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 실행 컨텍스트 인터페이스
/// </summary>
public interface ILLMContext : IExecutionContext
{
    /// <summary>
    /// 사용자 입력
    /// </summary>
    string UserInput { get; }

    /// <summary>
    /// 대화 이력
    /// </summary>
    IReadOnlyList<string> ConversationHistory { get; }

    /// <summary>
    /// 프롬프트 치환 파라미터
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// 시스템 정보 (현재 시간 등)
    /// </summary>
    IReadOnlyDictionary<string, string> SystemInfo { get; }
}
