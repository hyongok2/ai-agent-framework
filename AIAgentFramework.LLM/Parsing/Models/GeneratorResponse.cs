using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Parsing.Models;

/// <summary>
/// Generator 응답 모델
/// </summary>
public class GeneratorResponse
{
    /// <summary>
    /// 생성된 콘텐츠
    /// </summary>
    [JsonPropertyName("generated_content")]
    public string GeneratedContent { get; set; } = string.Empty;

    /// <summary>
    /// 콘텐츠 타입
    /// </summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 단어 수
    /// </summary>
    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    /// <summary>
    /// 스타일 노트
    /// </summary>
    [JsonPropertyName("style_notes")]
    public string StyleNotes { get; set; } = string.Empty;

    /// <summary>
    /// 주요 특징
    /// </summary>
    [JsonPropertyName("key_features")]
    public List<string> KeyFeatures { get; set; } = new();

    /// <summary>
    /// 품질 점수 (1-10)
    /// </summary>
    [JsonPropertyName("quality_score")]
    public string QualityScore { get; set; } = string.Empty;

    /// <summary>
    /// 완료 여부
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }
}