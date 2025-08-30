namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 프롬프트 관리자 인터페이스
/// </summary>
public interface IPromptManager
{
    /// <summary>
    /// 프롬프트 로드
    /// </summary>
    /// <param name="promptName">프롬프트 이름</param>
    /// <param name="parameters">치환 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 프롬프트</returns>
    Task<string> LoadPromptAsync(string promptName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 프롬프트 템플릿 로드 (치환 전)
    /// </summary>
    /// <param name="promptName">프롬프트 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>프롬프트 템플릿</returns>
    Task<string> LoadPromptTemplateAsync(string promptName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 프롬프트 템플릿 치환
    /// </summary>
    /// <param name="template">프롬프트 템플릿</param>
    /// <param name="parameters">치환 파라미터</param>
    /// <returns>치환된 프롬프트</returns>
    string ProcessTemplate(string template, Dictionary<string, object>? parameters = null);
    
    /// <summary>
    /// 프롬프트 캐시 무효화
    /// </summary>
    /// <param name="promptName">프롬프트 이름 (null이면 전체 캐시 무효화)</param>
    void InvalidateCache(string? promptName = null);
    
    /// <summary>
    /// 사용 가능한 프롬프트 목록
    /// </summary>
    /// <returns>프롬프트 이름 목록</returns>
    Task<IReadOnlyList<string>> GetAvailablePromptsAsync();
    
    /// <summary>
    /// 프롬프트 존재 여부 확인
    /// </summary>
    /// <param name="promptName">프롬프트 이름</param>
    /// <returns>존재 여부</returns>
    Task<bool> PromptExistsAsync(string promptName);
}