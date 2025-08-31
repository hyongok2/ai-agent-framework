using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIAgentFramework.Core.Actions.Abstractions;
using AIAgentFramework.Core.Actions.Factories;

using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.LLM.Models;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Orchestration.Execution;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.User;
using AIAgentFramework.Orchestration.Completion;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Orchestration
{
    /// <summary>
    /// 타입 안전한 오케스트레이션 엔진 구현
    /// </summary>
    public class TypeSafeOrchestrationEngine : IOrchestrationEngine
    {
        private readonly ILLMFunctionRegistry _llmRegistry;
        private readonly IToolRegistry _toolRegistry;
        private readonly IExecutionContextFactory _contextFactory;
        private readonly IActionFactory _actionFactory;
        private readonly ILogger<TypeSafeOrchestrationEngine> _logger;

        public TypeSafeOrchestrationEngine(
            ILLMFunctionRegistry llmRegistry,
            IToolRegistry toolRegistry,
            IExecutionContextFactory contextFactory,
            IActionFactory actionFactory,
            ILogger<TypeSafeOrchestrationEngine> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest userRequest)
        {
            ArgumentNullException.ThrowIfNull(userRequest);
            
            var sessionId = Guid.NewGuid().ToString();
            var context = _contextFactory.CreateContext(sessionId, userRequest.Content);
            
            try
            {
                _logger.LogInformation("타입 안전한 오케스트레이션 시작: {SessionId}", sessionId);
                
                var orchestrationContext = await ExecuteInternalAsync(context, CancellationToken.None);
                return new OrchestrationResult(orchestrationContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오케스트레이션 실행 중 오류 발생: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.IsCompleted)
            {
                _logger.LogWarning("이미 완료된 컨텍스트입니다: {SessionId}", context.SessionId);
                return new OrchestrationResult(context);
            }

            try
            {
                var executionContext = _contextFactory.CreateContextFromOrchestration(context);
                var updatedContext = await ExecuteInternalAsync(executionContext, CancellationToken.None);
                return new OrchestrationResult(updatedContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오케스트레이션 계속 실행 중 오류 발생: {SessionId}", context.SessionId);
                throw;
            }
        }

        private async Task<IOrchestrationContext> ExecuteInternalAsync(IExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var orchestrationContext = new OrchestrationContext(executionContext.SessionId, executionContext.UserRequest, executionContext.Registry);

            // 실행 히스토리 복사
            foreach (var step in executionContext.ExecutionHistory)
            {
                orchestrationContext.ExecutionHistory.Add(step);
            }

            // 공유 데이터 복사
            foreach (var kvp in executionContext.SharedData)
            {
                orchestrationContext.SharedData[kvp.Key] = kvp.Value;
            }

            while (!orchestrationContext.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                // 1. 계획 수립 (타입 안전한 방식)
                var planResult = await ExecutePlanningAsync(orchestrationContext, executionContext, cancellationToken);
                if (!planResult.Success)
                {
                    orchestrationContext.SetError($"계획 수립 실패: {planResult.ErrorMessage}");
                    break;
                }

                // 2. 계획된 액션 실행 (타입 안전한 방식)
                await ExecutePlannedActionsAsync(orchestrationContext, executionContext, cancellationToken);
                
                // 3. 완료 조건 확인
                await CheckCompletionAsync(orchestrationContext, cancellationToken);
            }

            _logger.LogInformation("타입 안전한 오케스트레이션 완료: {SessionId}, 단계: {StepCount}", 
                orchestrationContext.SessionId, orchestrationContext.ExecutionHistory.Count);

            return orchestrationContext;
        }

        private async Task<ILLMResult> ExecutePlanningAsync(IOrchestrationContext orchestrationContext, IExecutionContext executionContext, CancellationToken cancellationToken)
        {
            // 타입 안전한 방식으로 planner 함수 가져오기
            if (!_llmRegistry.IsRegistered("planner"))
            {
                throw new InvalidOperationException("Planner 함수가 등록되지 않았습니다.");
            }

            var planner = _llmRegistry.Resolve("planner");
            
            var llmContext = new LLMContext
            {
                UserRequest = orchestrationContext.UserRequest,
                ExecutionHistory = orchestrationContext.ExecutionHistory,
                SharedData = orchestrationContext.SharedData
            };
            llmContext.Parameters["user_request"] = orchestrationContext.UserRequest;

            var result = await planner.ExecuteAsync(llmContext, cancellationToken);
            
            var step = new ExecutionStep
            {
                StepType = "Planning",
                Description = "타입 안전한 사용자 요청 분석 및 실행 계획 수립",
                Input = orchestrationContext.UserRequest,
                Output = result.Content,
                Success = result.Success,
                ExecutionTime = result.ExecutionTime
            };
            
            orchestrationContext.AddExecutionStep(step);
            return result;
        }

        private async Task ExecutePlannedActionsAsync(IOrchestrationContext orchestrationContext, IExecutionContext executionContext, CancellationToken cancellationToken)
        {
            if (!orchestrationContext.SharedData.ContainsKey("plan_actions"))
                return;

            var actionsData = orchestrationContext.SharedData["plan_actions"];
            
            // 타입 안전한 액션 변환
            List<IOrchestrationAction> actions;
            try
            {
                if (actionsData is List<Dictionary<string, object>> actionDictList)
                {
                    actions = _actionFactory.CreateActionsFromJsonArray(actionDictList);
                }
                else
                {
                    _logger.LogWarning("Invalid action data format in plan_actions");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "타입 안전한 액션 파싱 실패");
                return;
            }

            foreach (var action in actions)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                await ExecuteActionAsync(orchestrationContext, executionContext, action, cancellationToken);
            }
        }

        private async Task ExecuteActionAsync(IOrchestrationContext orchestrationContext, IExecutionContext executionContext, IOrchestrationAction action, CancellationToken cancellationToken)
        {
            var step = new ExecutionStep
            {
                StepType = action.Type.ToString(),
                Description = $"타입 안전한 액션 실행: {action.Name}",
                Input = action.Name,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("타입 안전한 액션 실행 시작: {ActionName} (타입: {ActionType})", action.Name, action.Type);
                
                var result = await action.ExecuteAsync(executionContext, cancellationToken);
                
                step.Success = result.IsSuccess;
                step.Output = result.Data?.ToString() ?? "";
                step.ExecutionTime = result.ExecutionTime;
                step.EndTime = DateTime.UtcNow;
                
                if (!result.IsSuccess)
                {
                    step.ErrorMessage = result.ErrorMessage;
                    _logger.LogWarning("타입 안전한 액션 실행 실패: {ActionName} - {ErrorMessage}", action.Name, result.ErrorMessage);
                }
                else
                {
                    _logger.LogDebug("타입 안전한 액션 실행 성공: {ActionName}", action.Name);
                    
                    // 결과를 공유 데이터에 저장
                    if (result.Data != null)
                    {
                        orchestrationContext.SharedData[$"action_{action.Name}_result"] = result.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.ErrorMessage = ex.Message;
                step.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "타입 안전한 액션 실행 중 예외 발생: {ActionName}", action.Name);
            }
            
            orchestrationContext.AddExecutionStep(step);
        }

        private async Task CheckCompletionAsync(IOrchestrationContext context, CancellationToken cancellationToken)
        {
            var completionChecker = new CompletionChecker();
            context.IsCompleted = completionChecker.IsCompleted(context);
            
            if (context.IsCompleted)
            {
                context.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("타입 안전한 오케스트레이션 완료 조건 충족: {SessionId}", context.SessionId);
            }
            
            await Task.CompletedTask;
        }
    }
}