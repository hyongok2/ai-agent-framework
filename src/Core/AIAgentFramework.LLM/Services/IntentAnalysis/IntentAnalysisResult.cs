using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Services.IntentAnalysis;

/// <summary>
/// 의도 분석 결과
/// </summary>
public class IntentAnalysisResult
{
    /// <summary>
    /// 의도 타입
    /// </summary>
    [JsonPropertyName("intentType")]
    public IntentType IntentType { get; set; }

    /// <summary>
    /// 계획 수립 필요 여부
    /// </summary>
    [JsonPropertyName("needsPlanning")]
    public bool NeedsPlanning { get; set; }

    /// <summary>
    /// 직접 응답 (Chat/Question인 경우)
    /// </summary>
    [JsonPropertyName("directResponse")]
    public string? DirectResponse { get; set; }

    /// <summary>
    /// 작업 설명 (Task인 경우)
    /// </summary>
    [JsonPropertyName("taskDescription")]
    public string? TaskDescription { get; set; }

    /// <summary>
    /// 신뢰도
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

/// <summary>
/// 의도 타입
/// </summary>
public enum IntentType
{
    /// <summary>
    /// 일반 대화 (인사, 감사 등)
    /// </summary>
    Chat,

    /// <summary>
    /// 정보 요청 (질문, 설명 요청)
    /// </summary>
    Question,

    /// <summary>
    /// 작업 실행 (도구/LLM 사용 필요)
    /// </summary>
    Task
}
