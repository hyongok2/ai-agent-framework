using AIAgentFramework.Core.Models;
using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Functions.Planning;

/// <summary>
/// 계획 수립 응답 모델
/// </summary>
public class PlannerResponse
{
    /// <summary>
    /// 요청 분석 결과
    /// </summary>
    [JsonPropertyName("analysis")]
    public string Analysis { get; set; } = string.Empty;

    /// <summary>
    /// 최종 목표
    /// </summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    /// 실행 계획 액션 목록
    /// </summary>
    [JsonPropertyName("actions")]
    public List<PlanAction> Actions { get; set; } = new();

    /// <summary>
    /// 계획 완료 여부
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// 예상 실행 시간 (분)
    /// </summary>
    [JsonPropertyName("estimated_duration_minutes")]
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// 계획 복잡도 (1-10)
    /// </summary>
    [JsonPropertyName("complexity_score")]
    public int ComplexityScore { get; set; } = 1;

    /// <summary>
    /// 필요한 외부 리소스
    /// </summary>
    [JsonPropertyName("required_resources")]
    public List<string> RequiredResources { get; set; } = new();
}

/// <summary>
/// 계획 액션 모델
/// </summary>
public class PlanAction
{
    /// <summary>
    /// 액션 ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 액션 타입 (llm_function, tool, delay, conditional)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 실행할 기능/도구 이름
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 액션 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 입력 파라미터
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// 이전 액션 의존성
    /// </summary>
    [JsonPropertyName("depends_on")]
    public List<string> DependsOn { get; set; } = new();

    /// <summary>
    /// 예상 실행 시간 (초)
    /// </summary>
    [JsonPropertyName("estimated_duration_seconds")]
    public int EstimatedDurationSeconds { get; set; }

    /// <summary>
    /// 액션 우선순위 (1-10, 높을수록 우선)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 실행 조건 (선택적)
    /// </summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>
    /// 실행 순서
    /// </summary>
    [JsonPropertyName("execution_order")]
    public int ExecutionOrder { get; set; }
}