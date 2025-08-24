namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 요청 옵션
/// </summary>
public sealed record RequestOptions
{
    /// <summary>
    /// 스트리밍 활성화
    /// </summary>
    public bool EnableStreaming { get; init; } = true;

    /// <summary>
    /// 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMs { get; init; } = 30000;

    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 응답 형식
    /// </summary>
    public ResponseFormat Format { get; init; } = ResponseFormat.Text;

    /// <summary>
    /// 최대 토큰 수
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// 온도 (창의성)
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// 도구 사용 허용
    /// </summary>
    public bool AllowTools { get; init; } = true;

    /// <summary>
    /// 허용된 도구 목록 (null이면 모두 허용)
    /// </summary>
    public List<string>? AllowedTools { get; init; }

    /// <summary>
    /// 메모리 사용
    /// </summary>
    public bool UseMemory { get; init; } = true;

    /// <summary>
    /// 캐싱 활성화
    /// </summary>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// 최대 메시지 길이
    /// </summary>
    public int MaxMessageLength { get; init; } = 10000;

    /// <summary>
    /// 최대 첨부 파일 수
    /// </summary>
    public int MaxAttachments { get; init; } = 10;

    /// <summary>
    /// 최대 첨부 파일 크기 (바이트)
    /// </summary>
    public long MaxAttachmentSize { get; init; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 실행 모드
    /// </summary>
    public ExecutionMode Mode { get; init; } = ExecutionMode.Auto;

    /// <summary>
    /// 디버그 모드
    /// </summary>
    public bool Debug { get; init; }
}