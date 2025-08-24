
namespace Agent.Core.Abstractions.Schema;
/// <summary>
/// 스키마 메타데이터
/// </summary>
public sealed record SchemaMetadata
{
    /// <summary>
    /// 스키마 ID
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 스키마 버전
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// 스키마 제목
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 스키마 설명
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 스키마 타입 (예: "object", "array")
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// 등록 시간
    /// </summary>
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 마지막 수정 시간
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; init; }
}