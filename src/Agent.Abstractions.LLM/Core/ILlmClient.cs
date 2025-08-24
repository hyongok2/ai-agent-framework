using Agent.Abstractions.LLM.Models.Completion;
using Agent.Abstractions.LLM.Enums;

namespace Agent.Abstractions.LLM.Core;

/// <summary>
/// LLM 클라이언트 인터페이스
/// </summary>
public interface ILlmClient : IDisposable
{
    /// <summary>
    /// 공급자명
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// 클라이언트 상태
    /// </summary>
    ClientStatus Status { get; }
    
    /// <summary>
    /// 지원하는 모델 목록
    /// </summary>
    IReadOnlyList<string> SupportedModels { get; }
    
    /// <summary>
    /// LLM 요청 처리
    /// </summary>
    /// <param name="request">요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답</returns>
    Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 스트리밍 LLM 요청 처리
    /// </summary>
    /// <param name="request">요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 스트림</returns>
    IAsyncEnumerable<LlmResponse> SendStreamAsync(LlmRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모델 지원 여부 확인
    /// </summary>
    /// <param name="modelId">모델 ID</param>
    /// <returns>지원 여부</returns>
    bool SupportsModel(string modelId);
    
    /// <summary>
    /// 클라이언트 상태 확인
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>상태 정보</returns>
    Task<ClientStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}