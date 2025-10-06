namespace AIAgentFramework.LLM.Services.Generation;

/// <summary>
/// Generator 입력 정보
/// </summary>
public class GenerationInput
{
    /// <summary>
    /// 생성할 콘텐츠의 주제 또는 설명
    /// </summary>
    public required string Topic { get; init; }

    /// <summary>
    /// 콘텐츠 유형 (예: "text", "code", "report", "email")
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// 생성 요구사항 (선택적)
    /// </summary>
    public string? Requirements { get; init; }

    /// <summary>
    /// 스타일 또는 톤 (선택적)
    /// 예: "formal", "casual", "technical", "creative"
    /// </summary>
    public string? Style { get; init; }
}
