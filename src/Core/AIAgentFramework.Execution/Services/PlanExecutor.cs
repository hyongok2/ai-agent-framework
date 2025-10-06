using System.Diagnostics;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.LLM.Services.Planning;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// Plan을 실제로 실행하는 Executor (Orchestration Layer)
/// </summary>
public class PlanExecutor : IExecutor
{
    private readonly IExecutableResolver _executableResolver;
    private readonly IParameterProcessor _parameterProcessor;
    private readonly IStepExecutor _toolExecutor;
    private readonly IStepExecutor _llmExecutor;

    public PlanExecutor(
        IExecutableResolver executableResolver,
        IParameterProcessor parameterProcessor,
        IStepExecutor toolExecutor,
        IStepExecutor llmExecutor)
    {
        _executableResolver = executableResolver ?? throw new ArgumentNullException(nameof(executableResolver));
        _parameterProcessor = parameterProcessor ?? throw new ArgumentNullException(nameof(parameterProcessor));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _llmExecutor = llmExecutor ?? throw new ArgumentNullException(nameof(llmExecutor));
    }

    public async Task<ExecutionResult> ExecuteAsync(
        ExecutionInput input,
        IAgentContext agentContext,
        Action<StepExecutionResult>? onStepCompleted = null,
        Action<string>? onStreamChunk = null,
        CancellationToken cancellationToken = default)
    {
        if (!input.Plan.IsExecutable)
        {
            return new ExecutionResult
            {
                IsSuccess = false,
                Steps = new List<StepExecutionResult>(),
                ErrorMessage = $"계획이 실행 불가능합니다: {input.Plan.ExecutionBlocker}",
                TotalExecutionTimeMs = 0
            };
        }

        var totalStopwatch = Stopwatch.StartNew();
        var stepResults = new List<StepExecutionResult>();

        foreach (var step in input.Plan.Steps.OrderBy(s => s.StepNumber))
        {
            var stepResult = await ExecuteStepAsync(step, input.UserRequest, agentContext, onStreamChunk, cancellationToken);
            stepResults.Add(stepResult);

            // 단계 완료 콜백 호출 (스트리밍 출력용)
            onStepCompleted?.Invoke(stepResult);

            // 실행 실패 시 중단
            if (!stepResult.IsSuccess)
            {
                totalStopwatch.Stop();
                return new ExecutionResult
                {
                    IsSuccess = false,
                    Steps = stepResults,
                    ErrorMessage = $"Step {step.StepNumber} 실행 실패: {stepResult.ErrorMessage}",
                    TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds
                };
            }

            // 성공 시 결과를 AgentContext에 저장 (다음 단계에서 사용)
            if (!string.IsNullOrEmpty(step.OutputVariable) && stepResult.Output != null)
            {
                agentContext.Set(step.OutputVariable, stepResult.Output);
            }
        }

        totalStopwatch.Stop();

        return new ExecutionResult
        {
            IsSuccess = true,
            Steps = stepResults,
            Summary = $"{stepResults.Count}개 단계 모두 성공적으로 완료",
            TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds
        };
    }

    private async Task<StepExecutionResult> ExecuteStepAsync(
        TaskStep step,
        string userRequest,
        IAgentContext agentContext,
        Action<string>? onStreamChunk,
        CancellationToken cancellationToken)
    {
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Resolve: Tool 또는 LLM Function 찾기
            var executable = _executableResolver.Resolve(step.ToolName);
            if (executable == null)
            {
                stepStopwatch.Stop();
                return CreateFailureResult(step, $"Tool 또는 LLM Function을 찾을 수 없습니다: {step.ToolName}", stepStopwatch);
            }

            // 2. Process: 파라미터 처리 (변수 치환 + 검증 + 자동 생성)
            var paramResult = await _parameterProcessor.ProcessAsync(
                executable.Tool,
                step.Parameters,
                userRequest,
                step.Description,
                agentContext,
                cancellationToken);

            if (!paramResult.IsSuccess)
            {
                stepStopwatch.Stop();
                return CreateFailureResult(step, paramResult.ErrorMessage!, stepStopwatch);
            }

            // 3. Execute: Tool 또는 LLM Function 실행
            var executor = executable.Type == ExecutableType.Tool ? _toolExecutor : _llmExecutor;
            var executionResult = await executor.ExecuteAsync(
                step,
                executable,
                paramResult.ProcessedParameters,
                userRequest,
                agentContext,
                onStreamChunk,
                cancellationToken);

            stepStopwatch.Stop();

            // 4. Build Result
            return new StepExecutionResult
            {
                StepNumber = step.StepNumber,
                Description = step.Description,
                ToolName = step.ToolName,
                Parameters = paramResult.ProcessedParameters,
                IsSuccess = executionResult.IsSuccess,
                Output = executionResult.Output,
                ErrorMessage = executionResult.ErrorMessage,
                ExecutionTimeMs = stepStopwatch.ElapsedMilliseconds,
                OutputVariable = step.OutputVariable
            };
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            return CreateFailureResult(step, $"실행 중 예외 발생: {ex.Message}", stepStopwatch);
        }
    }

    private StepExecutionResult CreateFailureResult(TaskStep step, string errorMessage, Stopwatch stopwatch)
    {
        return new StepExecutionResult
        {
            StepNumber = step.StepNumber,
            Description = step.Description,
            ToolName = step.ToolName,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }
}
