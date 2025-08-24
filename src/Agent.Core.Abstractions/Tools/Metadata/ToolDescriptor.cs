using System;
using System.Text.Json.Nodes;
using Agent.Core.Abstractions.Common.Identifiers;

namespace Agent.Core.Abstractions.Tools.Metadata;

/// <summary>
/// 도구 설명자
/// </summary>
public sealed record ToolDescriptor
{
    /// <summary>
    /// 도구 ID
    /// </summary>
    public required ToolId Id { get; init; }
    
    /// <summary>
    /// 도구 이름 (사용자 친화적)
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 도구 설명
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 입력 스키마
    /// </summary>
    public required JsonNode InputSchema { get; init; }
    
    /// <summary>
    /// 출력 스키마
    /// </summary>
    public required JsonNode OutputSchema { get; init; }
    
    /// <summary>
    /// 도구 카테고리
    /// </summary>
    public string? Category { get; init; }
    
    /// <summary>
    /// 태그
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 지원하는 기능
    /// </summary>
    public ToolCapabilities Capabilities { get; init; } = new();
    
    /// <summary>
    /// 필요한 권한
    /// </summary>
    public IReadOnlyList<string> RequiredPermissions { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 사용 예제
    /// </summary>
    public IReadOnlyList<ToolExample> Examples { get; init; } = Array.Empty<ToolExample>();
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}