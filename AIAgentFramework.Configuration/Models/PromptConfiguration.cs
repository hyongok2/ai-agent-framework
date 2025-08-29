namespace AIAgentFramework.Configuration.Models;

/// <summary>
/// 프롬프트 설정 모델
/// </summary>
public class PromptConfiguration
{
    /// <summary>
    /// 프롬프트 템플릿 디렉토리
    /// </summary>
    public string TemplateDirectory { get; set; } = "./prompts";
    
    /// <summary>
    /// 캐시 TTL (분)
    /// </summary>
    public int CacheTTLMinutes { get; set; } = 60;
    
    /// <summary>
    /// 캐시 활성화 여부
    /// </summary>
    public bool CacheEnabled { get; set; } = true;
    
    /// <summary>
    /// 프롬프트 파일 확장자
    /// </summary>
    public string FileExtension { get; set; } = ".md";
    
    /// <summary>
    /// 기본 시스템 정보
    /// </summary>
    public SystemInformation DefaultSystemInfo { get; set; } = new();
    
    /// <summary>
    /// 역할별 프롬프트 설정
    /// </summary>
    public Dictionary<string, RolePromptConfiguration> RoleConfigurations { get; set; } = new();
    
    /// <summary>
    /// 공통 치환 변수
    /// </summary>
    public Dictionary<string, string> CommonVariables { get; set; } = new();
}

/// <summary>
/// 시스템 정보
/// </summary>
public class SystemInformation
{
    /// <summary>
    /// 시스템 이름
    /// </summary>
    public string SystemName { get; set; } = "AI Agent Framework";
    
    /// <summary>
    /// 시스템 버전
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// 현재 시간 포맷
    /// </summary>
    public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss UTC";
    
    /// <summary>
    /// 시간대
    /// </summary>
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// 역할별 프롬프트 설정
/// </summary>
public class RolePromptConfiguration
{
    /// <summary>
    /// 프롬프트 파일명 (확장자 제외)
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// 캐시 TTL 오버라이드 (분)
    /// </summary>
    public int? CacheTTLMinutes { get; set; }
    
    /// <summary>
    /// 역할별 특화 변수
    /// </summary>
    public Dictionary<string, string> SpecificVariables { get; set; } = new();
    
    /// <summary>
    /// 필수 변수 목록
    /// </summary>
    public List<string> RequiredVariables { get; set; } = new();
    
    /// <summary>
    /// 응답 형식 검증 활성화
    /// </summary>
    public bool ValidateResponseFormat { get; set; } = true;
    
    /// <summary>
    /// 예상 응답 스키마 (JSON Schema)
    /// </summary>
    public string? ExpectedResponseSchema { get; set; }
}