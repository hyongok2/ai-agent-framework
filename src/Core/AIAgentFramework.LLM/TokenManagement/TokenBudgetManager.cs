using AIAgentFramework.LLM.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentFramework.LLM.TokenManagement
{
    /// <summary>
    /// 메모리 기반 토큰 예산 관리자 구현
    /// </summary>
    public class TokenBudgetManager : ITokenBudgetManager, IDisposable
    {
        private readonly ILogger<TokenBudgetManager> _logger;
        private readonly TokenBudgetLimits _limits;
        private readonly ConcurrentDictionary<DateOnly, DailyTokenUsage> _dailyUsage;
        private readonly ConcurrentDictionary<DateTime, HourlyTokenUsage> _hourlyUsage;
        private readonly object _lockObject = new();
        private volatile bool _disposed;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="options">토큰 예산 설정</param>
        /// <param name="logger">로거</param>
        public TokenBudgetManager(IOptions<TokenBudgetLimits> options, ILogger<TokenBudgetManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _limits = options?.Value ?? new TokenBudgetLimits();
            _dailyUsage = new ConcurrentDictionary<DateOnly, DailyTokenUsage>();
            _hourlyUsage = new ConcurrentDictionary<DateTime, HourlyTokenUsage>();

            _logger.LogInformation("토큰 예산 관리자 초기화 완료: 일일한도={DailyLimit}, 시간한도={HourlyLimit}",
                _limits.DailyTokenLimit, _limits.HourlyTokenLimit);
        }

        /// <inheritdoc />
        public async Task<bool> CanUseTokensAsync(TokenUsageEstimate usage, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(usage);

            try
            {
                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);
                var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

                var dailyUsage = await GetDailyUsageAsync(today, cancellationToken);
                var hourlyUsage = await GetHourlyUsageAsync(currentHour, cancellationToken);

                // 일일 한도 확인
                var newDailyTotal = dailyUsage.TotalTokens + usage.TotalTokens;
                if (newDailyTotal > _limits.DailyTokenLimit)
                {
                    _logger.LogWarning("일일 토큰 한도 초과 예상: 현재={Current}, 요청={Requested}, 한도={Limit}",
                        dailyUsage.TotalTokens, usage.TotalTokens, _limits.DailyTokenLimit);
                    return false;
                }

                // 시간당 한도 확인
                var newHourlyTotal = hourlyUsage.TotalTokens + usage.TotalTokens;
                if (newHourlyTotal > _limits.HourlyTokenLimit)
                {
                    _logger.LogWarning("시간당 토큰 한도 초과 예상: 현재={Current}, 요청={Requested}, 한도={Limit}",
                        hourlyUsage.TotalTokens, usage.TotalTokens, _limits.HourlyTokenLimit);
                    return false;
                }

                // 일일 예산 확인
                var newDailyCost = dailyUsage.TotalCost + usage.EstimatedCost;
                if (newDailyCost > _limits.DailyBudgetLimit)
                {
                    _logger.LogWarning("일일 예산 한도 초과 예상: 현재={Current:C}, 요청={Requested:C}, 한도={Limit:C}",
                        dailyUsage.TotalCost, usage.EstimatedCost, _limits.DailyBudgetLimit);
                    return false;
                }

                // 시간당 예산 확인
                var newHourlyCost = hourlyUsage.TotalCost + usage.EstimatedCost;
                if (newHourlyCost > _limits.HourlyBudgetLimit)
                {
                    _logger.LogWarning("시간당 예산 한도 초과 예상: 현재={Current:C}, 요청={Requested:C}, 한도={Limit:C}",
                        hourlyUsage.TotalCost, usage.EstimatedCost, _limits.HourlyBudgetLimit);
                    return false;
                }

                _logger.LogDebug("토큰 사용 승인: 일일={DailyUsage}/{DailyLimit}, 시간={HourlyUsage}/{HourlyLimit}",
                    newDailyTotal, _limits.DailyTokenLimit, newHourlyTotal, _limits.HourlyTokenLimit);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 사용 가능성 확인 중 오류 발생");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RecordUsageAsync(TokenUsageEstimate usage, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(usage);

            try
            {
                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);
                var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

                lock (_lockObject)
                {
                    // 일일 사용량 업데이트
                    _dailyUsage.AddOrUpdate(today,
                        new DailyTokenUsage
                        {
                            Date = today,
                            TotalTokens = usage.TotalTokens,
                            InputTokens = usage.InputTokens,
                            OutputTokens = usage.EstimatedOutputTokens,
                            TotalCost = usage.EstimatedCost,
                            RequestCount = 1,
                            LastUpdatedAt = now
                        },
                        (date, existing) => existing with
                        {
                            TotalTokens = existing.TotalTokens + usage.TotalTokens,
                            InputTokens = existing.InputTokens + usage.InputTokens,
                            OutputTokens = existing.OutputTokens + usage.EstimatedOutputTokens,
                            TotalCost = existing.TotalCost + usage.EstimatedCost,
                            RequestCount = existing.RequestCount + 1,
                            LastUpdatedAt = now
                        });

                    // 시간당 사용량 업데이트
                    _hourlyUsage.AddOrUpdate(currentHour,
                        new HourlyTokenUsage
                        {
                            Hour = currentHour,
                            TotalTokens = usage.TotalTokens,
                            InputTokens = usage.InputTokens,
                            OutputTokens = usage.EstimatedOutputTokens,
                            TotalCost = usage.EstimatedCost,
                            RequestCount = 1
                        },
                        (hour, existing) => existing with
                        {
                            TotalTokens = existing.TotalTokens + usage.TotalTokens,
                            InputTokens = existing.InputTokens + usage.InputTokens,
                            OutputTokens = existing.OutputTokens + usage.EstimatedOutputTokens,
                            TotalCost = existing.TotalCost + usage.EstimatedCost,
                            RequestCount = existing.RequestCount + 1
                        });
                }

                _logger.LogDebug("토큰 사용량 기록 완료: 토큰={Tokens}, 비용={Cost:C}, 모델={Model}",
                    usage.TotalTokens, usage.EstimatedCost, usage.Model);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 사용량 기록 중 오류 발생");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DailyTokenUsage> GetDailyUsageAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var usage = _dailyUsage.GetValueOrDefault(date, new DailyTokenUsage
            {
                Date = date,
                TotalTokens = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalCost = 0,
                RequestCount = 0,
                LastUpdatedAt = DateTime.UtcNow
            });

            return await Task.FromResult(usage);
        }

        /// <inheritdoc />
        public async Task<HourlyTokenUsage> GetHourlyUsageAsync(DateTime hour, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var normalizedHour = new DateTime(hour.Year, hour.Month, hour.Day, hour.Hour, 0, 0, DateTimeKind.Utc);
            var usage = _hourlyUsage.GetValueOrDefault(normalizedHour, new HourlyTokenUsage
            {
                Hour = normalizedHour,
                TotalTokens = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalCost = 0,
                RequestCount = 0
            });

            return await Task.FromResult(usage);
        }

        /// <inheritdoc />
        public async Task<TokenBudgetStatus> GetBudgetStatusAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);
                var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

                var dailyUsage = await GetDailyUsageAsync(today, cancellationToken);
                var hourlyUsage = await GetHourlyUsageAsync(currentHour, cancellationToken);

                var dailyUsageRatio = (double)dailyUsage.TotalTokens / _limits.DailyTokenLimit;
                var hourlyUsageRatio = (double)hourlyUsage.TotalTokens / _limits.HourlyTokenLimit;

                var dailyRemainingTokens = Math.Max(0, _limits.DailyTokenLimit - dailyUsage.TotalTokens);
                var hourlyRemainingTokens = Math.Max(0, _limits.HourlyTokenLimit - hourlyUsage.TotalTokens);

                var dailyRemainingBudget = Math.Max(0, _limits.DailyBudgetLimit - dailyUsage.TotalCost);
                var hourlyRemainingBudget = Math.Max(0, _limits.HourlyBudgetLimit - hourlyUsage.TotalCost);

                // 위험도 계산: 일일/시간당 사용률 중 높은 값 사용
                var riskLevel = Math.Max(dailyUsageRatio, hourlyUsageRatio);

                var status = new TokenBudgetStatus
                {
                    DailyUsageRatio = dailyUsageRatio,
                    HourlyUsageRatio = hourlyUsageRatio,
                    DailyRemainingTokens = dailyRemainingTokens,
                    HourlyRemainingTokens = hourlyRemainingTokens,
                    DailyRemainingBudget = dailyRemainingBudget,
                    HourlyRemainingBudget = hourlyRemainingBudget,
                    RiskLevel = riskLevel,
                    StatusTime = now
                };

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "예산 상태 조회 중 오류 발생");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateBudgetLimitsAsync(TokenBudgetLimits limits, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(limits);

            // 현재 구현에서는 런타임 업데이트를 지원하지 않음
            // 향후 설정 저장소와 연동하여 구현 예정
            _logger.LogWarning("런타임 예산 한도 업데이트는 현재 지원되지 않습니다");
            
            await Task.CompletedTask;
            throw new NotSupportedException("런타임 예산 한도 업데이트는 현재 지원되지 않습니다");
        }

        /// <summary>
        /// 오래된 사용량 데이터를 정리합니다
        /// </summary>
        /// <param name="retentionDays">보관 일수</param>
        public void CleanupOldUsageData(int retentionDays = 30)
        {
            ThrowIfDisposed();

            try
            {
                var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-retentionDays));
                var cutoffHour = DateTime.UtcNow.AddHours(-retentionDays * 24);

                var removedDailyCount = 0;
                var removedHourlyCount = 0;

                // 일일 사용량 정리
                var dailyKeysToRemove = new List<DateOnly>();
                foreach (var kvp in _dailyUsage)
                {
                    if (kvp.Key < cutoffDate)
                        dailyKeysToRemove.Add(kvp.Key);
                }

                foreach (var key in dailyKeysToRemove)
                {
                    if (_dailyUsage.TryRemove(key, out _))
                        removedDailyCount++;
                }

                // 시간당 사용량 정리
                var hourlyKeysToRemove = new List<DateTime>();
                foreach (var kvp in _hourlyUsage)
                {
                    if (kvp.Key < cutoffHour)
                        hourlyKeysToRemove.Add(kvp.Key);
                }

                foreach (var key in hourlyKeysToRemove)
                {
                    if (_hourlyUsage.TryRemove(key, out _))
                        removedHourlyCount++;
                }

                _logger.LogInformation("사용량 데이터 정리 완료: 일일={DailyRemoved}개, 시간={HourlyRemoved}개",
                    removedDailyCount, removedHourlyCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용량 데이터 정리 중 오류 발생");
            }
        }

        /// <summary>
        /// 리소스를 해제합니다
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _dailyUsage.Clear();
                _hourlyUsage.Clear();
                _disposed = true;
                _logger.LogDebug("TokenBudgetManager 리소스 해제 완료");
            }
        }

        /// <summary>
        /// 해제 여부를 확인합니다
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TokenBudgetManager));
        }
    }
}