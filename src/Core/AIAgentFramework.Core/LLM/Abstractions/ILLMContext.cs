namespace AIAgentFramework.Core.LLM.Abstractions;

/// <summary>
/// LLM 컨텍스트 인터페이스 - 핵심 컨텍스트 정보만
/// </summary>
public interface ILLMContext
{
    /// <summary>
    /// 세션 ID
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// 사용할 모델
    /// </summary>
    string? Model { get; set; }

    /// <summary>
    /// 파라미터
    /// </summary>
    Dictionary<string, object> Parameters { get; }

    /// <summary>
    /// 메타데이터
    /// </summary>
    Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// 공유 데이터
    /// </summary>
    Dictionary<string, object> SharedData { get; }

    /// <summary>
    /// 사용자 요청
    /// </summary>
    string? UserRequest { get; set; }
}