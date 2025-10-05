namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 역할 (기능) 분류
/// </summary>
public enum LLMRole
{
    /// <summary>
    /// 계획 수립 및 조율 (핵심)
    /// </summary>
    Planner,

    /// <summary>
    /// 입력 분석 및 해석
    /// </summary>
    Analyzer,

    /// <summary>
    /// 내용 요약
    /// </summary>
    Summarizer,

    /// <summary>
    /// 콘텐츠 생성
    /// </summary>
    Generator,

    /// <summary>
    /// 품질 평가 및 비평
    /// </summary>
    Evaluator,

    /// <summary>
    /// 개선 및 재작성
    /// </summary>
    Refiner,

    /// <summary>
    /// 설명 및 교육
    /// </summary>
    Explainer,

    /// <summary>
    /// 추론 및 논리 검증
    /// </summary>
    Reasoner,

    /// <summary>
    /// 변환 및 번역
    /// </summary>
    Converter,

    /// <summary>
    /// 텍스트 기반 시각화
    /// </summary>
    Visualizer,

    /// <summary>
    /// Tool 파라미터 설정
    /// </summary>
    ToolParameterSetter,

    /// <summary>
    /// 대화 관리
    /// </summary>
    DialogueManager,

    /// <summary>
    /// 지식 검색
    /// </summary>
    KnowledgeRetriever,

    /// <summary>
    /// 메타 관리 및 자기 반성
    /// </summary>
    MetaManager
}
