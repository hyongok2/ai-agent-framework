using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Abstractions;

/// <summary>
/// Agent 실행 중 발생하는 이벤트를 처리하는 인터페이스
/// UI/API/CLI 등 다양한 클라이언트에서 구현 가능
/// </summary>
public interface IAgentEventHandler
{
    /// <summary>
    /// Agent가 사용자 입력을 받았을 때
    /// </summary>
    Task OnUserInputAsync(AgentInputEvent evt);

    /// <summary>
    /// Intent 분석 시작
    /// </summary>
    Task OnIntentAnalysisStartAsync(AgentPhaseEvent evt);

    /// <summary>
    /// Intent 분석 완료
    /// </summary>
    Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt);

    /// <summary>
    /// 계획 수립 시작
    /// </summary>
    Task OnPlanningStartAsync(AgentPhaseEvent evt);

    /// <summary>
    /// 계획 수립 완료
    /// </summary>
    Task OnPlanningCompleteAsync(PlanningCompleteEvent evt);

    /// <summary>
    /// 계획 실행 시작
    /// </summary>
    Task OnExecutionStartAsync(AgentPhaseEvent evt);

    /// <summary>
    /// 단계 실행 시작
    /// </summary>
    Task OnStepStartAsync(StepExecutionEvent evt);

    /// <summary>
    /// 단계 실행 완료
    /// </summary>
    Task OnStepCompleteAsync(StepExecutionEvent evt);

    /// <summary>
    /// 계획 실행 완료
    /// </summary>
    Task OnExecutionCompleteAsync(AgentPhaseEvent evt);

    /// <summary>
    /// 응답 생성 중 (스트리밍)
    /// </summary>
    Task OnResponseChunkAsync(ResponseChunkEvent evt);

    /// <summary>
    /// 최종 응답 완료
    /// </summary>
    Task OnResponseCompleteAsync(ResponseCompleteEvent evt);

    /// <summary>
    /// 오류 발생
    /// </summary>
    Task OnErrorAsync(AgentErrorEvent evt);
}
