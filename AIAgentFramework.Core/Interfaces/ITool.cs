namespace AIAgentFramework.Core.Interfaces;

/// <summary>
/// 도구 인터페이스
/// </summary>
public interface ITool
{
    /// <summary>
    /// 도구 이름
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 설명
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 카테고리
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// 도구 계약 (입출력 스키마)
    /// </summary>
    IToolContract Contract { get; }
    
    /// <summary>
    /// 도구 실행
    /// </summary>
    /// <param name="input">입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<IToolResult> ExecuteAsync(IToolInput input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 입력 검증
    /// </summary>
    /// <param name="input">입력</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 결과</returns>
    Task<bool> ValidateAsync(IToolInput input, CancellationToken cancellationToken = default);
}