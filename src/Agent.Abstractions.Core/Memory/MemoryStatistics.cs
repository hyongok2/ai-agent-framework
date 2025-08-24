namespace Agent.Abstractions.Core.Memory;

/// <summary>
/// 메모리 사용 통계
/// </summary>
public sealed record MemoryStatistics
{
    /// <summary>총 대화 항목 수</summary>
    public int TotalConversationEntries { get; init; }
    
    /// <summary>총 임시 데이터 항목 수</summary>
    public int TotalTemporaryEntries { get; init; }
    
    /// <summary>예상 총 토큰 수</summary>
    public int EstimatedTotalTokens { get; init; }
    
    /// <summary>메모리 사용량 (바이트)</summary>
    public long MemoryUsageBytes { get; init; }
    
    /// <summary>가장 오래된 항목 생성 시간</summary>
    public DateTimeOffset? OldestEntryCreatedAt { get; init; }
    
    /// <summary>가장 최근 항목 생성 시간</summary>
    public DateTimeOffset? NewestEntryCreatedAt { get; init; }
    
    /// <summary>만료된 항목 수</summary>
    public int ExpiredEntries { get; init; }
    
    /// <summary>통계 생성 시간</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 빈 통계 생성
    /// </summary>
    /// <returns>빈 메모리 통계</returns>
    public static MemoryStatistics Empty()
        => new()
        {
            TotalConversationEntries = 0,
            TotalTemporaryEntries = 0,
            EstimatedTotalTokens = 0,
            MemoryUsageBytes = 0,
            ExpiredEntries = 0
        };
}