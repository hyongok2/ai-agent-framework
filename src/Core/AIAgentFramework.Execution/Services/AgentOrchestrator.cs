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

            // ✅ PartialSuccess나 Failed여도 Evaluator는 호출 (부분 결과라도 평가)
            // (기존 if문 제거)

            // 3. Evaluator 실행 - 결과 평가 (항상 실행)
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

        // 3. Task인 경우 - 자가 개선 루프 (최대 5회 반복)
        yield return StreamChunk.Status($"\n\n✅ 의도 파악 완료: {intentResult.TaskDescription}\n");

        const double QUALITY_THRESHOLD = 0.75;  // 75점 기준
        const int MAX_ITERATIONS = 5;

        var planner = _llmRegistry.GetFunction(LLMRole.Planner)
            ?? throw new InvalidOperationException("TaskPlanner not registered");
        var evaluator = _llmRegistry.GetFunction(LLMRole.Evaluator);

        PlanningResult? plan = null;
        ExecutionResult? executionResult = null;
        EvaluationResult? evaluation = null;
        string? previousAttemptSummary = null;
        string? evaluationFeedback = null;

        for (int iteration = 1; iteration <= MAX_ITERATIONS; iteration++)
        {
            // 3-1. 계획 수립 (재시도 시 피드백 포함)
            yield return StreamChunk.Status(iteration == 1
                ? "📋 계획 수립 중..."
                : $"📋 계획 재수립 중... ({iteration}차 시도)");

            var planningInput = new PlanningInput
            {
                UserRequest = userInput,
                Context = context.Get<string>("CONTEXT"),
                ConversationHistory = context.Get<string>("HISTORY"),
                PreviousResults = context.Get<string>("PREVIOUS_RESULTS"),
                IterationNumber = iteration,
                PreviousAttemptSummary = previousAttemptSummary,
                EvaluationFeedback = evaluationFeedback,
                FailureReason = iteration > 1 && evaluation != null
                    ? string.Join(", ", evaluation.Weaknesses ?? new List<string>())
                    : null
            };

            plan = null;
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

            if (!plan.IsExecutable)
            {
                yield return StreamChunk.Error(plan.ExecutionBlocker ?? "계획 실행 불가");
                yield return StreamChunk.Complete();
                yield break;
            }

            yield return StreamChunk.Status($"\n\n✅ 계획 수립 완료: {plan.Summary}\n");

            // 3-2. 계획 실행
            yield return StreamChunk.Status("⚙️ 계획 실행 중...\n");

            var executionInput = new ExecutionInput
            {
                Plan = plan,
                UserRequest = userInput
            };

            var executionChunks = new List<string>();
            executionResult = await _planExecutor.ExecuteAsync(
                executionInput,
                context,
                onStreamChunk: (chunk) => executionChunks.Add(chunk),
                cancellationToken: cancellationToken);

            foreach (var chunk in executionChunks)
            {
                yield return StreamChunk.Text(chunk);
            }

            if (executionResult.Status == ExecutionStatus.PartialSuccess)
            {
                yield return StreamChunk.Status($"\n\n⚠️ 계획 부분 실행 완료: {executionResult.Summary}\n");
            }
            else if (executionResult.Status == ExecutionStatus.Failed)
            {
                yield return StreamChunk.Status($"\n\n❌ 계획 실행 실패: {executionResult.Summary}\n");
            }
            else
            {
                yield return StreamChunk.Status($"\n\n✅ 계획 실행 완료\n");
            }

            // 3-3. 품질 평가
            yield return StreamChunk.Status("\n🔍 결과 평가 중...\n");

            evaluation = null;
            if (evaluator != null)
            {
                var evaluationInput = new EvaluationInput
                {
                    TaskDescription = userInput,
                    ExecutionResult = executionResult.Summary ?? "실행 완료",
                    DetailedStepResults = FormatStepResults(executionResult.Steps),
                    ExpectedOutcome = null,
                    EvaluationCriteria = null
                };

                await foreach (var chunk in ((EvaluatorFunction)evaluator).ExecuteStreamAsync(evaluationInput, cancellationToken))
                {
                    if (chunk.IsFinal && chunk.ParsedResult != null)
                    {
                        evaluation = (EvaluationResult)chunk.ParsedResult;
                    }
                }
            }

            // 3-4. 품질 기준 체크
            double qualityScore = evaluation?.QualityScore ?? 0.0;
            yield return StreamChunk.Status($"📊 품질 점수: {(int)(qualityScore * 100)}점\n");

            if (qualityScore >= QUALITY_THRESHOLD)
            {
                yield return StreamChunk.Status($"✅ 품질 기준 달성! ({iteration}차 시도)\n");
                break;  // 성공! 루프 종료
            }
            else if (iteration < MAX_ITERATIONS)
            {
                yield return StreamChunk.Status($"⚠️ 품질 기준 미달 ({(int)(qualityScore * 100)}점 < 75점). 재시도합니다...\n");

                // 다음 반복을 위한 피드백 준비
                previousAttemptSummary = executionResult.Summary;
                evaluationFeedback = evaluation != null
                    ? $"평가: {evaluation.Assessment}\n권장사항: {string.Join(", ", evaluation.Recommendations ?? new List<string>())}"
                    : "평가 정보 없음";
            }
            else
            {
                yield return StreamChunk.Status($"⚠️ 최대 시도 횟수 도달 ({MAX_ITERATIONS}회). 현재 결과로 진행합니다.\n");
            }
        }

        // ✅ 7. Conversationalist로 사용자 질문에 답변 (실행 결과 + 평가 활용)
        yield return StreamChunk.Status("\n💬 응답 생성 중...\n\n");

        var conversationalist = _llmRegistry.GetFunction(LLMRole.Conversationalist);
        if (conversationalist != null && executionResult != null)
        {
            // ✅ 사용자의 원래 질문 + 실행 결과 + 평가 결과를 결합
            var answerPrompt = $@"사용자 질문: ""{userInput}""

실행 결과:
{FormatStepResults(executionResult.Steps)}

품질 평가:
- 성공 여부: {(evaluation?.IsSuccess ?? true ? "성공" : "실패")}
- 품질 점수: {(evaluation != null ? (int)(evaluation.QualityScore * 100) : 0)}점
- 평가: {evaluation?.Assessment ?? "평가 없음"}

위 실행 결과를 바탕으로 사용자의 질문에 답변해주세요.
- 진행 상황이나 평가를 설명하는게 아니라, 질문 자체에 대한 답변을 하세요
- 실행 결과 데이터를 활용하여 구체적으로 답변하세요
- 부분 실패가 있었다면, 가능한 범위 내에서 답변하고 제한사항을 설명하세요
- 품질 평가는 참고만 하고, 사용자에게 점수를 언급할 필요는 없습니다";

            var conversationInput = new ConversationInput
            {
                UserMessage = answerPrompt,
                ConversationHistory = context.Get<string>("HISTORY"),
                SystemContext = "사용자 질문에 실행 결과를 활용하여 답변하는 역할"
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
            // Conversationalist가 없거나 실행 결과가 없으면 기본 응답
            yield return StreamChunk.Text(executionResult != null
                ? $"작업이 {executionResult.Summary}"
                : "작업 실행 결과를 가져올 수 없습니다.");
        }

        yield return StreamChunk.Complete($"응답 완료");
    }
}
