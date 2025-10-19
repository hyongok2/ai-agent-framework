# UI 인터페이스 사양

## 개요

AI Agent Framework는 **Event-Driven UI 인터페이스**를 제공하여 다양한 클라이언트(CLI, Web, Desktop, Mobile)에서 유연하게 사용할 수 있습니다.

## 설계 원칙

### 1. 관심사의 분리
- **비즈니스 로직**: AgentOrchestrator가 순수하게 처리
- **UI 표현**: IAgentEventHandler 구현체가 담당
- **결합도 최소화**: Console.Write 등 UI 코드가 비즈니스 로직에 없음

### 2. 이벤트 기반 아키텍처
```
Agent → Event → Handler(s) → UI/API/Log
```

### 3. 다중 핸들러 지원
```csharp
var handlers = new IAgentEventHandler[]
{
    new ConsoleEventHandler(),      // 콘솔 출력
    new MetricsEventHandler(),      // 메트릭 수집
    new LoggingEventHandler()       // 구조화 로깅
};
var composite = new CompositeEventHandler(handlers);
```

## 인터페이스 정의

### IAgentEventHandler

```csharp
public interface IAgentEventHandler
{
    // 사용자 입력
    Task OnUserInputAsync(AgentInputEvent evt);

    // Intent 분석
    Task OnIntentAnalysisStartAsync(AgentPhaseEvent evt);
    Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt);

    // 계획 수립
    Task OnPlanningStartAsync(AgentPhaseEvent evt);
    Task OnPlanningCompleteAsync(PlanningCompleteEvent evt);

    // 실행
    Task OnExecutionStartAsync(AgentPhaseEvent evt);
    Task OnStepStartAsync(StepExecutionEvent evt);
    Task OnStepCompleteAsync(StepExecutionEvent evt);
    Task OnExecutionCompleteAsync(AgentPhaseEvent evt);

    // 응답
    Task OnResponseChunkAsync(ResponseChunkEvent evt);
    Task OnResponseCompleteAsync(ResponseCompleteEvent evt);

    // 오류
    Task OnErrorAsync(AgentErrorEvent evt);
}
```

## 이벤트 타입

### 1. AgentInputEvent
사용자가 입력을 제공했을 때

```json
{
  "executionId": "abc123",
  "sessionId": "session-1",
  "timestamp": "2025-10-12T10:30:00Z",
  "input": "파일을 읽고 요약해줘"
}
```

### 2. IntentAnalysisCompleteEvent
Intent 분석 완료

```json
{
  "executionId": "abc123",
  "intentType": "Task",
  "taskDescription": "파일 읽고 요약",
  "confidence": 0.95
}
```

### 3. PlanningCompleteEvent
계획 수립 완료

```json
{
  "executionId": "abc123",
  "summary": "파일을 읽고 UniversalLLM으로 요약",
  "stepCount": 2,
  "estimatedSeconds": 15,
  "isExecutable": true
}
```

### 4. StepExecutionEvent
단계 실행 시작/완료

```json
{
  "executionId": "abc123",
  "stepNumber": 1,
  "totalSteps": 2,
  "description": "파일 읽기",
  "toolName": "FileReader",
  "isSuccess": true,
  "output": "파일 내용...",
  "durationMs": 150
}
```

### 5. ResponseChunkEvent
응답 스트리밍 (실시간)

```json
{
  "executionId": "abc123",
  "content": "이 파일은",
  "chunkType": "Text"
}
```

### 6. ResponseCompleteEvent
응답 완료

```json
{
  "executionId": "abc123",
  "fullResponse": "이 파일은 AI Agent Framework에 대한 설명입니다...",
  "totalDurationMs": 3500,
  "tokensUsed": 1250
}
```

### 7. AgentErrorEvent
오류 발생

```json
{
  "executionId": "abc123",
  "errorMessage": "파일을 찾을 수 없습니다",
  "phase": "Execution",
  "isRecoverable": false
}
```

## 구현 예제

### 1. Console UI (기본 제공)

```csharp
public class ConsoleEventHandler : IAgentEventHandler
{
    public Task OnIntentAnalysisStartAsync(AgentPhaseEvent evt)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("\n🔍 의도 분석 중...");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    public Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✅ {evt.TaskDescription}");
        Console.ResetColor();
        return Task.CompletedTask;
    }
    // ... 나머지 메서드
}
```

### 2. Web API (SSE)

```csharp
public class WebApiEventHandler : IAgentEventHandler
{
    private readonly ConcurrentQueue<object> _eventQueue = new();

    public async IAsyncEnumerable<object> GetEventsAsync()
    {
        while (true)
        {
            if (_eventQueue.TryDequeue(out var evt))
                yield return evt;
            await Task.Delay(10);
        }
    }

    public Task OnStepCompleteAsync(StepExecutionEvent evt)
    {
        _eventQueue.Enqueue(new { type = "step_complete", data = evt });
        return Task.CompletedTask;
    }
    // ... 나머지 메서드
}
```

**ASP.NET Core 컨트롤러**:
```csharp
[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    [HttpPost("execute")]
    public async Task ExecuteAsync([FromBody] AgentRequest request)
    {
        var handler = new WebApiEventHandler();
        var orchestrator = new AgentOrchestrator(/* ... */, handler);

        Response.ContentType = "text/event-stream";

        await foreach (var evt in handler.GetEventsAsync())
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(evt)}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}
```

### 3. Blazor UI

```csharp
@inject AgentService AgentService

<div class="agent-ui">
    @foreach (var evt in _events)
    {
        @if (evt is StepExecutionEvent step)
        {
            <div class="step @(step.IsSuccess ? "success" : "error")">
                [@step.StepNumber/@step.TotalSteps] @step.Description
            </div>
        }
    }
</div>

@code {
    private List<AgentEventBase> _events = new();

    private async Task ExecuteAsync(string input)
    {
        await AgentService.ExecuteAsync(input, OnEventAsync);
    }

    private Task OnEventAsync(AgentEventBase evt)
    {
        _events.Add(evt);
        StateHasChanged();
        return Task.CompletedTask;
    }
}
```

### 4. 메트릭 수집

```csharp
public class MetricsEventHandler : IAgentEventHandler
{
    private readonly IMetrics _metrics;

    public Task OnStepCompleteAsync(StepExecutionEvent evt)
    {
        _metrics.RecordHistogram(
            "agent.step.duration",
            evt.DurationMs,
            new[] { new KeyValuePair<string, object>("tool", evt.ToolName) }
        );

        if (evt.IsSuccess)
            _metrics.IncrementCounter("agent.step.success");
        else
            _metrics.IncrementCounter("agent.step.failure");

        return Task.CompletedTask;
    }

    public Task OnResponseCompleteAsync(ResponseCompleteEvent evt)
    {
        _metrics.RecordHistogram("agent.total.duration", evt.TotalDurationMs);
        _metrics.RecordHistogram("agent.tokens.used", evt.TokensUsed);
        return Task.CompletedTask;
    }
    // ... 나머지 메서드
}
```

### 5. 구조화 로깅

```csharp
public class LoggingEventHandler : IAgentEventHandler
{
    private readonly ILogger _logger;

    public Task OnIntentAnalysisCompleteAsync(IntentAnalysisCompleteEvent evt)
    {
        _logger.LogInformation(
            "Intent analyzed: {IntentType} with confidence {Confidence}",
            evt.IntentType,
            evt.Confidence
        );
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(AgentErrorEvent evt)
    {
        _logger.LogError(
            "Agent error in phase {Phase}: {ErrorMessage}",
            evt.Phase,
            evt.ErrorMessage
        );
        return Task.CompletedTask;
    }
    // ... 나머지 메서드
}
```

## 사용 패턴

### 단일 핸들러
```csharp
var handler = new ConsoleEventHandler();
var orchestrator = new AgentOrchestrator(/* deps */, handler);
await orchestrator.ExecuteAsync(input, context);
```

### 다중 핸들러 (권장)
```csharp
var handlers = new IAgentEventHandler[]
{
    new ConsoleEventHandler(),
    new MetricsEventHandler(metrics),
    new LoggingEventHandler(logger)
};
var composite = new CompositeEventHandler(handlers);
var orchestrator = new AgentOrchestrator(/* deps */, composite);
```

### 조건부 핸들러
```csharp
public class ConditionalEventHandler : IAgentEventHandler
{
    private readonly IAgentEventHandler _inner;
    private readonly Func<bool> _condition;

    public Task OnStepCompleteAsync(StepExecutionEvent evt)
    {
        if (_condition())
            return _inner.OnStepCompleteAsync(evt);
        return Task.CompletedTask;
    }
}
```

## 장점

### 1. 유연한 UI 통합
- ✅ CLI, Web, Desktop, Mobile 모두 동일한 인터페이스
- ✅ UI 프레임워크 독립적
- ✅ 실시간 업데이트 지원

### 2. 관심사 분리
- ✅ 비즈니스 로직에 UI 코드 없음
- ✅ 테스트 용이 (Mock Handler)
- ✅ 유지보수 간단

### 3. 확장성
- ✅ 새 핸들러 추가 쉬움
- ✅ 다중 핸들러 동시 실행
- ✅ 이벤트 필터링/변환 가능

### 4. 관찰 가능성
- ✅ 메트릭 자동 수집
- ✅ 구조화 로깅
- ✅ 분산 추적 (Distributed Tracing)

## 다음 단계

1. **AgentOrchestrator에 IAgentEventHandler 통합**
2. **기존 StreamChunk를 Event로 변환**
3. **ChatBot에서 ConsoleEventHandler 사용**
4. **Web API 예제 프로젝트 생성**
5. **문서화 및 예제 추가**
