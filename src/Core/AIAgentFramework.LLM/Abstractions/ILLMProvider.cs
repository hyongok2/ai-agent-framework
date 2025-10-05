namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 제공자 인터페이스
/// 실제 LLM 모델과의 통신 담당
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// 제공자 이름 (GPT, Claude, Local 등)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// 지원하는 모델 목록
    /// </summary>
    IReadOnlyList<string> SupportedModels { get; }

    /// <summary>
    /// LLM 호출
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="modelName">모델 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>LLM 응답</returns>
    Task<string> CallAsync(
        string prompt,
        string modelName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스트리밍 LLM 호출 (향후 지원)
    /// </summary>
    /// <param name="prompt">프롬프트</param>
    /// <param name="modelName">모델 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트리밍 응답</returns>
    IAsyncEnumerable<string> CallStreamAsync(
        string prompt,
        string modelName,
        CancellationToken cancellationToken = default);
}
