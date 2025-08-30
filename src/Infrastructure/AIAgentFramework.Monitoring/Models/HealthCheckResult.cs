using System.Text.Json.Serialization;

namespace AIAgentFramework.Monitoring.Models;

/// <summary>
/// Health Check 결과
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Health Check 이름
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Health Check 상태
    /// </summary>
    [JsonPropertyName("status")]
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;
    
    /// <summary>
    /// 상태 메시지
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 오류 정보 (상태가 Unhealthy인 경우)
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    /// <summary>
    /// 응답 시간 (밀리초)
    /// </summary>
    [JsonPropertyName("response_time_ms")]
    public long ResponseTimeMs { get; set; }
    
    /// <summary>
    /// 추가 데이터
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();
    
    /// <summary>
    /// Health Check 실행 시간
    /// </summary>
    [JsonPropertyName("checked_at")]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 태그
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// 성공적인 결과 생성
    /// </summary>
    /// <param name="name">Health Check 이름</param>
    /// <param name="message">상태 메시지</param>
    /// <param name="responseTimeMs">응답 시간</param>
    /// <returns>성공 결과</returns>
    public static HealthCheckResult Healthy(string name, string message = "Health check passed", long responseTimeMs = 0)
    {
        return new HealthCheckResult
        {
            Name = name,
            Status = HealthStatus.Healthy,
            Message = message,
            ResponseTimeMs = responseTimeMs
        };
    }
    
    /// <summary>
    /// 경고 결과 생성
    /// </summary>
    /// <param name="name">Health Check 이름</param>
    /// <param name="message">경고 메시지</param>
    /// <param name="responseTimeMs">응답 시간</param>
    /// <returns>경고 결과</returns>
    public static HealthCheckResult Warning(string name, string message, long responseTimeMs = 0)
    {
        return new HealthCheckResult
        {
            Name = name,
            Status = HealthStatus.Warning,
            Message = message,
            ResponseTimeMs = responseTimeMs
        };
    }
    
    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    /// <param name="name">Health Check 이름</param>
    /// <param name="message">오류 메시지</param>
    /// <param name="error">오류 상세 정보</param>
    /// <param name="responseTimeMs">응답 시간</param>
    /// <returns>실패 결과</returns>
    public static HealthCheckResult Unhealthy(string name, string message, string? error = null, long responseTimeMs = 0)
    {
        return new HealthCheckResult
        {
            Name = name,
            Status = HealthStatus.Unhealthy,
            Message = message,
            Error = error,
            ResponseTimeMs = responseTimeMs
        };
    }
    
    /// <summary>
    /// 데이터 추가
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    /// <returns>현재 인스턴스</returns>
    public HealthCheckResult WithData(string key, object value)
    {
        Data[key] = value;
        return this;
    }
    
    /// <summary>
    /// 태그 추가
    /// </summary>
    /// <param name="tags">태그 배열</param>
    /// <returns>현재 인스턴스</returns>
    public HealthCheckResult WithTags(params string[] tags)
    {
        Tags.AddRange(tags);
        return this;
    }
}

/// <summary>
/// Health 상태 열거형
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// 알 수 없음
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// 정상
    /// </summary>
    Healthy = 1,
    
    /// <summary>
    /// 경고
    /// </summary>
    Warning = 2,
    
    /// <summary>
    /// 비정상
    /// </summary>
    Unhealthy = 3
}

/// <summary>
/// Health Check 구성
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int RetryCount { get; set; } = 1;
    
    /// <summary>
    /// 재시도 간격 (밀리초)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// 실행 주기 (초, 연속 실행인 경우)
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 경고 임계값 (예: 응답 시간)
    /// </summary>
    public Dictionary<string, object> WarningThresholds { get; set; } = new();
    
    /// <summary>
    /// 크리티컬 임계값
    /// </summary>
    public Dictionary<string, object> CriticalThresholds { get; set; } = new();
}