namespace AIAgentFramework.Core.LLM.Abstractions;

/// <summary>
/// LLM Provider 인터페이스
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Provider 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 지원하는 모델 목록
    /// </summary>
    IReadOnlyList<string> SupportedModels { get; }
    
    /// <summary>
    /// 기본 모델
    /// </summary>
    string DefaultModel { get; }
    
    /// <summary>
    /// Provider가 사용 가능한지 확인
    /// </summary>
    /// <returns>사용 가능 여부</returns>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// 텍스트 생성
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 텍스트</returns>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 텍스트 생성 (모델 지정)
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="model">모델명</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 텍스트</returns>
    Task<string> GenerateAsync(string prompt, string model, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 구조화된 응답 생성
    /// </summary>
    /// <typeparam name="T">응답 타입</typeparam>
    /// <param name="prompt">프롬프트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>구조화된 응답</returns>
    Task<T?> GenerateStructuredAsync<T>(string prompt, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// 구조화된 응답 생성 (모델 지정)
    /// </summary>
    /// <typeparam name="T">응답 타입</typeparam>
    /// <param name="prompt">프롬프트</param>
    /// <param name="model">모델명</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>구조화된 응답</returns>
    Task<T?> GenerateStructuredAsync<T>(string prompt, string model, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// 스트리밍 텍스트 생성
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>텍스트 스트림</returns>
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 토큰 수 계산
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="model">모델명 (선택사항)</param>
    /// <returns>토큰 수</returns>
    Task<int> CountTokensAsync(string text, string? model = null);
}