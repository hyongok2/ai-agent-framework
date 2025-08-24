using Agent.Abstractions.Tools.Metadata;

namespace Agent.Abstractions.Tools.Registry;

/// <summary>
/// 도구 검색 조건
/// </summary>
public sealed record ToolSearchCriteria
{
    /// <summary>
    /// 이름 패턴 (와일드카드 지원)
    /// </summary>
    public string? NamePattern { get; init; }
    
    /// <summary>
    /// 설명 키워드
    /// </summary>
    public string? DescriptionKeyword { get; init; }
    
    /// <summary>
    /// 카테고리
    /// </summary>
    public string? Category { get; init; }
    
    /// <summary>
    /// 태그 목록 (모두 포함해야 함)
    /// </summary>
    public IReadOnlyList<string> RequiredTags { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 공급자
    /// </summary>
    public string? Provider { get; init; }
    
    /// <summary>
    /// 네임스페이스
    /// </summary>
    public string? Namespace { get; init; }
    
    /// <summary>
    /// 버전 패턴
    /// </summary>
    public string? VersionPattern { get; init; }
    
    /// <summary>
    /// 필요한 권한
    /// </summary>
    public IReadOnlyList<string> RequiredPermissions { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 기능 요구사항
    /// </summary>
    public ToolCapabilityRequirements? CapabilityRequirements { get; init; }
    
    /// <summary>
    /// 결과 제한
    /// </summary>
    public int? Limit { get; init; }
    
    /// <summary>
    /// 정렬 기준
    /// </summary>
    public ToolSortBy SortBy { get; init; } = ToolSortBy.Name;
    
    /// <summary>
    /// 정렬 방향
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}