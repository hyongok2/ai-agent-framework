using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Functions.Analysis;

/// <summary>
/// 분석 결과 응답 모델
/// </summary>
public class AnalysisResponse
{
    /// <summary>
    /// 분석 요약
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 분석 타입
    /// </summary>
    [JsonPropertyName("analysis_type")]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// 신뢰도 점수 (0-100)
    /// </summary>
    [JsonPropertyName("confidence_score")]
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// 주요 발견사항
    /// </summary>
    [JsonPropertyName("key_findings")]
    public List<AnalysisFinding> KeyFindings { get; set; } = new();

    /// <summary>
    /// 권장사항
    /// </summary>
    [JsonPropertyName("recommendations")]
    public List<AnalysisRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// 메트릭 및 수치 데이터
    /// </summary>
    [JsonPropertyName("metrics")]
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// 위험 요소 (해당하는 경우)
    /// </summary>
    [JsonPropertyName("risks")]
    public List<AnalysisRisk> Risks { get; set; } = new();

    /// <summary>
    /// 기회 요소 (해당하는 경우)
    /// </summary>
    [JsonPropertyName("opportunities")]
    public List<AnalysisOpportunity> Opportunities { get; set; } = new();

    /// <summary>
    /// 분석 완료 시간
    /// </summary>
    [JsonPropertyName("analyzed_at")]
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 분석 발견사항 모델
/// </summary>
public class AnalysisFinding
{
    /// <summary>
    /// 발견사항 제목
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 발견사항 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 중요도 (1-10, 높을수록 중요)
    /// </summary>
    [JsonPropertyName("importance")]
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 카테고리
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 관련 데이터 또는 증거
    /// </summary>
    [JsonPropertyName("evidence")]
    public List<string> Evidence { get; set; } = new();
}

/// <summary>
/// 분석 권장사항 모델
/// </summary>
public class AnalysisRecommendation
{
    /// <summary>
    /// 권장사항 제목
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 권장사항 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 우선순위 (1-10, 높을수록 우선)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 예상 영향도 (1-10, 높을수록 큰 영향)
    /// </summary>
    [JsonPropertyName("impact")]
    public int Impact { get; set; } = 5;

    /// <summary>
    /// 실행 난이도 (1-10, 높을수록 어려움)
    /// </summary>
    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; } = 5;

    /// <summary>
    /// 권장사항 타입
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 실행 단계
    /// </summary>
    [JsonPropertyName("action_steps")]
    public List<string> ActionSteps { get; set; } = new();
}

/// <summary>
/// 분석 위험 요소 모델
/// </summary>
public class AnalysisRisk
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
    /// 발생 가능성 (1-10, 높을수록 가능성 높음)
    /// </summary>
    [JsonPropertyName("probability")]
    public int Probability { get; set; } = 5;

    /// <summary>
    /// 영향도 (1-10, 높을수록 큰 영향)
    /// </summary>
    [JsonPropertyName("impact")]
    public int Impact { get; set; } = 5;

    /// <summary>
    /// 위험 레벨 (낮음, 보통, 높음, 매우높음)
    /// </summary>
    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = "보통";

    /// <summary>
    /// 완화 방안
    /// </summary>
    [JsonPropertyName("mitigation_strategies")]
    public List<string> MitigationStrategies { get; set; } = new();
}

/// <summary>
/// 분석 기회 요소 모델
/// </summary>
public class AnalysisOpportunity
{
    /// <summary>
    /// 기회 요소 제목
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 기회 설명
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 잠재적 가치 (1-10, 높을수록 큰 가치)
    /// </summary>
    [JsonPropertyName("potential_value")]
    public int PotentialValue { get; set; } = 5;

    /// <summary>
    /// 실현 가능성 (1-10, 높을수록 실현 가능)
    /// </summary>
    [JsonPropertyName("feasibility")]
    public int Feasibility { get; set; } = 5;

    /// <summary>
    /// 필요한 리소스
    /// </summary>
    [JsonPropertyName("required_resources")]
    public List<string> RequiredResources { get; set; } = new();

    /// <summary>
    /// 예상 타임라인
    /// </summary>
    [JsonPropertyName("timeline")]
    public string Timeline { get; set; } = string.Empty;
}