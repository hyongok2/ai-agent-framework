using System;
using System.Collections.Generic;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Context
{
    /// <summary>
    /// 실행 컨텍스트 팩토리 구현
    /// </summary>
    public class ExecutionContextFactory : IExecutionContextFactory
    {
        private readonly ILLMFunctionRegistry _llmRegistry;
        private readonly IToolRegistry _toolRegistry;
        private readonly ILogger<ExecutionContextFactory> _logger;

        public ExecutionContextFactory(
            ILLMFunctionRegistry llmRegistry,
            IToolRegistry toolRegistry,
            ILogger<ExecutionContextFactory> logger)
        {
            _llmRegistry = llmRegistry ?? throw new ArgumentNullException(nameof(llmRegistry));
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 새로운 실행 컨텍스트를 생성합니다
        /// </summary>
        public IExecutionContext CreateContext(string sessionId, string userRequest)
        {
            ArgumentException.ThrowIfNullOrEmpty(sessionId);
            ArgumentException.ThrowIfNullOrEmpty(userRequest);

            var context = new ExecutionContext(
                sessionId,
                userRequest,
                _llmRegistry,
                _toolRegistry);

            _logger.LogDebug("새로운 실행 컨텍스트 생성됨: {SessionId}", sessionId);
            return context;
        }

        /// <summary>
        /// 기존 오케스트레이션 컨텍스트로부터 실행 컨텍스트를 생성합니다
        /// </summary>
        public IExecutionContext CreateContextFromOrchestration(IOrchestrationContext orchestrationContext)
        {
            ArgumentNullException.ThrowIfNull(orchestrationContext);

            var context = new ExecutionContext(
                orchestrationContext.SessionId,
                orchestrationContext.UserRequest ?? string.Empty,
                _llmRegistry,
                _toolRegistry);

            // 기존 실행 히스토리 복사
            foreach (var step in orchestrationContext.ExecutionHistory)
            {
                context.ExecutionHistory.Add(step);
            }

            // 공유 데이터 복사
            foreach (var kvp in orchestrationContext.SharedData)
            {
                context.SharedData[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug("기존 오케스트레이션 컨텍스트로부터 실행 컨텍스트 생성됨: {SessionId}", 
                orchestrationContext.SessionId);
            
            return context;
        }
    }

    /// <summary>
    /// 실행 컨텍스트 구현
    /// </summary>
    internal class ExecutionContext : IExecutionContext
    {
        private readonly ILLMFunctionRegistry _llmRegistry;
        private readonly IToolRegistry _toolRegistry;

        public ExecutionContext(
            string sessionId,
            string userRequest,
            ILLMFunctionRegistry llmRegistry,
            IToolRegistry toolRegistry)
        {
            SessionId = sessionId;
            UserRequest = userRequest;
            _llmRegistry = llmRegistry;
            _toolRegistry = toolRegistry;
            
            ExecutionHistory = new List<IExecutionStep>();
            SharedData = new Dictionary<string, object>();
            
            // Registry는 기존 시스템과의 호환성을 위해 null로 설정
            // 실제로는 _llmRegistry와 _toolRegistry를 통해 접근
            Registry = null!;
        }

        public string SessionId { get; }
        public string UserRequest { get; }
        public List<IExecutionStep> ExecutionHistory { get; }
        public Dictionary<string, object> SharedData { get; }
        public IRegistry Registry { get; }

        public ILLMFunction GetLLMFunction(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return _llmRegistry.Resolve(name);
        }

        public T GetLLMFunction<T>() where T : class, ILLMFunction
        {
            return _llmRegistry.Resolve<T>();
        }

        public ITool GetTool(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return _toolRegistry.Resolve(name);
        }

        public T GetTool<T>() where T : class, ITool
        {
            return _toolRegistry.Resolve<T>();
        }

        public bool HasLLMFunction(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return _llmRegistry.IsRegistered(name);
        }

        public bool HasTool(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return _toolRegistry.IsRegistered(name);
        }
    }
}