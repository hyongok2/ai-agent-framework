using AIAgentFramework.LLM.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.LLM.TokenManagement
{
    /// <summary>
    /// 토큰 예산 관리 인터페이스
    /// </summary>
    public interface ITokenBudgetManager
    {
        /// <summary>
        /// 요청된 토큰을 사용할 수 있는지 확인합니다
        /// </summary>
        /// <param name="usage">토큰 사용량</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>사용 가능 여부</returns>
        Task<bool> CanUseTokensAsync(TokenUsageEstimate usage, CancellationToken cancellationToken = default);

        /// <summary>
        /// 토큰 사용량을 기록합니다
        /// </summary>
        /// <param name="usage">실제 사용량</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>기록 완료 Task</returns>
        Task RecordUsageAsync(TokenUsageEstimate usage, CancellationToken cancellationToken = default);

        /// <summary>
        /// 일일 사용량을 조회합니다
        /// </summary>
        /// <param name="date">조회 날짜</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>일일 사용량</returns>
        Task<DailyTokenUsage> GetDailyUsageAsync(DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>
        /// 시간당 사용량을 조회합니다
        /// </summary>
        /// <param name="hour">조회 시간 (UTC)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>시간당 사용량</returns>
        Task<HourlyTokenUsage> GetHourlyUsageAsync(DateTime hour, CancellationToken cancellationToken = default);

        /// <summary>
        /// 현재 예산 상태를 조회합니다
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>예산 상태</returns>
        Task<TokenBudgetStatus> GetBudgetStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 예산 한도를 업데이트합니다
        /// </summary>
        /// <param name="limits">새로운 한도</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>업데이트 완료 Task</returns>
        Task UpdateBudgetLimitsAsync(TokenBudgetLimits limits, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 일일 토큰 사용량
    /// </summary>
    public sealed record DailyTokenUsage
    {
        /// <summary>
        /// 날짜
        /// </summary>
        public required DateOnly Date { get; init; }

        /// <summary>
        /// 총 사용 토큰 수
        /// </summary>
        public int TotalTokens { get; init; }

        /// <summary>
        /// 입력 토큰 수
        /// </summary>
        public int InputTokens { get; init; }

        /// <summary>
        /// 출력 토큰 수
        /// </summary>
        public int OutputTokens { get; init; }

        /// <summary>
        /// 총 비용
        /// </summary>
        public decimal TotalCost { get; init; }

        /// <summary>
        /// 요청 횟수
        /// </summary>
        public int RequestCount { get; init; }

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdatedAt { get; init; }
    }

    /// <summary>
    /// 시간당 토큰 사용량
    /// </summary>
    public sealed record HourlyTokenUsage
    {
        /// <summary>
        /// 시간 (UTC)
        /// </summary>
        public required DateTime Hour { get; init; }

        /// <summary>
        /// 총 사용 토큰 수
        /// </summary>
        public int TotalTokens { get; init; }

        /// <summary>
        /// 입력 토큰 수
        /// </summary>
        public int InputTokens { get; init; }

        /// <summary>
        /// 출력 토큰 수
        /// </summary>
        public int OutputTokens { get; init; }

        /// <summary>
        /// 총 비용
        /// </summary>
        public decimal TotalCost { get; init; }

        /// <summary>
        /// 요청 횟수
        /// </summary>
        public int RequestCount { get; init; }
    }

    /// <summary>
    /// 토큰 예산 상태
    /// </summary>
    public sealed record TokenBudgetStatus
    {
        /// <summary>
        /// 일일 사용률 (0.0 ~ 1.0)
        /// </summary>
        public double DailyUsageRatio { get; init; }

        /// <summary>
        /// 시간당 사용률 (0.0 ~ 1.0)
        /// </summary>
        public double HourlyUsageRatio { get; init; }

        /// <summary>
        /// 일일 남은 토큰 수
        /// </summary>
        public int DailyRemainingTokens { get; init; }

        /// <summary>
        /// 시간당 남은 토큰 수
        /// </summary>
        public int HourlyRemainingTokens { get; init; }

        /// <summary>
        /// 일일 남은 예산
        /// </summary>
        public decimal DailyRemainingBudget { get; init; }

        /// <summary>
        /// 시간당 남은 예산
        /// </summary>
        public decimal HourlyRemainingBudget { get; init; }

        /// <summary>
        /// 예산 초과 위험도 (0.0 ~ 1.0)
        /// </summary>
        public double RiskLevel { get; init; }

        /// <summary>
        /// 상태 조회 시간
        /// </summary>
        public DateTime StatusTime { get; init; }
    }

    /// <summary>
    /// 토큰 예산 한도
    /// </summary>
    public sealed record TokenBudgetLimits
    {
        /// <summary>
        /// 일일 토큰 한도
        /// </summary>
        public int DailyTokenLimit { get; init; } = 100_000;

        /// <summary>
        /// 시간당 토큰 한도
        /// </summary>
        public int HourlyTokenLimit { get; init; } = 10_000;

        /// <summary>
        /// 일일 예산 한도 (USD)
        /// </summary>
        public decimal DailyBudgetLimit { get; init; } = 100.0m;

        /// <summary>
        /// 시간당 예산 한도 (USD)
        /// </summary>
        public decimal HourlyBudgetLimit { get; init; } = 10.0m;

        /// <summary>
        /// 경고 임계값 (0.0 ~ 1.0)
        /// </summary>
        public double WarningThreshold { get; init; } = 0.8;

        /// <summary>
        /// 차단 임계값 (0.0 ~ 1.0)
        /// </summary>
        public double BlockThreshold { get; init; } = 0.95;

        /// <summary>
        /// 모델별 우선순위 가중치
        /// </summary>
        public IReadOnlyDictionary<string, double> ModelPriorityWeights { get; init; } = 
            new Dictionary<string, double>();
    }

    /// <summary>
    /// 토큰 예산 초과 예외
    /// </summary>
    public class TokenBudgetExceededException : Exception
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        public TokenBudgetExceededException(string message) : base(message) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="innerException">내부 예외</param>
        public TokenBudgetExceededException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 초과한 사용량
        /// </summary>
        public TokenUsageEstimate? ExceededUsage { get; init; }

        /// <summary>
        /// 현재 예산 상태
        /// </summary>
        public TokenBudgetStatus? BudgetStatus { get; init; }
    }
}