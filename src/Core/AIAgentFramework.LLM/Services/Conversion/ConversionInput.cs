namespace AIAgentFramework.LLM.Services.Conversion;

/// <summary>
/// Converter 입력 정보
/// </summary>
public class ConversionInput
{
    /// <summary>
    /// 변환할 원본 내용
    /// </summary>
    public required string SourceContent { get; init; }

    /// <summary>
    /// 원본 형식 (예: "JSON", "Markdown", "Korean", "XML")
    /// </summary>
    public required string SourceFormat { get; init; }

    /// <summary>
    /// 대상 형식 (예: "YAML", "HTML", "English", "JSON")
    /// </summary>
    public required string TargetFormat { get; init; }

    /// <summary>
    /// 변환 옵션 (선택적)
    /// 예: "preserve_structure", "simplify", "verbose"
    /// </summary>
    public string? Options { get; init; }
}
