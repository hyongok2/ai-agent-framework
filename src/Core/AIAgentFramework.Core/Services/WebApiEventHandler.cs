using System.Collections.Concurrent;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.Core.Services;

/// <summary>
/// Web API용 이벤트 핸들러
/// SSE (Server-Sent Events) 또는 WebSocket으로 전송 가능
/// </summary>
public class WebApiEventHandler : IAgentEventHandler
{
    private readonly ConcurrentQueue<object> _eventQueue = new();
    private readonly SemaphoreSlim _semaphore = new(0);

    /// <summary>
    /// 대기 중인 이벤트 가져오기 (비동기 스트림)
    /// </summary>
    public async IAsyncEnumerable<object> GetEventsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_eventQueue.TryDequeue(out var evt))
            {
                yield return evt;
            }
        }
    }

    public Task OnUserInputAsync(AgentInputEvent evt)
    {
        EnqueueEvent(new
        {
            type = "user_input",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnIntentAnalysisStartAsync(AgentPhaseEvent evt)
    {
        EnqueueEvent(new
        {
            type = "intent_analysis_start",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt)
    {
        EnqueueEvent(new
        {
            type = "intent_analysis_complete",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnPlanningStartAsync(AgentPhaseEvent evt)
    {
        EnqueueEvent(new
        {
            type = "planning_start",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnPlanningCompleteAsync(PlanningCompleteEvent evt)
    {
        EnqueueEvent(new
        {
            type = "planning_complete",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnExecutionStartAsync(AgentPhaseEvent evt)
    {
        EnqueueEvent(new
        {
            type = "execution_start",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnStepStartAsync(StepExecutionEvent evt)
    {
        EnqueueEvent(new
        {
            type = "step_start",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnStepCompleteAsync(StepExecutionEvent evt)
    {
        EnqueueEvent(new
        {
            type = "step_complete",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnExecutionCompleteAsync(AgentPhaseEvent evt)
    {
        EnqueueEvent(new
        {
            type = "execution_complete",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnResponseChunkAsync(ResponseChunkEvent evt)
    {
        EnqueueEvent(new
        {
            type = "response_chunk",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnResponseCompleteAsync(ResponseCompleteEvent evt)
    {
        EnqueueEvent(new
        {
            type = "response_complete",
            data = evt
        });
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(AgentErrorEvent evt)
    {
        EnqueueEvent(new
        {
            type = "error",
            data = evt
        });
        return Task.CompletedTask;
    }

    private void EnqueueEvent(object evt)
    {
        _eventQueue.Enqueue(evt);
        _semaphore.Release();
    }
}
