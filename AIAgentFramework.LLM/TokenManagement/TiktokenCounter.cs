using AIAgentFramework.LLM.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAgentFramework.LLM.TokenManagement
{
    /// <summary>
    /// Tiktoken 라이브러리 기반 토큰 카운터 구현
    /// </summary>
    public class TiktokenCounter : ITokenCounter, IDisposable
    {
        private readonly ILogger<TiktokenCounter> _logger;
        private readonly ConcurrentDictionary<string, ModelConfiguration> _modelConfigurations;
        private volatile bool _disposed;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="logger">로거</param>
        public TiktokenCounter(ILogger<TiktokenCounter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelConfigurations = new ConcurrentDictionary<string, ModelConfiguration>();
            InitializeModelConfigurations();
        }

        /// <inheritdoc />
        public IReadOnlyList<string> SupportedModels => _modelConfigurations.Keys.ToList();

        /// <inheritdoc />
        public int CountTokens(string text, string model)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            ArgumentException.ThrowIfNullOrWhiteSpace(model);

            if (!IsModelSupported(model))
            {
                throw new ArgumentException($"지원되지 않는 모델: {model}", nameof(model));
            }

            try
            {
                var config = _modelConfigurations[model];
                var tokens = EstimateTokensFromText(text, config);
                
                _logger.LogDebug("토큰 카운팅 완료: 모델={Model}, 텍스트길이={Length}, 토큰수={TokenCount}", 
                    model, text.Length, tokens);
                
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 카운팅 실패: 모델={Model}", model);
                throw new InvalidOperationException($"토큰 카운팅 실패: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public Task<int> CountTokensAsync(string text, string model)
        {
            // 현재는 동기식으로 처리하지만, 필요시 비동기 처리로 확장 가능
            var result = CountTokens(text, model);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public TokenUsageEstimate EstimateUsage(LLMTokenRequest request)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var inputTokens = CalculateInputTokens(request);
                var estimatedOutputTokens = Math.Min(request.MaxTokens, GetMaxTokens(request.Model) / 4); // 보수적 추정
                var contextUsageRatio = (double)(inputTokens + estimatedOutputTokens) / GetContextWindowSize(request.Model);
                var estimatedCost = CalculateEstimatedCost(inputTokens, estimatedOutputTokens, request.Model);

                var estimate = new TokenUsageEstimate
                {
                    InputTokens = inputTokens,
                    EstimatedOutputTokens = estimatedOutputTokens,
                    EstimatedCost = estimatedCost,
                    ContextUsageRatio = contextUsageRatio,
                    Model = request.Model
                };

                _logger.LogDebug("토큰 사용량 추정 완료: 입력={InputTokens}, 출력={OutputTokens}, 비용={Cost:C}, 모델={Model}",
                    inputTokens, estimatedOutputTokens, estimatedCost, request.Model);

                return estimate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "토큰 사용량 추정 실패: 모델={Model}", request.Model);
                throw new InvalidOperationException($"토큰 사용량 추정 실패: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public bool IsModelSupported(string model)
        {
            ThrowIfDisposed();
            return !string.IsNullOrWhiteSpace(model) && _modelConfigurations.ContainsKey(model);
        }

        /// <inheritdoc />
        public int GetMaxTokens(string model)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(model);

            if (!IsModelSupported(model))
            {
                throw new ArgumentException($"지원되지 않는 모델: {model}", nameof(model));
            }

            return _modelConfigurations[model].MaxTokens;
        }

        /// <inheritdoc />
        public int GetContextWindowSize(string model)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(model);

            if (!IsModelSupported(model))
            {
                throw new ArgumentException($"지원되지 않는 모델: {model}", nameof(model));
            }

            return _modelConfigurations[model].ContextWindowSize;
        }

        /// <summary>
        /// 모델 설정을 초기화합니다
        /// </summary>
        private void InitializeModelConfigurations()
        {
            // Claude 모델 설정
            _modelConfigurations["claude-3-5-sonnet-20241022"] = new ModelConfiguration
            {
                ModelName = "claude-3-5-sonnet-20241022",
                ContextWindowSize = 200000,
                MaxTokens = 8192,
                InputCostPer1K = 0.003m,
                OutputCostPer1K = 0.015m,
                TokenRatio = 4.0 // 영어 기준 대략적인 문자 대 토큰 비율
            };

            _modelConfigurations["claude-3-5-haiku-20241022"] = new ModelConfiguration
            {
                ModelName = "claude-3-5-haiku-20241022",
                ContextWindowSize = 200000,
                MaxTokens = 8192,
                InputCostPer1K = 0.00025m,
                OutputCostPer1K = 0.00125m,
                TokenRatio = 4.0
            };

            _modelConfigurations["claude-3-opus-20240229"] = new ModelConfiguration
            {
                ModelName = "claude-3-opus-20240229",
                ContextWindowSize = 200000,
                MaxTokens = 4096,
                InputCostPer1K = 0.015m,
                OutputCostPer1K = 0.075m,
                TokenRatio = 4.0
            };

            _modelConfigurations["claude-3-sonnet-20240229"] = new ModelConfiguration
            {
                ModelName = "claude-3-sonnet-20240229",
                ContextWindowSize = 200000,
                MaxTokens = 4096,
                InputCostPer1K = 0.003m,
                OutputCostPer1K = 0.015m,
                TokenRatio = 4.0
            };

            _modelConfigurations["claude-3-haiku-20240307"] = new ModelConfiguration
            {
                ModelName = "claude-3-haiku-20240307",
                ContextWindowSize = 200000,
                MaxTokens = 4096,
                InputCostPer1K = 0.00025m,
                OutputCostPer1K = 0.00125m,
                TokenRatio = 4.0
            };

            _logger.LogInformation("토큰 카운터 초기화 완료: {ModelCount}개 모델 지원", _modelConfigurations.Count);
        }

        /// <summary>
        /// 입력 토큰 수를 계산합니다
        /// </summary>
        /// <param name="request">LLM 요청</param>
        /// <returns>입력 토큰 수</returns>
        private int CalculateInputTokens(LLMTokenRequest request)
        {
            var totalTokens = 0;

            // 시스템 메시지 토큰
            if (!string.IsNullOrWhiteSpace(request.SystemMessage))
            {
                totalTokens += CountTokens(request.SystemMessage, request.Model);
            }

            // 프롬프트 토큰
            totalTokens += CountTokens(request.Prompt, request.Model);

            // 대화 히스토리 토큰
            if (request.ConversationHistory != null)
            {
                foreach (var message in request.ConversationHistory)
                {
                    totalTokens += CountTokens(message.Content, request.Model);
                    // 메시지 구조 오버헤드 (role, timestamp 등)
                    totalTokens += 10; // 대략적인 오버헤드
                }
            }

            return totalTokens;
        }

        /// <summary>
        /// 텍스트에서 토큰 수를 추정합니다
        /// </summary>
        /// <param name="text">텍스트</param>
        /// <param name="config">모델 설정</param>
        /// <returns>추정 토큰 수</returns>
        private int EstimateTokensFromText(string text, ModelConfiguration config)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // 실제 tiktoken 라이브러리가 없으므로, 보수적인 추정 방식 사용
            // 향후 실제 tiktoken 라이브러리 통합 시 이 부분 교체 필요

            // 한글과 영어를 구분하여 계산
            var koreanChars = text.Count(c => c >= '가' && c <= '힣');
            var englishChars = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
            var otherChars = text.Length - koreanChars - englishChars;

            // 한글: 1글자 ≈ 1.5토큰, 영어: 4글자 ≈ 1토큰
            var estimatedTokens = (int)Math.Ceiling(
                (koreanChars * 1.5) + 
                (englishChars / config.TokenRatio) + 
                (otherChars / config.TokenRatio)
            );

            // 최소 1토큰 보장
            return Math.Max(estimatedTokens, 1);
        }

        /// <summary>
        /// 예상 비용을 계산합니다
        /// </summary>
        /// <param name="inputTokens">입력 토큰 수</param>
        /// <param name="outputTokens">출력 토큰 수</param>
        /// <param name="model">모델명</param>
        /// <returns>예상 비용 (USD)</returns>
        private decimal CalculateEstimatedCost(int inputTokens, int outputTokens, string model)
        {
            var config = _modelConfigurations[model];
            
            var inputCost = (inputTokens / 1000m) * config.InputCostPer1K;
            var outputCost = (outputTokens / 1000m) * config.OutputCostPer1K;
            
            return inputCost + outputCost;
        }

        /// <summary>
        /// 리소스를 해제합니다
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _modelConfigurations.Clear();
                _disposed = true;
                _logger.LogDebug("TiktokenCounter 리소스 해제 완료");
            }
        }

        /// <summary>
        /// 해제 여부를 확인합니다
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TiktokenCounter));
        }

        /// <summary>
        /// 모델 설정 정보
        /// </summary>
        private sealed record ModelConfiguration
        {
            public required string ModelName { get; init; }
            public required int ContextWindowSize { get; init; }
            public required int MaxTokens { get; init; }
            public required decimal InputCostPer1K { get; init; }
            public required decimal OutputCostPer1K { get; init; }
            public required double TokenRatio { get; init; }
        }
    }
}