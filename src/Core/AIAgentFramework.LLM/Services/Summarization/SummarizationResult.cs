namespace AIAgentFramework.LLM.Services.Summarization;

/// <summary>
/// Summarizer 요약 결과
/// </summary>
public class SummarizationResult
{
    /// <summary>
    /// 요약 텍스트
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// 사용된 요약 스타일
    /// </summary>
    public required string Style { get; init; }

    /// <summary>
    /// 핵심 포인트 목록
    /// </summary>
    public required List<string> KeyPoints { get; init; } = new();

    /// <summary>
    /// 요약 단어 수
    /// </summary>
    public required int WordCount { get; init; }

    /// <summary>
    /// 원본 길이 (단어/문자 수)
    /// </summary>
    public required int OriginalLength { get; init; }

    /// <summary>
    /// 파싱 실패 시 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
}
