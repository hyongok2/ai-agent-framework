using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIAgentFramework.LLM.Interfaces
{
    /// <summary>
    /// 토큰 카운팅 인터페이스
    /// </summary>
    public interface ITokenCounter
    {
        /// <summary>
        /// 지원하는 모델 목록
        /// </summary>
        IReadOnlyList<string> SupportedModels { get; }

        /// <summary>
        /// 텍스트의 토큰 수를 계산합니다
        /// </summary>
        /// <param name="text">계산할 텍스트</param>
        /// <param name="model">모델명</param>
        /// <returns>토큰 수</returns>
        int CountTokens(string text, string model);

        /// <summary>
        /// 텍스트의 토큰 수를 비동기적으로 계산합니다
        /// </summary>
        /// <param name="text">계산할 텍스트</param>
        /// <param name="model">모델명</param>
        /// <returns>토큰 수</returns>
        Task<int> CountTokensAsync(string text, string model);

        /// <summary>
        /// 토큰 사용량을 추정합니다
        /// </summary>
        /// <param name="request">LLM 요청</param>
        /// <returns>토큰 사용량 추정치</returns>
        TokenUsageEstimate EstimateUsage(LLMTokenRequest request);

        /// <summary>
        /// 모델이 지원되는지 확인합니다
        /// </summary>
        /// <param name="model">모델명</param>
        /// <returns>지원 여부</returns>
        bool IsModelSupported(string model);

        /// <summary>
        /// 모델의 최대 토큰 수를 가져옵니다
        /// </summary>
        /// <param name="model">모델명</param>
        /// <returns>최대 토큰 수</returns>
        int GetMaxTokens(string model);

        /// <summary>
        /// 모델의 컨텍스트 윈도우 크기를 가져옵니다
        /// </summary>
        /// <param name="model">모델명</param>
        /// <returns>컨텍스트 윈도우 크기</returns>
        int GetContextWindowSize(string model);
    }

    /// <summary>
    /// LLM 토큰 요청 정보
    /// </summary>
    public sealed record LLMTokenRequest
    {
        /// <summary>
        /// 프롬프트 텍스트
        /// </summary>
        public required string Prompt { get; init; }

        /// <summary>
        /// 시스템 메시지
        /// </summary>
        public string? SystemMessage { get; init; }

        /// <summary>
        /// 모델명
        /// </summary>
        public required string Model { get; init; }

        /// <summary>
        /// 최대 생성 토큰 수
        /// </summary>
        public int MaxTokens { get; init; } = 4000;

        /// <summary>
        /// 대화 히스토리
        /// </summary>
        public IReadOnlyList<ChatMessage>? ConversationHistory { get; init; }
    }

    /// <summary>
    /// 채팅 메시지
    /// </summary>
    public sealed record ChatMessage
    {
        /// <summary>
        /// 메시지 역할 (user, assistant, system)
        /// </summary>
        public required string Role { get; init; }

        /// <summary>
        /// 메시지 내용
        /// </summary>
        public required string Content { get; init; }

        /// <summary>
        /// 메시지 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 토큰 사용량 추정치
    /// </summary>
    public sealed record TokenUsageEstimate
    {
        /// <summary>
        /// 입력(프롬프트) 토큰 수
        /// </summary>
        public int InputTokens { get; init; }

        /// <summary>
        /// 예상 출력(완성) 토큰 수
        /// </summary>
        public int EstimatedOutputTokens { get; init; }

        /// <summary>
        /// 총 토큰 수
        /// </summary>
        public int TotalTokens => InputTokens + EstimatedOutputTokens;

        /// <summary>
        /// 예상 비용 (USD)
        /// </summary>
        public decimal EstimatedCost { get; init; }

        /// <summary>
        /// 컨텍스트 윈도우 사용률 (0.0 ~ 1.0)
        /// </summary>
        public double ContextUsageRatio { get; init; }

        /// <summary>
        /// 모델명
        /// </summary>
        public required string Model { get; init; }

        /// <summary>
        /// 계산 시간
        /// </summary>
        public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
    }
}