namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// 프롬프트 템플릿 인터페이스
/// 파일 기반 프롬프트 관리
/// </summary>
public interface IPromptTemplate
{
    /// <summary>
    /// LLM 역할
    /// </summary>
    LLMRole Role { get; }

    /// <summary>
    /// 템플릿 콘텐츠 (치환 전)
    /// </summary>
    string Template { get; }

    /// <summary>
    /// 치환 가능한 변수 목록
    /// </summary>
    IReadOnlyList<string> Variables { get; }

    /// <summary>
    /// 변수를 치환하여 최종 프롬프트 생성
    /// </summary>
    /// <param name="parameters">치환 파라미터</param>
    /// <returns>치환된 프롬프트</returns>
    string Render(IReadOnlyDictionary<string, object> parameters);
}
