namespace Agent.Abstractions.LLM.Registry;

/// <summary>
/// LLM 모델 기능 정보
/// </summary>
public sealed record LlmCapabilities
{
    /// <summary>
    /// 함수 호출 지원 여부
    /// </summary>
    public bool SupportsFunctionCalling { get; init; }
    
    /// <summary>
    /// 스트리밍 지원 여부
    /// </summary>
    public bool SupportsStreaming { get; init; }
    
    /// <summary>
    /// JSON 모드 지원 여부
    /// </summary>
    public bool SupportsJsonMode { get; init; }
    
    /// <summary>
    /// 멀티모달 지원 여부 (이미지 입력)
    /// </summary>
    public bool SupportsMultimodal { get; init; }
    
    /// <summary>
    /// 비전 지원 여부
    /// </summary>
    public bool SupportsVision { get; init; }
    
    /// <summary>
    /// 시드 지원 여부 (재현 가능한 생성)
    /// </summary>
    public bool SupportsSeed { get; init; }
    
    /// <summary>
    /// 로그 확률 지원 여부
    /// </summary>
    public bool SupportsLogProbs { get; init; }
    
    /// <summary>
    /// 병렬 함수 호출 지원 여부
    /// </summary>
    public bool SupportsParallelFunctionCalling { get; init; }
    
    /// <summary>
    /// 시스템 메시지 지원 여부
    /// </summary>
    public bool SupportsSystemMessage { get; init; } = true;
    
    /// <summary>
    /// 사용자 정의 정지 시퀀스 지원 여부
    /// </summary>
    public bool SupportsStopSequences { get; init; }
    
    /// <summary>
    /// 지원하는 최대 이미지 수 (멀티모달인 경우)
    /// </summary>
    public int? MaxImages { get; init; }
    
    /// <summary>
    /// 지원하는 최대 이미지 크기 (바이트)
    /// </summary>
    public long? MaxImageSize { get; init; }
    
    /// <summary>
    /// 지원하는 이미지 포맷
    /// </summary>
    public IReadOnlyList<string> SupportedImageFormats { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 추가 기능
    /// </summary>
    public IDictionary<string, object> AdditionalCapabilities { get; init; } = new Dictionary<string, object>();
}