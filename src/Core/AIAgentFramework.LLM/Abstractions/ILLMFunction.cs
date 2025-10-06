namespace AIAgentFramework.LLM.Abstractions;

/// <summary>
/// LLM 기능 Base 인터페이스 (Registry용 비제네릭)
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
    /// 스트리밍 지원 여부
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// LLM 실행 (비제네릭 - 동적 호출용)
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<ILLMResult> ExecuteAsync(
        ILLMContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// LLM 실행 스트리밍 (비제네릭 - 동적 호출용)
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트리밍 청크</returns>
    IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamAsync(
        ILLMContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// LLM 기능 인터페이스 (타입 안전한 Input 사용)
/// </summary>
/// <typeparam name="TInput">입력 데이터 타입</typeparam>
/// <typeparam name="TOutput">출력 데이터 타입</typeparam>
public interface ILLMFunction<TInput, TOutput> : ILLMFunction
{

    /// <summary>
    /// LLM 실행 (일반 모드)
    /// </summary>
    /// <param name="input">입력 데이터 (타입 안전)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>실행 결과</returns>
    Task<ILLMResult> ExecuteAsync(
        TInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// LLM 실행 (스트리밍 모드)
    /// </summary>
    /// <param name="input">입력 데이터 (타입 안전)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>스트리밍 청크</returns>
    IAsyncEnumerable<ILLMStreamChunk> ExecuteStreamAsync(
        TInput input,
        CancellationToken cancellationToken = default);
}
