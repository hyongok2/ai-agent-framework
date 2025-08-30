using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Parsing.Models;

/// <summary>
/// Interpreter 응답 모델
/// </summary>
public class InterpreterResponse
{
    /// <summary>
    /// 해석 결과
    /// </summary>
    [JsonPropertyName("interpretation")]
    public string Interpretation { get; set; } = string.Empty;

    /// <summary>
    /// 주요 인사이트
    /// </summary>
    [JsonPropertyName("key_insights")]
    public List<string> KeyInsights { get; set; } = new();

    /// <summary>
    /// 발견된 패턴
    /// </summary>
    [JsonPropertyName("patterns")]
    public List<string> Patterns { get; set; } = new();

    /// <summary>
    /// 요약
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 신뢰도
    /// </summary>
    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;

    /// <summary>
    /// 추천사항
    /// </summary>
    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// 완료 여부
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }
}