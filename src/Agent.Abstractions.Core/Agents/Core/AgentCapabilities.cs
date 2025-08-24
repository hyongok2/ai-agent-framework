using System;

namespace Agent.Abstractions.Core.Agents;

/// <summary>
/// 에이전트 기능
/// </summary>
public sealed record AgentCapabilities
{
    /// <summary>스트리밍 지원</summary>
    public bool SupportsStreaming { get; init; } = true;
    
    /// <summary>배치 실행 지원</summary>
    public bool SupportsBatchExecution { get; init; } = true;
    
    /// <summary>대화 관리 지원</summary>
    public bool SupportsConversation { get; init; } = true;
    
    /// <summary>도구 사용 지원</summary>
    public bool SupportsTools { get; init; } = true;
    
    /// <summary>멀티모달 지원 (이미지, 오디오 등)</summary>
    public bool SupportsMultimodal { get; init; }
    
    /// <summary>병렬 실행 지원</summary>
    public bool SupportsParallelExecution { get; init; } = true;
    
    /// <summary>최대 동시 실행 수</summary>
    public int MaxConcurrentExecutions { get; init; } = 10;
    
    /// <summary>최대 대화 길이</summary>
    public int MaxConversationLength { get; init; } = 100;
    
    /// <summary>지원하는 언어</summary>
    public IReadOnlyList<string> SupportedLanguages { get; init; } = new[] { "en", "ko", "ja", "zh" };
    
    /// <summary>지원하는 응답 형식</summary>
    public IReadOnlyList<ResponseFormat> SupportedFormats { get; init; } = 
        new[] { ResponseFormat.Text, ResponseFormat.Json, ResponseFormat.Markdown };
}
