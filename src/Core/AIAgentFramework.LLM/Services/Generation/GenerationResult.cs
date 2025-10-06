namespace AIAgentFramework.LLM.Services.Generation;

/// <summary>
/// Generator 결과
/// </summary>
public class GenerationResult
{
    /// <summary>
    /// 생성된 콘텐츠
    /// </summary>
    public required string GeneratedContent { get; init; }

    /// <summary>
    /// 콘텐츠 유형
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// 생성된 콘텐츠의 단어 수 또는 줄 수
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// 적용된 스타일
    /// </summary>
    public string? AppliedStyle { get; init; }

    /// <summary>
    /// 생성 품질 자체 평가 (0.0 ~ 1.0)
    /// </summary>
    public required double QualityScore { get; init; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; init; }
}
