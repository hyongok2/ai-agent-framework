using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Services.Universal;

/// <summary>
/// LLM 실행을 위한 동적 프롬프트 가이드
/// Planner가 각 단계마다 생성하여 UniversalLLM에 전달
/// </summary>
public class ResponseGuide
{
    /// <summary>
    /// 상세 지시사항
    /// </summary>
    [JsonPropertyName("instruction")]
    public string Instruction { get; set; } = string.Empty;

    /// <summary>
    /// 출력 포맷 (JSON, Text, Markdown 등)
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "JSON";

    /// <summary>
    /// 출력 스키마 (JSON 형식일 때)
    /// </summary>
    [JsonPropertyName("outputSchema")]
    public string? OutputSchema { get; set; }

    /// <summary>
    /// 응답 스타일 (concise, detailed, technical, casual 등)
    /// </summary>
    [JsonPropertyName("style")]
    public string Style { get; set; } = "standard";

    /// <summary>
    /// 제약조건 목록
    /// </summary>
    [JsonPropertyName("constraints")]
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// 예제 (선택적)
    /// </summary>
    [JsonPropertyName("examples")]
    public List<string>? Examples { get; set; }
}
