using AIAgentFramework.LLM.Models;

namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// 프롬프트 레지스트리 인터페이스
/// 프롬프트 템플릿을 등록하고 관리
/// </summary>
public interface IPromptRegistry
{
    /// <summary>
    /// 프롬프트 정의 등록
    /// </summary>
    void Register(PromptDefinition definition);

    /// <summary>
    /// 이름으로 프롬프트 템플릿 조회
    /// </summary>
    IPromptTemplate? GetPrompt(string name);

    /// <summary>
    /// 역할로 프롬프트 템플릿 조회
    /// </summary>
    IPromptTemplate? GetPromptByRole(LLMRole role);

    /// <summary>
    /// 모든 프롬프트 정의 조회
    /// </summary>
    IReadOnlyCollection<PromptDefinition> GetAllPrompts();

    /// <summary>
    /// 프롬프트 변수 검증
    /// </summary>
    ValidationResult ValidateVariables(string promptName, IReadOnlyDictionary<string, object> variables);
}
