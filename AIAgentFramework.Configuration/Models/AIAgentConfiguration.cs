namespace AIAgentFramework.Configuration.Models;

/// <summary>
/// AI 에이전트 프레임워크 메인 설정
/// </summary>
public class AIAgentConfiguration
{
    /// <summary>
    /// 애플리케이션 정보
    /// </summary>
    public ApplicationInfo Application { get; set; } = new();
    
    /// <summary>
    /// LLM 설정
    /// </summary>
    public LLMConfiguration LLM { get; set; } = new();
    
    /// <summary>
    /// 도구 설정
    /// </summary>
    public ToolConfiguration Tools { get; set; } = new();
    
    /// <summary>
    /// 프롬프트 설정
    /// </summary>
    public PromptConfiguration Prompts { get; set; } = new();
    
    /// <summary>
    /// UI 설정
    /// </summary>
    public UIConfiguration UI { get; set; } = new();
    
    /// <summary>
    /// 오케스트레이션 설정
    /// </summary>
    public OrchestrationConfiguration Orchestration { get; set; } = new();
    
    /// <summary>
    /// 로깅 설정
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();
    
    /// <summary>
    /// 보안 설정
    /// </summary>
    public SecurityConfiguration Security { get; set; } = new();
}

/// <summary>
/// 애플리케이션 정보
/// </summary>
public class ApplicationInfo
{
    /// <summary>
    /// 애플리케이션 이름
    /// </summary>
    public string Name { get; set; } = "AI Agent Framework";
    
    /// <summary>
    /// 버전
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// 환경 (Development, Testing, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";
    
    /// <summary>
    /// 인스턴스 ID
    /// </summary>
    public string InstanceId { get; set; } = GetDefaultInstanceId();
    
    private static string GetDefaultInstanceId()
    {
        return System.Environment.MachineName;
    }
}

/// <summary>
/// 오케스트레이션 설정
/// </summary>
public class OrchestrationConfiguration
{
    /// <summary>
    /// 최대 실행 단계 수
    /// </summary>
    public int MaxExecutionSteps { get; set; } = 50;
    
    /// <summary>
    /// 실행 타임아웃 (분)
    /// </summary>
    public int ExecutionTimeoutMinutes { get; set; } = 10;
    
    /// <summary>
    /// 세션 만료 시간 (시간)
    /// </summary>
    public int SessionExpirationHours { get; set; } = 24;
    
    /// <summary>
    /// 병렬 세션 최대 개수
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 100;
    
    /// <summary>
    /// 실행 이력 보관 일수
    /// </summary>
    public int HistoryRetentionDays { get; set; } = 30;
}

/// <summary>
/// 로깅 설정
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// 로그 레벨
    /// </summary>
    public string LogLevel { get; set; } = "Information";
    
    /// <summary>
    /// 로그 파일 경로
    /// </summary>
    public string? LogFilePath { get; set; }
    
    /// <summary>
    /// 구조화된 로깅 사용 여부
    /// </summary>
    public bool StructuredLogging { get; set; } = true;
    
    /// <summary>
    /// 성능 로깅 활성화
    /// </summary>
    public bool PerformanceLogging { get; set; } = true;
    
    /// <summary>
    /// 로그 보관 일수
    /// </summary>
    public int RetentionDays { get; set; } = 30;
}

/// <summary>
/// 보안 설정
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// API 키 암호화 활성화
    /// </summary>
    public bool EncryptApiKeys { get; set; } = true;
    
    /// <summary>
    /// 민감한 데이터 마스킹
    /// </summary>
    public bool MaskSensitiveData { get; set; } = true;
    
    /// <summary>
    /// 감사 로깅 활성화
    /// </summary>
    public bool AuditLogging { get; set; } = true;
    
    /// <summary>
    /// 허용된 IP 주소 목록
    /// </summary>
    public List<string> AllowedIPs { get; set; } = new();
    
    /// <summary>
    /// HTTPS 강제 사용
    /// </summary>
    public bool RequireHttps { get; set; } = false;
}