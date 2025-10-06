namespace AIAgentFramework.LLM.Services.Extraction;

/// <summary>
/// Extractor 결과
/// </summary>
public class ExtractionResult
{
    /// <summary>
    /// 추출된 항목 목록
    /// </summary>
    public required List<ExtractedItem> ExtractedItems { get; init; }

    /// <summary>
    /// 추출 유형
    /// </summary>
    public required string ExtractionType { get; init; }

    /// <summary>
    /// 추출된 항목 개수
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// 추출 신뢰도 (0.0 ~ 1.0)
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 추출된 개별 항목
/// </summary>
public class ExtractedItem
{
    /// <summary>
    /// 추출된 값
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// 항목 유형 또는 카테고리
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// 원본 텍스트에서의 컨텍스트
    /// </summary>
    public string? Context { get; init; }
}
