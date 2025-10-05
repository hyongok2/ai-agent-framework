namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 기능 인터페이스
/// 각 역할별 LLM 실행 담당
/// </summary>
public interface ILLMFunction
{
    /// <summary>
    /// LLM 역할
    /// </summary>
    LLMRole Role { get; }

    /// <summary>
    /// 역할 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// LLM 실행
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<ILLMResult> ExecuteAsync(
        ILLMContext context,
        CancellationToken cancellationToken = default);
}
