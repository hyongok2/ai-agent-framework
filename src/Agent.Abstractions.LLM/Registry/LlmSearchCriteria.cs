using Agent.Abstractions.LLM.Enums;

namespace Agent.Abstractions.LLM.Registry;

/// <summary>
/// LLM 검색 조건
/// </summary>
public sealed record LlmSearchCriteria
{
    /// <summary>
    /// 공급자명 패턴
    /// </summary>
    public string? ProviderPattern { get; init; }
    
    /// <summary>
    /// 모델 ID 패턴
    /// </summary>
    public string? ModelPattern { get; init; }
    
    /// <summary>
    /// 모델 타입
    /// </summary>
    public ModelType? ModelType { get; init; }
    
    /// <summary>
    /// 필요한 기능
    /// </summary>
    public LlmCapabilityRequirements? CapabilityRequirements { get; init; }
    
    /// <summary>
    /// 최소 성능 요구사항
    /// </summary>
    public LlmPerformanceRequirements? PerformanceRequirements { get; init; }
    
    /// <summary>
    /// 최대 비용 (토큰당 USD)
    /// </summary>
    public decimal? MaxCostPerToken { get; init; }
    
    /// <summary>
    /// 사용 가능한 클라이언트만
    /// </summary>
    public bool OnlyAvailable { get; init; } = true;
    
    /// <summary>
    /// 결과 제한
    /// </summary>
    public int? Limit { get; init; }
    
    /// <summary>
    /// 정렬 기준
    /// </summary>
    public LlmSortBy SortBy { get; init; } = LlmSortBy.Provider;
    
    /// <summary>
    /// 정렬 방향
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}