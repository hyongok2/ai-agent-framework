using AIAgentFramework.Core.Abstractions;

namespace AIAgentFramework.Core.Models;

/// <summary>
/// 스트리밍 청크 구현체
/// - Orchestrator의 ExecuteStreamAsync에서 반환
/// </summary>
public class StreamChunk : IStreamChunk
{
    public required StreamChunkType Type { get; init; }
    public required string Content { get; init; }
    public required bool IsFinal { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 텍스트 청크 생성
    /// </summary>
    public static StreamChunk Text(string content, bool isFinal = false)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Text,
            Content = content,
            IsFinal = isFinal
        };
    }

    /// <summary>
    /// 상태 청크 생성
    /// </summary>
    public static StreamChunk Status(string status)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Status,
            Content = status,
            IsFinal = false
        };
    }

    /// <summary>
    /// Tool 호출 시작 청크
    /// </summary>
    public static StreamChunk ToolCallStart(string toolName)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.ToolCallStart,
            Content = $"🔧 {toolName} 실행 시작...",
            IsFinal = false
        };
    }

    /// <summary>
    /// Tool 호출 완료 청크
    /// </summary>
    public static StreamChunk ToolCallComplete(string toolName)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.ToolCallComplete,
            Content = $"✅ {toolName} 실행 완료",
            IsFinal = false
        };
    }

    /// <summary>
    /// 오류 청크 생성
    /// </summary>
    public static StreamChunk Error(string errorMessage)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = errorMessage,
            IsFinal = true
        };
    }

    /// <summary>
    /// 완료 청크 생성
    /// </summary>
    public static StreamChunk Complete(string summary = "")
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = summary,
            IsFinal = true
        };
    }
}
