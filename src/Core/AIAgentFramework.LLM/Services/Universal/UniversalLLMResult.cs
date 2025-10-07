using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Services.Universal;

/// <summary>
/// UniversalLLM 실행 결과
/// </summary>
public class UniversalLLMResult
{
    /// <summary>
    /// 생성된 응답 내용
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// 실행된 작업 유형
    /// </summary>
    [JsonPropertyName("taskType")]
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// 적용된 페르소나
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; set; }

    /// <summary>
    /// 응답 품질 점수 (0.0 ~ 1.0)
    /// </summary>
    [JsonPropertyName("qualityScore")]
    public double QualityScore { get; set; } = 1.0;

    /// <summary>
    /// 에러 메시지 (실패 시)
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
