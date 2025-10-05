using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Tools.Abstractions;

/// <summary>
/// Tool 기본 인터페이스
/// Tool = Function + Metadata + Contract
/// </summary>
public interface ITool
{
    /// <summary>
    /// Tool 메타데이터
    /// </summary>
    IToolMetadata Metadata { get; }

    /// <summary>
    /// Tool 계약 (입출력 스키마)
    /// </summary>
    IToolContract Contract { get; }

    /// <summary>
    /// Tool 실행
    /// </summary>
    /// <param name="input">입력 데이터</param>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<IToolResult> ExecuteAsync(
        object? input,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}
