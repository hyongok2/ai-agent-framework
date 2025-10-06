namespace AIAgentFramework.LLM.Services.Planning;

/// <summary>
/// 계획 타입 - TaskPlanner가 분석한 사용자 요청의 유형
/// </summary>
public enum PlanType
{
    /// <summary>
    /// 도구 실행 계획 - 파일 생성, 명령 실행 등 Tool이 필요한 작업
    /// </summary>
    ToolExecution,

    /// <summary>
    /// 단순 응답 - 인사, 감사, 일반 대화 등 도구가 필요 없는 대화
    /// </summary>
    SimpleResponse,

    /// <summary>
    /// 정보 제공 - 설명, 안내, 질문 답변 등 지식 기반 응답
    /// </summary>
    Information,

    /// <summary>
    /// 불명확 - 사용자 의도가 불명확하여 추가 정보 필요
    /// </summary>
    Clarification
}
