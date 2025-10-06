namespace AIAgentFramework.LLM.Services.Extraction;

/// <summary>
/// Extractor 입력 정보
/// </summary>
public class ExtractionInput
{
    /// <summary>
    /// 정보를 추출할 원본 텍스트
    /// </summary>
    public required string SourceText { get; init; }

    /// <summary>
    /// 추출할 정보 유형 (예: "entities", "dates", "emails", "keywords")
    /// </summary>
    public required string ExtractionType { get; init; }

    /// <summary>
    /// 추출 기준 또는 패턴 (선택적)
    /// </summary>
    public string? Criteria { get; init; }
}
