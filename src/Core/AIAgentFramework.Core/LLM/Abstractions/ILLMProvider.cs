namespace AIAgentFramework.Core.LLM.Abstractions;

/// <summary>
/// 단순한 LLM Provider 인터페이스 - 핵심 기능만
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
    /// 텍스트 생성 (핵심 기능)
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="model">모델명 (선택사항)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 텍스트</returns>
    Task<string> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 토큰 수 계산
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="model">모델명 (선택사항)</param>
    /// <returns>토큰 수</returns>
    Task<int> CountTokensAsync(string text, string? model = null);
}