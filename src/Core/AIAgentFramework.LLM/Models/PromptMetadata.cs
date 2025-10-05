namespace AIAgentFramework.LLM.Models;

/// <summary>
/// 프롬프트 메타데이터
/// 버전, 설명, 작성자 등 부가 정보
/// </summary>
public class PromptMetadata
{
    public string Version { get; init; } = "1.0";
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = "System";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> Tags { get; init; } = new();
}
