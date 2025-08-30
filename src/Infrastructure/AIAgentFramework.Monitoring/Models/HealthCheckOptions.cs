namespace AIAgentFramework.Monitoring.Models;

/// <summary>
/// Health Check 옵션 설정
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// 백그라운드 체크 활성화 여부
    /// </summary>
    public bool EnableBackgroundChecks { get; set; } = false;

    /// <summary>
    /// 백그라운드 체크 간격 (초)
    /// </summary>
    public int BackgroundCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 체크 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 실패 임계값
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
}