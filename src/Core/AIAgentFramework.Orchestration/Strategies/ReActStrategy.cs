
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Models;

using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Tools.Models;
using AIAgentFramework.Core.Common.Registry;

namespace AIAgentFramework.Orchestration.Strategies;

/// <summary>
/// ReAct (Reasoning + Acting) 전략 구현
/// 추론과 행동을 반복하며 문제를 해결하는 전략
/// </summary>
public class ReActStrategy : IOrchestrationStrategy
{
    private readonly IRegistry _registry;
    private readonly ILogger<ReActStrategy> _logger;
    private readonly int _maxIterations;

    public ReActStrategy(
        IRegistry registry,
        ILogger<ReActStrategy> logger,
        int maxIterations = 15)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxIterations = maxIterations;
    }

    public string Name => "ReAct";
    
    public string Description => "추론(Reasoning)과 행동(Acting)을 반복하며 단계적으로 문제를 해결하는 전략";
    
    public int Priority => 90;

    public bool CanHandle(IOrchestrationContext context)
    {
        // 복잡한 추론이 필요한 작업에 적합
        var keywords = new[] { "분석", "추론", "탐색", "조사", "연구", "analyze", "investigate", "research" };
        return keywords.Any(k => context.UserRequest.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IOrchestrationResult> ExecuteAsync(
        IOrchestrationContext context,
        CancellationToken cancellationToken = default)
    {
        var orchestrationContext = context as OrchestrationContext 
            ?? throw new InvalidCastException("Invalid context type");
        
        try
        {
            _logger.LogInformation("[ReAct] 전략 시작: {SessionId}", context.SessionId);
            
            var thoughtActionPairs = new List<ThoughtActionPair>();
            int iteration = 0;
            
            while (!orchestrationContext.IsCompleted && iteration < _maxIterations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                iteration++;
                
                _logger.LogDebug("[ReAct] Iteration {Iteration}/{Max}", iteration, _maxIterations);
                
                // 1단계: Thought (추론)
                var thought = await GenerateThoughtAsync(orchestrationContext, thoughtActionPairs, cancellationToken);
                _logger.LogDebug("[ReAct] Thought: {Thought}", thought.Content);
                
                // 2단계: Action Decision (행동 결정)
                var action = await DecideActionAsync(orchestrationContext, thought, cancellationToken);
                
                if (action == null || action.Type == "finish")
                {
                    // 완료 판단
                    orchestrationContext.IsCompleted = true;
                    orchestrationContext.CompletedAt = DateTime.UtcNow;
                    _logger.LogInformation("[ReAct] 작업 완료 판단");
                    break;
                }
                
                // 3단계: Action Execution (행동 실행)
                var observation = await ExecuteActionAsync(orchestrationContext, action, cancellationToken);
                
                // 4단계: Store Thought-Action-Observation
                thoughtActionPairs.Add(new ThoughtActionPair
                {
                    Thought = thought.Content,
                    Action = action,
                    Observation = observation.Content
                });
                
                // SharedData에 중간 결과 저장
                orchestrationContext.SharedData[$"react_iteration_{iteration}"] = new
                {
                    thought = thought.Content,
                    action = action.ToString(),
                    observation = observation.Content
                };
                
                // 5단계: Check if goal is achieved
                if (await IsGoalAchievedAsync(orchestrationContext, thoughtActionPairs, cancellationToken))
                {
                    orchestrationContext.IsCompleted = true;
                    orchestrationContext.CompletedAt = DateTime.UtcNow;
                    _logger.LogInformation("[ReAct] 목표 달성 확인");
                    break;
                }
            }
            
            if (iteration >= _maxIterations)
            {
                _logger.LogWarning("[ReAct] 최대 반복 횟수 도달: {MaxIterations}", _maxIterations);
                orchestrationContext.SetError($"최대 반복 횟수({_maxIterations}) 초과");
            }
            
            // 최종 답변 생성
            await GenerateFinalAnswerAsync(orchestrationContext, thoughtActionPairs, cancellationToken);
            
            return new OrchestrationResult(orchestrationContext);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[ReAct] 작업 취소됨");
            orchestrationContext.SetError("작업이 취소되었습니다");
            return new OrchestrationResult(orchestrationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReAct] 전략 실행 중 오류");
            orchestrationContext.SetError($"전략 실행 오류: {ex.Message}");
            return new OrchestrationResult(orchestrationContext);
        }
    }

    private async Task<ILLMResult> GenerateThoughtAsync(
        OrchestrationContext context,
        List<ThoughtActionPair> history,
        CancellationToken cancellationToken)
    {
        var reasoner = _registry.GetLLMFunction("reasoner") 
            ?? _registry.GetLLMFunction("analyzer")
            ?? throw new InvalidOperationException("Reasoner/Analyzer LLM 기능을 찾을 수 없습니다");
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            SharedData = context.SharedData
        };
        
        // ReAct 프롬프트 구성
        var prompt = BuildReActPrompt(context.UserRequest, history);
        llmContext.Parameters["prompt"] = prompt;
        llmContext.Parameters["instruction"] = "다음 단계를 위한 추론을 생성하세요.";
        
        var result = await reasoner.ExecuteAsync(llmContext, cancellationToken);
        
        // 실행 단계 기록
        context.AddExecutionStep(new ExecutionStep
        {
            StepType = "Thought",
            Description = "추론 생성",
            Input = prompt,
            Output = result.Content,
            Success = result.Success,
            ExecutionTime = result.ExecutionTime
        });
        
        return result;
    }

    private async Task<ReActAction?> DecideActionAsync(
        OrchestrationContext context,
        ILLMResult thought,
        CancellationToken cancellationToken)
    {
        var planner = _registry.GetLLMFunction("planner")
            ?? throw new InvalidOperationException("Planner LLM 기능을 찾을 수 없습니다");
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            SharedData = context.SharedData
        };
        
        llmContext.Parameters["thought"] = thought.Content;
        llmContext.Parameters["available_tools"] = GetAvailableTools();
        llmContext.Parameters["instruction"] = "추론을 바탕으로 다음 행동을 결정하세요. 목표가 달성되었다면 'finish'를 선택하세요.";
        
        var result = await planner.ExecuteAsync(llmContext, cancellationToken);
        
        try
        {
            return ParseAction(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ReAct] 액션 파싱 실패");
            return null;
        }
    }

    private async Task<ObservationResult> ExecuteActionAsync(
        OrchestrationContext context,
        ReActAction action,
        CancellationToken cancellationToken)
    {
        var step = new ExecutionStep
        {
            StepType = "Action",
            Description = $"{action.Type}: {action.Name}",
            Input = JsonSerializer.Serialize(action.Parameters),
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            string observation;
            
            if (action.Type == "tool")
            {
                var tool = _registry.GetTool(action.Name);
                if (tool != null)
                {
                    var toolInput = new ToolInput { Parameters = action.Parameters };
                    var result = await tool.ExecuteAsync(toolInput, cancellationToken);
                    observation = JsonSerializer.Serialize(result.Data);
                    step.ExecutionTime = result.ExecutionTime;
                }
                else
                {
                    observation = $"도구를 찾을 수 없습니다: {action.Name}";
                }
            }
            else if (action.Type == "llm")
            {
                var llmFunction = _registry.GetLLMFunction(action.Name);
                if (llmFunction != null)
                {
                    var llmContext = new LLMContext
                    {
                        Parameters = action.Parameters,
                        SharedData = context.SharedData
                    };
                    var result = await llmFunction.ExecuteAsync(llmContext, cancellationToken);
                    observation = result.Content;
                    step.ExecutionTime = result.ExecutionTime;
                }
                else
                {
                    observation = $"LLM 기능을 찾을 수 없습니다: {action.Name}";
                }
            }
            else
            {
                observation = $"알 수 없는 액션 타입: {action.Type}";
            }
            
            step.Output = observation;
            step.Success = true;
            
            return new ObservationResult { Content = observation, Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReAct] 액션 실행 오류");
            step.Success = false;
            step.ErrorMessage = ex.Message;
            return new ObservationResult { Content = ex.Message, Success = false };
        }
        finally
        {
            step.EndTime = DateTime.UtcNow;
            context.AddExecutionStep(step);
        }
    }

    private async Task<bool> IsGoalAchievedAsync(
        OrchestrationContext context,
        List<ThoughtActionPair> history,
        CancellationToken cancellationToken)
    {
        if (history.Count < 2) return false; // 최소 2번의 사이클 필요
        
        var evaluator = _registry.GetLLMFunction("evaluator") 
            ?? _registry.GetLLMFunction("completion_checker");
            
        if (evaluator == null)
        {
            // 기본 로직: 특정 키워드 확인
            var lastObservation = history.Last().Observation.ToLower();
            return lastObservation.Contains("완료") || 
                   lastObservation.Contains("성공") ||
                   lastObservation.Contains("complete") ||
                   lastObservation.Contains("success");
        }
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            Parameters = new Dictionary<string, object>
            {
                ["user_request"] = context.UserRequest,
                ["history"] = JsonSerializer.Serialize(history),
                ["instruction"] = "사용자의 요청이 충분히 달성되었는지 판단하세요."
            }
        };
        
        var result = await evaluator.ExecuteAsync(llmContext, cancellationToken);
        
        try
        {
            using var doc = JsonDocument.Parse(result.Content);
            return doc.RootElement.GetProperty("is_complete").GetBoolean();
        }
        catch
        {
            return false;
        }
    }

    private async Task GenerateFinalAnswerAsync(
        OrchestrationContext context,
        List<ThoughtActionPair> history,
        CancellationToken cancellationToken)
    {
        var summarizer = _registry.GetLLMFunction("summarizer")
            ?? _registry.GetLLMFunction("generator");
            
        if (summarizer == null)
        {
            _logger.LogWarning("[ReAct] Summarizer를 찾을 수 없음");
            return;
        }
        
        var llmContext = new LLMContext
        {
            UserRequest = context.UserRequest,
            Parameters = new Dictionary<string, object>
            {
                ["user_request"] = context.UserRequest,
                ["history"] = JsonSerializer.Serialize(history),
                ["instruction"] = "전체 과정을 요약하고 최종 답변을 생성하세요."
            }
        };
        
        var result = await summarizer.ExecuteAsync(llmContext, cancellationToken);
        
        context.SharedData["final_answer"] = result.Content;
        context.AddExecutionStep(new ExecutionStep
        {
            StepType = "Summary",
            Description = "최종 답변 생성",
            Output = result.Content,
            Success = result.Success,
            ExecutionTime = result.ExecutionTime
        });
    }

    private string BuildReActPrompt(string userRequest, List<ThoughtActionPair> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"사용자 요청: {userRequest}");
        sb.AppendLine();
        
        if (history.Any())
        {
            sb.AppendLine("이전 추론과 행동:");
            foreach (var pair in history)
            {
                sb.AppendLine($"Thought: {pair.Thought}");
                sb.AppendLine($"Action: {pair.Action}");
                sb.AppendLine($"Observation: {pair.Observation}");
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("다음 단계를 위한 추론을 생성하세요.");
        return sb.ToString();
    }

    private string GetAvailableTools()
    {
        var tools = _registry.GetAllTools();
        return JsonSerializer.Serialize(tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            category = t.Category
        }));
    }

    private ReActAction? ParseAction(string actionContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(actionContent);
            var root = doc.RootElement;
            
            return new ReActAction
            {
                Type = root.GetProperty("type").GetString() ?? "unknown",
                Name = root.GetProperty("name").GetString() ?? "",
                Parameters = root.TryGetProperty("parameters", out var paramsElement)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) ?? new()
                    : new()
            };
        }
        catch
        {
            // 텍스트 형식 파싱 시도
            if (actionContent.Contains("finish", StringComparison.OrdinalIgnoreCase))
            {
                return new ReActAction { Type = "finish" };
            }
            
            return null;
        }
    }
    
    private class ThoughtActionPair
    {
        public string Thought { get; set; } = "";
        public ReActAction Action { get; set; } = new();
        public string Observation { get; set; } = "";
    }
    
    private class ReActAction
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        public override string ToString()
        {
            return $"{Type}:{Name}({string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"))})";
        }
    }
    
    private class ObservationResult
    {
        public string Content { get; set; } = "";
        public bool Success { get; set; }
    }
}