using AIAgentFramework.LLM.Abstractions;

namespace AIAgentFramework.LLM.Models;

/// <summary>
/// 프롬프트 정의
/// 프롬프트의 이름, 역할, 템플릿 경로, 필수/선택 변수 등을 정의
/// </summary>
public class PromptDefinition
{
    public string Name { get; init; } = string.Empty;
    public LLMRole Role { get; init; }
    public string TemplatePath { get; init; } = string.Empty;
    public IReadOnlyList<string> RequiredVariables { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OptionalVariables { get; init; } = Array.Empty<string>();
    public PromptMetadata Metadata { get; init; } = new();
}
