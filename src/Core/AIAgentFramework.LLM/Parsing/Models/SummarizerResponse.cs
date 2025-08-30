using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Parsing.Models;

/// <summary>
/// Summarizer 응답 모델
/// </summary>
public class SummarizerResponse
{
    /// <summary>
    /// 요약 내용
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 주요 포인트
    /// </summary>
    [JsonPropertyName("key_points")]
    public List<string> KeyPoints { get; set; } = new();

    /// <summary>
    /// 단어 수
    /// </summary>
    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    /// <summary>
    /// 원본 길이
    /// </summary>
    [JsonPropertyName("original_length")]
    public int OriginalLength { get; set; }

    /// <summary>
    /// 압축 비율
    /// </summary>
    [JsonPropertyName("compression_ratio")]
    public string CompressionRatio { get; set; } = string.Empty;

    /// <summary>
    /// 다룬 주제들
    /// </summary>
    [JsonPropertyName("topics_covered")]
    public List<string> TopicsCovered { get; set; } = new();

    /// <summary>
    /// 완료 여부
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }
}