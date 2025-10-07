namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 역할 (기능) 분류
/// </summary>
/// <remarks>
/// AI Agent Framework의 LLM 기능을 역할 기반으로 분류합니다.
/// 새로운 아키텍처: IntentAnalyzer → Planner → UniversalLLM
/// </remarks>
public enum LLMRole
{
    // ========================================
    // 핵심 오케스트레이션
    // ========================================

    /// <summary>
    /// 의도 분석 - 사용자 입력의 의도 파악 및 즉시 응답 가능 여부 판단 (Chat/Question/Task)
    /// </summary>
    IntentAnalyzer,

    /// <summary>
    /// 계획 수립 및 조율 - 사용자 요구 분석, 단계별 실행 계획 수립, ResponseGuide 생성
    /// </summary>
    Planner,

    /// <summary>
    /// Tool 파라미터 생성 - 외부 Tool 호출에 필요한 파라미터 값 구성
    /// </summary>
    ToolParameterSetter,

    /// <summary>
    /// 범용 LLM - Persona와 ResponseGuide를 받아 모든 LLM 작업 수행
    /// (Summarize, Analyze, Convert, Generate, Extract, Classify, Reason, Refine, Explain 등 통합)
    /// </summary>
    Universal,

    // ========================================
    // 평가 및 대화
    // ========================================

    /// <summary>
    /// 품질 평가 및 비평 - 결과물의 품질 평가, 대안 제시, 개선 포인트 피드백
    /// </summary>
    Evaluator,

    /// <summary>
    /// 대화가 - 일반적인 대화 및 질문 답변 (비구조화, 도구 사용 없음)
    /// </summary>
    Conversationalist
}
