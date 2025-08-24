namespace Agent.Core.Abstractions.Tools.Execution;

/// <summary>
/// 실행 환경 정보
/// </summary>
public sealed record ExecutionEnvironment
{
    /// <summary>
    /// 환경 타입 (development, staging, production)
    /// </summary>
    public string Type { get; init; } = "development";
    
    /// <summary>
    /// 지역 정보
    /// </summary>
    public string? Region { get; init; }
    
    /// <summary>
    /// 타임존
    /// </summary>
    public string TimeZone { get; init; } = TimeZoneInfo.Local.Id;
    
    /// <summary>
    /// 언어 코드
    /// </summary>
    public string Language { get; init; } = "en";
    
    /// <summary>
    /// 사용자 정의 속성
    /// </summary>
    public IDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}