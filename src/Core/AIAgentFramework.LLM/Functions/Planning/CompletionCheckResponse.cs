using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Functions.Planning;

/// <summary>
/// 완료도 검사 응답 모델
/// </summary>
public class CompletionCheckResponse
{
    /// <summary>
    /// 전체 완료도 퍼센테이지 (0-100)
    /// </summary>
    [JsonPropertyName("completion_percentage")]
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// 완전히 완료되었는지 여부
    /// </summary>
    [JsonPropertyName("is_fully_completed")]
    public bool IsFullyCompleted { get; set; } = false;

    /// <summary>
    /// 전체 품질 점수 (0-100)
    /// </summary>
    [JsonPropertyName("quality_score")]
    public int QualityScore { get; set; }

    /// <summary>
    /// 완료도 평가 요약
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 완료된 작업 목록
    /// </summary>
    [JsonPropertyName("completed_tasks")]
    public List<CompletionTask> CompletedTasks { get; set; } = new();

    /// <summary>
    /// 남은 작업 목록
    /// </summary>
    [JsonPropertyName("remaining_tasks")]
    public List<CompletionTask> RemainingTasks { get; set; } = new();

    /// <summary>
    /// 다음 단계 추천
    /// </summary>
    [JsonPropertyName("next_steps")]
    public List<NextStepRecommendation> NextSteps { get; set; } = new();

    /// <summary>
    /// 품질 평가 상세
    /// </summary>
    [JsonPropertyName("quality_assessment")]
    public QualityAssessment QualityAssessment { get; set; } = new();

    /// <summary>
    /// 기능성 점수 (0-100)
    /// </summary>
    [JsonPropertyName("functionality_score")]
    public int FunctionalityScore { get; set; }

    /// <summary>
    /// 신뢰성 점수 (0-100)
    /// </summary>
    [JsonPropertyName("reliability_score")]
    public int ReliabilityScore { get; set; }

    /// <summary>
    /// 성능 점수 (0-100)
    /// </summary>
    [JsonPropertyName("performance_score")]
    public int PerformanceScore { get; set; }

    /// <summary>
    /// 사용성 점수 (0-100)
    /// </summary>
    [JsonPropertyName("usability_score")]
    public int UsabilityScore { get; set; }

    /// <summary>
    /// 유지보수성 점수 (0-100)
    /// </summary>
    [JsonPropertyName("maintainability_score")]
    public int MaintainabilityScore { get; set; }

    /// <summary>
    /// 위험 요소들
    /// </summary>
    [JsonPropertyName("risks")]
    public List<CompletionRisk> Risks { get; set; } = new();

    /// <summary>
    /// 차단 요소들
    /// </summary>
    [JsonPropertyName("blockers")]
    public List<CompletionBlocker> Blockers { get; set; } = new();

    /// <summary>
    /// 완료 예상 일정
    /// </summary>
    [JsonPropertyName("estimated_completion_date")]
    public DateTime? EstimatedCompletionDate { get; set; }

    /// <summary>
    /// 완료도 검사 수행 시간
    /// </summary>
    [JsonPropertyName("checked_at")]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 완료도 작업 모델
/// </summary>
public class CompletionTask
{
    /// <summary>
    /// 작업 ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 작업 이름
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 작업 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 작업 상태
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 완료도 퍼센테이지 (0-100)
    /// </summary>
    [JsonPropertyName("completion_percentage")]
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// 우선순위 (1-10, 높을수록 우선)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 예상 소요 시간 (시간)
    /// </summary>
    [JsonPropertyName("estimated_hours")]
    public double EstimatedHours { get; set; }

    /// <summary>
    /// 의존성 작업들
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// 다음 단계 추천 모델
/// </summary>
public class NextStepRecommendation
{
    /// <summary>
    /// 추천 제목
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 추천 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 우선순위 (1-10, 높을수록 우선)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 추천 타입 (immediate, short_term, long_term)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "short_term";

    /// <summary>
    /// 예상 소요 시간 (시간)
    /// </summary>
    [JsonPropertyName("estimated_hours")]
    public double EstimatedHours { get; set; }

    /// <summary>
    /// 필요한 리소스
    /// </summary>
    [JsonPropertyName("required_resources")]
    public List<string> RequiredResources { get; set; } = new();

    /// <summary>
    /// 예상 이익/효과
    /// </summary>
    [JsonPropertyName("expected_benefits")]
    public List<string> ExpectedBenefits { get; set; } = new();
}

/// <summary>
/// 품질 평가 모델
/// </summary>
public class QualityAssessment
{
    /// <summary>
    /// 전체 평가 점수 (0-100)
    /// </summary>
    [JsonPropertyName("overall_score")]
    public int OverallScore { get; set; }

    /// <summary>
    /// 강점들
    /// </summary>
    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// 약점들
    /// </summary>
    [JsonPropertyName("weaknesses")]
    public List<string> Weaknesses { get; set; } = new();

    /// <summary>
    /// 개선 제안들
    /// </summary>
    [JsonPropertyName("improvement_suggestions")]
    public List<string> ImprovementSuggestions { get; set; } = new();

    /// <summary>
    /// 품질 지표들
    /// </summary>
    [JsonPropertyName("quality_metrics")]
    public Dictionary<string, object> QualityMetrics { get; set; } = new();
}

/// <summary>
/// 완료 위험 요소 모델
/// </summary>
public class CompletionRisk
{
    /// <summary>
    /// 위험 요소 제목
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 위험 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 위험 레벨 (low, medium, high, critical)
    /// </summary>
    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = "medium";

    /// <summary>
    /// 영향을 받는 영역들
    /// </summary>
    [JsonPropertyName("affected_areas")]
    public List<string> AffectedAreas { get; set; } = new();

    /// <summary>
    /// 완화 방안들
    /// </summary>
    [JsonPropertyName("mitigation_strategies")]
    public List<string> MitigationStrategies { get; set; } = new();
}

/// <summary>
/// 완료 차단 요소 모델
/// </summary>
public class CompletionBlocker
{
    /// <summary>
    /// 차단 요소 제목
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 차단 요소 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 차단 유형 (technical, resource, external, process)
    /// </summary>
    [JsonPropertyName("blocker_type")]
    public string BlockerType { get; set; } = "technical";

    /// <summary>
    /// 영향받는 작업들
    /// </summary>
    [JsonPropertyName("affected_tasks")]
    public List<string> AffectedTasks { get; set; } = new();

    /// <summary>
    /// 해결 방안들
    /// </summary>
    [JsonPropertyName("resolution_strategies")]
    public List<string> ResolutionStrategies { get; set; } = new();

    /// <summary>
    /// 예상 해결 시간 (시간)
    /// </summary>
    [JsonPropertyName("estimated_resolution_hours")]
    public double EstimatedResolutionHours { get; set; }
}