namespace Agent.Abstractions.Core.Memory;

/// <summary>
/// 메모리 컨텍스트 (현재 실행 상황의 요약)
/// </summary>
public sealed record MemoryContext
{
    /// <summary>컨텍스트 ID</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>현재 대화의 요약</summary>
    public string? Summary { get; init; }
    
    /// <summary>중요한 정보들</summary>
    public IReadOnlyList<string> KeyInformation { get; init; } = Array.Empty<string>();
    
    /// <summary>현재 작업 중인 태스크</summary>
    public string? CurrentTask { get; init; }
    
    /// <summary>사용자의 목표/의도</summary>
    public string? UserIntent { get; init; }
    
    /// <summary>컨텍스트 메타데이터</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } 
        = new Dictionary<string, object>();
    
    /// <summary>예상 토큰 수</summary>
    public int EstimatedTokenCount { get; init; }
    
    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// 빈 컨텍스트 생성
    /// </summary>
    /// <returns>빈 메모리 컨텍스트</returns>
    public static MemoryContext Empty()
        => new()
        {
            Summary = null,
            KeyInformation = Array.Empty<string>(),
            CurrentTask = null,
            UserIntent = null,
            EstimatedTokenCount = 0
        };
    
    /// <summary>
    /// 기본 컨텍스트 생성
    /// </summary>
    /// <param name="summary">요약</param>
    /// <param name="currentTask">현재 태스크</param>
    /// <param name="userIntent">사용자 의도</param>
    /// <returns>메모리 컨텍스트</returns>
    public static MemoryContext Create(string? summary = null, string? currentTask = null, string? userIntent = null)
        => new()
        {
            Summary = summary,
            CurrentTask = currentTask,
            UserIntent = userIntent
        };
    
    /// <summary>
    /// 컨텍스트에 핵심 정보 추가
    /// </summary>
    /// <param name="information">추가할 정보</param>
    /// <returns>업데이트된 컨텍스트</returns>
    public MemoryContext WithKeyInformation(string information)
        => this with 
        { 
            KeyInformation = KeyInformation.Append(information).ToArray(),
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    
    /// <summary>
    /// 컨텍스트 요약 업데이트
    /// </summary>
    /// <param name="newSummary">새 요약</param>
    /// <returns>업데이트된 컨텍스트</returns>
    public MemoryContext WithSummary(string newSummary)
        => this with 
        { 
            Summary = newSummary,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
}