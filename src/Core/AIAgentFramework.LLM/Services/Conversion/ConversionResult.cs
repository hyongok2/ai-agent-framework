namespace AIAgentFramework.LLM.Services.Conversion;

/// <summary>
/// Converter 결과
/// </summary>
public class ConversionResult
{
    /// <summary>
    /// 변환된 내용
    /// </summary>
    public required string ConvertedContent { get; init; }

    /// <summary>
    /// 원본 형식
    /// </summary>
    public required string SourceFormat { get; init; }

    /// <summary>
    /// 대상 형식
    /// </summary>
    public required string TargetFormat { get; init; }

    /// <summary>
    /// 변환 품질 점수 (0.0 ~ 1.0)
    /// </summary>
    public required double QualityScore { get; init; }

    /// <summary>
    /// 변환 시 발생한 경고 또는 주의사항
    /// </summary>
    public List<string>? Warnings { get; init; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; init; }
}
