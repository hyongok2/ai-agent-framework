namespace AIAgentFramework.LLM.Models;

/// <summary>
/// LLM Function 실행 옵션
/// </summary>
public class LLMFunctionOptions
{
    /// <summary>
    /// 기본 옵션
    /// </summary>
    public static LLMFunctionOptions Default => new();

    /// <summary>
    /// 사용할 LLM 모델 이름
    /// </summary>
    public string ModelName { get; init; } = "gpt-oss:20b";

    /// <summary>
    /// 스트리밍 지원 여부
    /// </summary>
    public bool EnableStreaming { get; init; } = false;

    /// <summary>
    /// 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMs { get; init; } = 30000;

    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int RetryCount { get; init; } = 0;
}
