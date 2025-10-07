using System.Runtime.CompilerServices;
using System.Text;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Execution.Models;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Services.Conversation;
using AIAgentFramework.LLM.Services.Evaluation;
using AIAgentFramework.LLM.Services.IntentAnalysis;
using AIAgentFramework.LLM.Services.Planning;

namespace AIAgentFramework.Execution.Services;

/// <summary>
/// AI Agent 오케스트레이터 구현체
/// 전체 워크플로우: TaskPlanner → PlanExecutor → Evaluator
/// </summary>
public class AgentOrchestrator : IOrchestrator
{
    private readonly ILLMRegistry _llmRegistry;
    private readonly PlanExecutor _planExecutor;

    public AgentOrchestrator(
        ILLMRegistry llmRegistry,
        PlanExecutor planExecutor)
    {
        _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
        _planExecutor = planExecutor ?? throw new ArgumentNullException(nameof(planExecutor));
    }


    private static string FormatStepResults(List<StepExecutionResult> steps)
    {
        if (steps == null || steps.Count == 0)
        {
            return "No step results available";
        }

        var sb = new StringBuilder();
        sb.AppendLine("```");

        foreach (var step in steps)
        {
            sb.AppendLine($"### Step {step.StepNumber}: {step.Description}");
            sb.AppendLine($"Tool: {step.ToolName}");
            sb.AppendLine($"Status: {(step.IsSuccess ? "✅ Success" : "❌ Failed")}");
            sb.AppendLine($"Execution Time: {step.ExecutionTimeMs}ms");

            if (!string.IsNullOrEmpty(step.Parameters))
            {
                sb.AppendLine($"Input Parameters:");
                sb.AppendLine(step.Parameters);
            }

            if (!string.IsNullOrEmpty(step.Output))
            {
                sb.AppendLine($"Output:");
                sb.AppendLine(step.Output);
            }

            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                sb.AppendLine($"Error: {step.ErrorMessage}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("```");
        return sb.ToString();
    }

    public async Task<IResult> ExecuteAsync(
        string userInput,
        IAgentContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            // 1. TaskPlanner 실행 - 계획 수립
            var planner = _llmRegistry.GetFunction(LLMRole.Planner)
                ?? throw new InvalidOperationException("TaskPlanner not registered");

            var planningInput = new PlanningInput
            {
                UserRequest = userInput,
                Context = context.Get<string>("CONTEXT"),
                ConversationHistory = context.Get<string>("HISTORY"),
                PreviousResults = context.Get<string>("PREVIOUS_RESULTS")
            };

            var planningResult = await ((TaskPlannerFunction)planner).ExecuteAsync(planningInput, cancellationToken);

            if (!planningResult.IsSuccess)
            {
                return new OrchestratorResult
                {
                    IsSuccess = false,
                    ErrorMessage = planningResult.ErrorMessage ?? "계획 수립 실패",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            var plan = (PlanningResult)planningResult.ParsedData!;

            if (!plan.IsExecutable)
            {
                return new OrchestratorResult
                {
                    IsSuccess = false,
                    ErrorMessage = plan.ExecutionBlocker ?? "실행 불가능한 계획",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    PlanSummary = plan.Summary
                };
            }

            // 2. PlanExecutor 실행 - 계획 실행
            var executionInput = new ExecutionInput
            {
                Plan = plan,
                UserRequest = userInput
            };

            var executionResult = await _planExecutor.ExecuteAsync(
                executionInput,
                context,
                cancellationToken: cancellationToken);

            if (!executionResult.IsSuccess)
            {
                return new OrchestratorResult
                {
                    IsSuccess = false,
                    ErrorMessage = executionResult.ErrorMessage ?? "계획 실행 실패",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    PlanSummary = plan.Summary,
                    ExecutionSummary = executionResult.Summary
                };
            }

            // 3. Evaluator 실행 - 결과 평가
            var evaluator = _llmRegistry.GetFunction(LLMRole.Evaluator)
                ?? throw new InvalidOperationException("Evaluator not registered");

            var evaluationInput = new EvaluationInput
            {
                TaskDescription = userInput,
                ExecutionResult = executionResult.Summary ?? "실행 완료",
                DetailedStepResults = FormatStepResults(executionResult.Steps),
                ExpectedOutcome = null,
                EvaluationCriteria = null
            };

            var evaluationResult = await ((EvaluatorFunction)evaluator).ExecuteAsync(evaluationInput, cancellationToken);
            var evaluation = (EvaluationResult)evaluationResult.ParsedData!;

            return new OrchestratorResult
            {
                IsSuccess = true,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                PlanSummary = plan.Summary,
                ExecutionSummary = executionResult.Summary,
                EvaluationScore = (int)(evaluation.QualityScore * 100),
                EvaluationSummary = evaluation.Assessment,
                Improvements = evaluation.Recommendations
            };
        }
        catch (Exception ex)
        {
            return new OrchestratorResult
            {
                IsSuccess = false,
                ErrorMessage = $"Orchestrator 실행 중 오류: {ex.Message}",
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public async IAsyncEnumerable<IStreamChunk> ExecuteStreamAsync(
        string userInput,
        IAgentContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. IntentAnalyzer 실행 - 의도 파악 및 즉시 응답 가능 여부 판단
        yield return StreamChunk.Status("🔍 의도 분석 중...");

        var intentAnalyzer = _llmRegistry.GetFunction(LLMRole.IntentAnalyzer)
            ?? throw new InvalidOperationException("IntentAnalyzer not registered");

        var intentInput = new IntentAnalysisInput
        {
            UserInput = userInput,
            ConversationHistory = context.Get<string>("HISTORY"),
            Context = context.Get<string>("CONTEXT")
        };

        IntentAnalysisResult? intentResult = null;
        await foreach (var chunk in ((IntentAnalyzerFunction)intentAnalyzer).ExecuteStreamAsync(intentInput, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return StreamChunk.Text(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                intentResult = (IntentAnalysisResult)chunk.ParsedResult;
            }
        }

        if (intentResult == null)
        {
            yield return StreamChunk.Error("의도 분석 실패");
            yield return StreamChunk.Complete();
            yield break;
        }

        // 2. 의도에 따른 라우팅
        if (intentResult.IntentType == IntentType.Chat || intentResult.IntentType == IntentType.Question)
        {
            // 즉시 응답 가능 - DirectResponse 반환
            if (!string.IsNullOrEmpty(intentResult.DirectResponse))
            {
                yield return StreamChunk.Text(intentResult.DirectResponse);
            }
            else
            {
                yield return StreamChunk.Text("응답을 생성할 수 없습니다.");
            }

            yield return StreamChunk.Complete("응답 완료");
            yield break;
        }

        // 3. Task인 경우 - 계획 수립 필요
        yield return StreamChunk.Status($"\n\n✅ 의도 파악 완료: {intentResult.TaskDescription}\n");
        yield return StreamChunk.Status("📋 계획 수립 중...");

        var planner = _llmRegistry.GetFunction(LLMRole.Planner)
            ?? throw new InvalidOperationException("TaskPlanner not registered");

        var planningInput = new PlanningInput
        {
            UserRequest = userInput,
            Context = context.Get<string>("CONTEXT"),
            ConversationHistory = context.Get<string>("HISTORY"),
            PreviousResults = context.Get<string>("PREVIOUS_RESULTS")
        };

        PlanningResult? plan = null;
        await foreach (var chunk in ((TaskPlannerFunction)planner).ExecuteStreamAsync(planningInput, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return StreamChunk.Text(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                plan = (PlanningResult)chunk.ParsedResult;
            }
        }

        if (plan == null)
        {
            yield return StreamChunk.Error("계획 수립 실패");
            yield return StreamChunk.Complete();
            yield break;
        }

        // 4. 실행 불가능한 계획인 경우 에러 처리
        if (!plan.IsExecutable)
        {
            yield return StreamChunk.Error(plan.ExecutionBlocker ?? "계획 실행 불가");
            yield return StreamChunk.Complete();
            yield break;
        }

        yield return StreamChunk.Status($"\n\n✅ 계획 수립 완료: {plan.Summary}\n");

        // 5. 계획 실행
        yield return StreamChunk.Status("⚙️ 계획 실행 중...\n");

        // PlanExecutor 실행 (스트리밍)
        var executionInput = new ExecutionInput
        {
            Plan = plan,
            UserRequest = userInput
        };

        // 스트리밍 청크를 수집하기 위한 리스트
        var executionChunks = new List<string>();

        var executionResult = await _planExecutor.ExecuteAsync(
            executionInput,
            context,
            onStreamChunk: (chunk) =>
            {
                executionChunks.Add(chunk);
            },
            cancellationToken: cancellationToken);

        // 수집한 스트리밍 청크들을 yield
        foreach (var chunk in executionChunks)
        {
            yield return StreamChunk.Text(chunk);
        }

        if (!executionResult.IsSuccess)
        {
            yield return StreamChunk.Error($"계획 실행 실패: {executionResult.ErrorMessage}");
            yield return StreamChunk.Complete();
            yield break;
        }

        yield return StreamChunk.Status($"\n\n✅ 계획 실행 완료\n");
        yield return StreamChunk.Status("🔍 결과 평가 중...\n");

        // 6. Evaluator 실행 (스트리밍)
        var evaluator = _llmRegistry.GetFunction(LLMRole.Evaluator)
            ?? throw new InvalidOperationException("Evaluator not registered");

        var evaluationInput = new EvaluationInput
        {
            TaskDescription = userInput,
            ExecutionResult = executionResult.Summary ?? "실행 완료",
            DetailedStepResults = FormatStepResults(executionResult.Steps),
            ExpectedOutcome = null,
            EvaluationCriteria = null
        };

        EvaluationResult? evaluation = null;
        await foreach (var chunk in ((EvaluatorFunction)evaluator).ExecuteStreamAsync(evaluationInput, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return StreamChunk.Text(chunk.Content);
            }

            if (chunk.IsFinal && chunk.ParsedResult != null)
            {
                evaluation = (EvaluationResult)chunk.ParsedResult;
            }
        }

        yield return StreamChunk.Status($"\n\n✅ 평가 완료 (점수: {(int)(evaluation?.QualityScore * 100 ?? 0)}점)\n");

        // 7. ConversationFunction으로 사용자에게 친절하게 설명
        yield return StreamChunk.Status("\n💬 결과 설명 중...\n\n");

        var conversationalist = _llmRegistry.GetFunction(LLMRole.Conversationalist);
        if (conversationalist != null && evaluation != null)
        {
            // 평가 결과를 대화 형식으로 설명하기 위한 프롬프트 생성
            var explanationPrompt = $@"사용자가 요청한 작업: ""{userInput}""

평가 결과:
- 성공 여부: {(evaluation.IsSuccess ? "성공" : "실패")}
- 품질 점수: {(int)(evaluation.QualityScore * 100)}점
- 평가 내용: {evaluation.Assessment}
- 강점: {string.Join(", ", evaluation.Strengths ?? new List<string>())}
- 약점: {string.Join(", ", evaluation.Weaknesses ?? new List<string>())}
- 권장사항: {string.Join(", ", evaluation.Recommendations ?? new List<string>())}

위 평가 결과를 바탕으로 사용자에게 친절하고 대화체로 작업 결과를 설명해주세요.";

            var conversationInput = new ConversationInput
            {
                UserMessage = explanationPrompt,
                ConversationHistory = context.Get<string>("HISTORY"),
                SystemContext = "작업 실행 결과를 사용자에게 친절하게 설명하는 역할"
            };

            await foreach (var chunk in ((ConversationFunction)conversationalist).ExecuteStreamAsync(conversationInput, cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    yield return StreamChunk.Text(chunk.Content);
                }
            }
        }
        else
        {
            // Conversationalist가 없으면 기본 메시지
            yield return StreamChunk.Text($"작업이 완료되었습니다. 품질 점수: {(int)(evaluation?.QualityScore * 100 ?? 0)}점");
        }

        yield return StreamChunk.Complete($"전체 워크플로우 완료");
    }
}
