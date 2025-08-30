using System;
using System.Threading.Tasks;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Orchestration.Engines
{
    /// <summary>
    /// 상태 지속성을 지원하는 오케스트레이션 엔진
    /// </summary>
    public class StatefulOrchestrationEngine : IOrchestrationEngine
    {
        private readonly IOrchestrationEngine _baseEngine;
        private readonly IStateProvider _stateProvider;
        private readonly ILogger<StatefulOrchestrationEngine> _logger;
        
        private const string STATE_KEY_PREFIX = "orchestration:";
        private static readonly TimeSpan DefaultStateExpiry = TimeSpan.FromHours(24);
        
        public StatefulOrchestrationEngine(
            IOrchestrationEngine baseEngine,
            IStateProvider stateProvider,
            ILogger<StatefulOrchestrationEngine> logger)
        {
            _baseEngine = baseEngine ?? throw new ArgumentNullException(nameof(baseEngine));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// 사용자 요청을 실행하고 상태를 저장합니다
        /// </summary>
        public async Task<IOrchestrationResult> ExecuteAsync(IUserRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            
            // SessionId를 RequestId로 사용 (또는 별도 메타데이터에서 가져옴)
            var sessionId = GetSessionIdFromRequest(request);
            var stateKey = GetStateKey(sessionId);
            
            try
            {
                // 1. 기존 상태 복원 시도
                var existingContext = await RestoreContextAsync(stateKey);
                
                // 2. 베이스 엔진으로 실행
                var result = await _baseEngine.ExecuteAsync(request);
                
                // 3. 실행 결과의 컨텍스트를 상태로 저장 (있을 경우)
                await SaveResultContextAsync(stateKey, result);
                
                _logger.LogInformation("오케스트레이션 완료 및 상태 저장: {SessionId}", sessionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 저장 오케스트레이션 실행 실패: {SessionId}", sessionId);
                
                // 실패 시에도 최소한의 상태 저장 (복구용)
                try
                {
                    await SaveFailureStateAsync(stateKey, ex.Message);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "실패 상태 저장 중 오류: {SessionId}", sessionId);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// 기존 컨텍스트를 계속 실행합니다
        /// </summary>
        public async Task<IOrchestrationResult> ContinueAsync(IOrchestrationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            
            var stateKey = GetStateKey(context.SessionId);
            
            try
            {
                // 베이스 엔진으로 계속 실행
                var result = await _baseEngine.ContinueAsync(context);
                
                // 실행 결과의 컨텍스트를 상태로 저장
                await SaveResultContextAsync(stateKey, result);
                
                _logger.LogInformation("오케스트레이션 계속 실행 완료 및 상태 저장: {SessionId}", context.SessionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 저장 오케스트레이션 계속 실행 실패: {SessionId}", context.SessionId);
                
                // 실패 시에도 최소한의 상태 저장 (복구용)
                try
                {
                    await SaveFailureStateAsync(stateKey, ex.Message);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "실패 상태 저장 중 오류: {SessionId}", context.SessionId);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// 세션 상태를 삭제합니다
        /// </summary>
        public async Task ClearSessionStateAsync(string sessionId)
        {
            ArgumentException.ThrowIfNullOrEmpty(sessionId);
            
            var stateKey = GetStateKey(sessionId);
            
            try
            {
                await _stateProvider.DeleteAsync(stateKey);
                _logger.LogInformation("세션 상태 삭제 완료: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 상태 삭제 실패: {SessionId}", sessionId);
                throw;
            }
        }
        
        /// <summary>
        /// 세션 상태가 존재하는지 확인합니다
        /// </summary>
        public async Task<bool> HasSessionStateAsync(string sessionId)
        {
            ArgumentException.ThrowIfNullOrEmpty(sessionId);
            
            var stateKey = GetStateKey(sessionId);
            
            try
            {
                return await _stateProvider.ExistsAsync(stateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 상태 존재 확인 실패: {SessionId}", sessionId);
                return false;
            }
        }
        
        /// <summary>
        /// 상태 저장소의 건강 상태를 확인합니다
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                return await _stateProvider.IsHealthyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 저장소 건강 상태 확인 실패");
                return false;
            }
        }
        
        /// <summary>
        /// 상태 저장소 통계를 조회합니다
        /// </summary>
        public async Task<StateProviderStatistics> GetStateStatisticsAsync()
        {
            try
            {
                return await _stateProvider.GetStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 저장소 통계 조회 실패");
                throw;
            }
        }
        
        /// <summary>
        /// 만료된 세션 상태들을 정리합니다
        /// </summary>
        public async Task<int> CleanupExpiredStatesAsync()
        {
            try
            {
                var cleanedCount = await _stateProvider.CleanupExpiredStatesAsync();
                _logger.LogInformation("만료된 상태 정리 완료: {Count}개", cleanedCount);
                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "만료된 상태 정리 실패");
                throw;
            }
        }
        
        /// <summary>
        /// 요청에서 세션 ID를 추출합니다
        /// </summary>
        private static string GetSessionIdFromRequest(IUserRequest request)
        {
            // 메타데이터에서 SessionId를 찾거나 RequestId를 사용
            if (request.Metadata.TryGetValue("SessionId", out var sessionId) && sessionId is string sessionIdStr)
            {
                return sessionIdStr;
            }
            
            return request.RequestId; // RequestId를 SessionId로 사용
        }
        
        /// <summary>
        /// 저장된 상태를 복원합니다 (간단한 구현)
        /// </summary>
        private async Task<SessionState?> RestoreContextAsync(string stateKey)
        {
            try
            {
                var state = await _stateProvider.GetAsync<SessionState>(stateKey);
                
                if (state != null)
                {
                    _logger.LogDebug("상태 복원 성공: {StateKey}, 마지막 업데이트: {LastUpdated}", 
                        stateKey, state.LastUpdated);
                        
                    return state;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "상태 복원 실패: {StateKey}", stateKey);
                return null;
            }
        }
        
        /// <summary>
        /// 오케스트레이션 결과를 상태로 저장합니다
        /// </summary>
        private async Task SaveResultContextAsync(string stateKey, IOrchestrationResult result)
        {
            try
            {
                var sessionState = new SessionState
                {
                    SessionId = result.SessionId,
                    IsCompleted = result.IsCompleted,
                    IsSuccess = result.IsSuccess,
                    FinalResponse = result.FinalResponse,
                    ErrorMessage = result.ErrorMessage,
                    TotalDuration = result.TotalDuration,
                    Metadata = result.Metadata,
                    LastUpdated = DateTime.UtcNow
                };
                
                await _stateProvider.SetAsync(stateKey, sessionState, DefaultStateExpiry);
                _logger.LogDebug("상태 저장 성공: {StateKey}", stateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "상태 저장 실패: {StateKey}", stateKey);
                throw;
            }
        }
        
        /// <summary>
        /// 실패 상태를 저장합니다
        /// </summary>
        private async Task SaveFailureStateAsync(string stateKey, string errorMessage)
        {
            try
            {
                var failureState = new SessionState
                {
                    SessionId = ExtractSessionIdFromStateKey(stateKey),
                    IsCompleted = false,
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    LastUpdated = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["failure_timestamp"] = DateTime.UtcNow,
                        ["failure_reason"] = "orchestration_execution_failed"
                    }
                };
                
                await _stateProvider.SetAsync(stateKey, failureState, TimeSpan.FromHours(1));
                _logger.LogDebug("실패 상태 저장 성공: {StateKey}", stateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "실패 상태 저장 실패: {StateKey}", stateKey);
                // 이 시점에서 예외를 다시 던지지 않음 (원래 예외를 우선)
            }
        }
        
        /// <summary>
        /// 세션 ID로부터 상태 키를 생성합니다
        /// </summary>
        private static string GetStateKey(string sessionId)
        {
            return $"{STATE_KEY_PREFIX}{sessionId}";
        }
        
        /// <summary>
        /// 상태 키에서 세션 ID를 추출합니다
        /// </summary>
        private static string ExtractSessionIdFromStateKey(string stateKey)
        {
            return stateKey.StartsWith(STATE_KEY_PREFIX) 
                ? stateKey.Substring(STATE_KEY_PREFIX.Length) 
                : stateKey;
        }
    }
    
    /// <summary>
    /// 세션 상태 데이터 클래스
    /// </summary>
    public class SessionState
    {
        public string SessionId { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsSuccess { get; set; }
        public string? FinalResponse { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}