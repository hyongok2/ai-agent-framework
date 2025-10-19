using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Services;

/// <summary>
/// 여러 이벤트 핸들러를 조합하는 컴포지트 핸들러
/// 예: Console + Logging + Metrics 동시 처리
/// </summary>
public class CompositeEventHandler : IAgentEventHandler
{
    private readonly IEnumerable<IAgentEventHandler> _handlers;

    public CompositeEventHandler(IEnumerable<IAgentEventHandler> handlers)
    {
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
    }

    public async Task OnUserInputAsync(AgentInputEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnUserInputAsync(evt)));
    }

    public async Task OnIntentAnalysisStartAsync(AgentPhaseEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnIntentAnalysisStartAsync(evt)));
    }

    public async Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnIntentAnalysisCompleteAsync(evt)));
    }

    public async Task OnPlanningStartAsync(AgentPhaseEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnPlanningStartAsync(evt)));
    }

    public async Task OnPlanningCompleteAsync(PlanningCompleteEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnPlanningCompleteAsync(evt)));
    }

    public async Task OnExecutionStartAsync(AgentPhaseEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnExecutionStartAsync(evt)));
    }

    public async Task OnStepStartAsync(StepExecutionEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnStepStartAsync(evt)));
    }

    public async Task OnStepCompleteAsync(StepExecutionEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnStepCompleteAsync(evt)));
    }

    public async Task OnExecutionCompleteAsync(AgentPhaseEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnExecutionCompleteAsync(evt)));
    }

    public async Task OnResponseChunkAsync(ResponseChunkEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnResponseChunkAsync(evt)));
    }

    public async Task OnResponseCompleteAsync(ResponseCompleteEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnResponseCompleteAsync(evt)));
    }

    public async Task OnErrorAsync(AgentErrorEvent evt)
    {
        await Task.WhenAll(_handlers.Select(h => h.OnErrorAsync(evt)));
    }
}
