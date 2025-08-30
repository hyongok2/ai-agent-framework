using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Parsing.Models;

/// <summary>
/// Planner 응답 모델
/// </summary>
public class PlannerResponse
{
    /// <summary>
    /// 사용자 요청 분석
    /// </summary>
    [JsonPropertyName("analysis")]
    public string Analysis { get; set; } = string.Empty;

    /// <summary>
    /// 주요 목표
    /// </summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    /// 실행 액션 목록
    /// </summary>
    [JsonPropertyName("actions")]
    public List<PlanAction> Actions { get; set; } = new();

    /// <summary>
    /// 완료 여부
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 계획 수립 근거
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// 계획 액션
/// </summary>
public class PlanAction
{
    /// <summary>
    /// 액션 타입 (LLM 또는 TOOL)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 기능/도구 이름
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 액션 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 파라미터
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// 의존성 (이전 단계들)
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}