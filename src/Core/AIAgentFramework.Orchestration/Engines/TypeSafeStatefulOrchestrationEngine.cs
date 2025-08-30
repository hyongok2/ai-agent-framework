using System;
using System.Threading;
using System.Threading.Tasks;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Core.Factories;
using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Orchestration.Engines
{
    /// <summary>
    /// 타입 안전성과 상태 지속성을 모두 지원하는 통합 오케스트레이션 엔진
    /// TypeSafeOrchestrationEngine을 기반으로 상태 지속성 기능 추가
    /// </summary>
    public class TypeSafeStatefulOrchestrationEngine : IOrchestrationEngine
    {
        private readonly TypeSafeOrchestrationEngine _baseEngine;
        private readonly IStateProvider _stateProvider;
        private readonly ILogger<TypeSafeStatefulOrchestrationEngine> _logger;
        
        private const string STATE_KEY_PREFIX = "orchestration:";
        private static readonly TimeSpan DefaultStateExpiry = TimeSpan.FromHours(24);
        private static readonly TimeSpan FailedStateExpiry = TimeSpan.FromHours(1);

        public TypeSafeStatefulOrchestrationEngine(
            TypeSafeOrchestrationEngine baseEngine,
            IStateProvider stateProvider,
            ILogger<TypeSafeStatefulOrchestrationEngine> logger)
        {
            _baseEngine = baseEngine ?? throw new ArgumentNullException(nameof(baseEngine));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest userRequest)
        {
            ArgumentNullException.ThrowIfNull(userRequest);
            
            var sessionId = Guid.NewGuid().ToString();
            var stateKey = GetStateKey(sessionId);
            
            _logger.LogInformation("상태 지속성 오케스트레이션 시작: SessionId={SessionId}", sessionId);

            try
            {
                // 1. 상태 복원 시도 (새 세션이므로 복원할 상태 없음)
                var result = await _baseEngine.ExecuteAsync(userRequest);
                
                // 2. 실행 결과 상태 저장
                await SaveResultStateAsync(stateKey, result);
                _logger.LogInformation("오케스트레이션 상태 저장 완료: SessionId={SessionId}", sessionId);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 지속성 오케스트레이션 실행 실패: SessionId={SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            
            var sessionId = context.SessionId;
            var stateKey = GetStateKey(sessionId);
            
            _logger.LogInformation("상태 지속성 오케스트레이션 계속 실행: SessionId={SessionId}", sessionId);

            try
            {
                // 1. 기존 상태 복원
                var savedContext = await RestoreContextAsync(stateKey);
                if (savedContext != null)
                {
                    _logger.LogInformation("기존 상태 복원됨: SessionId={SessionId}", sessionId);
                    context = savedContext;
                }

                // 2. 기본 엔진으로 계속 실행
                var result = await _baseEngine.ContinueAsync(context);
                
                // 3. 업데이트된 상태 저장
                await SaveResultStateAsync(stateKey, result);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 지속성 오케스트레이션 계속 실행 실패: SessionId={SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// 기존 상태를 복원합니다.
        /// </summary>
        private async Task<IOrchestrationContext?> RestoreContextAsync(string stateKey)
        {
            try
            {
                return await _stateProvider.GetAsync<IOrchestrationContext>(stateKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "상태 복원 실패: StateKey={StateKey}", stateKey);
                return null;
            }
        }

        /// <summary>
        /// 실행 결과를 상태로 저장합니다.
        /// </summary>
        private async Task SaveResultStateAsync(string stateKey, IOrchestrationResult result)
        {
            try
            {
                var expiry = result.IsSuccess ? DefaultStateExpiry : FailedStateExpiry;
                
                // 결과 메타데이터를 상태로 저장
                var stateData = new
                {
                    SessionId = result.SessionId,
                    IsSuccess = result.IsSuccess,
                    IsCompleted = result.IsCompleted,
                    FinalResponse = result.FinalResponse,
                    ExecutionSteps = result.ExecutionSteps,
                    TotalDuration = result.TotalDuration,
                    ErrorMessage = result.ErrorMessage,
                    SavedAt = DateTime.UtcNow
                };
                
                await _stateProvider.SetAsync(stateKey, stateData, expiry);
                
                _logger.LogDebug("실행 결과 상태 저장 완료: StateKey={StateKey}, Success={IsSuccess}, Expiry={Expiry}", 
                    stateKey, result.IsSuccess, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "실행 결과 상태 저장 실패: StateKey={StateKey}", stateKey);
                // 상태 저장 실패는 전체 처리에 영향을 주지 않음
            }
        }

        /// <summary>
        /// 세션 ID로부터 상태 키를 생성합니다.
        /// </summary>
        private string GetStateKey(string sessionId) => $"{STATE_KEY_PREFIX}{sessionId}";
    }
}