namespace AIAgentFramework.Core.LLM.Abstractions;

/// <summary>
/// LLM 기능 인터페이스
/// </summary>
public interface ILLMFunction
{
    /// <summary>
    /// 기능 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 역할
    /// </summary>
    string Role { get; }
    
    /// <summary>
    /// 설명
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// LLM 기능 실행
    /// </summary>
    /// <param name="context">LLM 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>LLM 결과</returns>
    Task<ILLMResult> ExecuteAsync(ILLMContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 입력 검증
    /// </summary>
    /// <param name="context">LLM 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    Task<bool> ValidateAsync(ILLMContext context, CancellationToken cancellationToken = default);
}