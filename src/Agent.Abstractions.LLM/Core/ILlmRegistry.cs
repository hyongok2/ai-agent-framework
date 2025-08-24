using Agent.Abstractions.LLM.Registry;

namespace Agent.Abstractions.LLM.Core;

/// <summary>
/// LLM 레지스트리 인터페이스
/// </summary>
public interface ILlmRegistry
{
    /// <summary>
    /// LLM 클라이언트 등록
    /// </summary>
    /// <param name="client">LLM 클라이언트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RegisterAsync(ILlmClient client, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// LLM 클라이언트 등록 해제
    /// </summary>
    /// <param name="providerName">공급자명</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>제거되었으면 true</returns>
    Task<bool> UnregisterAsync(string providerName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// LLM 클라이언트 조회
    /// </summary>
    /// <param name="providerName">공급자명</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>LLM 클라이언트, 없으면 null</returns>
    Task<ILlmClient?> GetAsync(string providerName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 기본 LLM 클라이언트 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>기본 LLM 클라이언트, 없으면 null</returns>
    Task<ILlmClient?> GetDefaultAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모델을 지원하는 클라이언트 조회
    /// </summary>
    /// <param name="modelId">모델 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>해당 모델을 지원하는 클라이언트, 없으면 null</returns>
    Task<ILlmClient?> GetByModelAsync(string modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 등록된 공급자 목록 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>공급자 목록</returns>
    Task<IReadOnlyList<LlmProvider>> GetProvidersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 사용 가능한 모든 모델 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>모델 목록</returns>
    Task<IReadOnlyList<LlmModel>> GetAllModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 조건에 맞는 클라이언트 검색
    /// </summary>
    /// <param name="criteria">검색 조건</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>매칭되는 클라이언트 목록</returns>
    Task<IReadOnlyList<ILlmClient>> SearchAsync(LlmSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 기본 공급자 설정
    /// </summary>
    /// <param name="providerName">공급자명</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task SetDefaultAsync(string providerName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 클라이언트 상태 확인
    /// </summary>
    /// <param name="providerName">공급자명</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>상태 정보</returns>
    Task<LlmClientStatus> GetStatusAsync(string providerName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 모든 클라이언트 상태 확인
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>상태 정보 목록</returns>
    Task<IReadOnlyList<LlmClientStatus>> GetAllStatusAsync(CancellationToken cancellationToken = default);
}

