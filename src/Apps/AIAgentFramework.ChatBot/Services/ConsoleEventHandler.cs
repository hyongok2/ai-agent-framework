using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;

namespace AIAgentFramework.ChatBot.Services;

/// <summary>
/// 콘솔 출력용 이벤트 핸들러
/// </summary>
public class ConsoleEventHandler : IAgentEventHandler
{
    private readonly object _lock = new();

    public Task OnUserInputAsync(AgentInputEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n👤 You: {evt.Input}");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnIntentAnalysisStartAsync(AgentPhaseEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\n🔍 의도 분석 중...");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ 의도 파악 완료: {evt.TaskDescription ?? evt.IntentType}");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnPlanningStartAsync(AgentPhaseEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\n📋 계획 수립 중...");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnPlanningCompleteAsync(PlanningCompleteEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ 계획 수립 완료: {evt.Summary}");
            Console.WriteLine($"   단계: {evt.StepCount}개, 예상 시간: {evt.EstimatedSeconds}초");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnExecutionStartAsync(AgentPhaseEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n⚙️ 계획 실행 중...");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnStepStartAsync(StepExecutionEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"\n  [{evt.StepNumber}/{evt.TotalSteps}] {evt.Description}...");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnStepCompleteAsync(StepExecutionEvent evt)
    {
        lock (_lock)
        {
            if (evt.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"      ✓ 완료 ({evt.DurationMs}ms)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"      ✗ 실패: {evt.ErrorMessage}");
            }
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnExecutionCompleteAsync(AgentPhaseEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ 실행 완료");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnResponseChunkAsync(ResponseChunkEvent evt)
    {
        lock (_lock)
        {
            if (evt.ChunkType == "Status")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(evt.Content);
                Console.ResetColor();
            }
            else
            {
                Console.Write(evt.Content);
            }
        }
        return Task.CompletedTask;
    }

    public Task OnResponseCompleteAsync(ResponseCompleteEvent evt)
    {
        lock (_lock)
        {
            Console.WriteLine("\n");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[완료: {evt.TotalDurationMs}ms]");
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(AgentErrorEvent evt)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ 오류 발생: {evt.ErrorMessage}");
            if (!string.IsNullOrEmpty(evt.Phase))
            {
                Console.WriteLine($"   단계: {evt.Phase}");
            }
            Console.ResetColor();
        }
        return Task.CompletedTask;
    }
}
