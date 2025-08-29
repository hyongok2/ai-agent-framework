namespace AIAgentFramework.Configuration.Models;

/// <summary>
/// UI 설정 모델
/// </summary>
public class UIConfiguration
{
    /// <summary>
    /// 활성화된 인터페이스 목록
    /// </summary>
    public List<string> EnabledInterfaces { get; set; } = new() { "web", "console", "api" };
    
    /// <summary>
    /// Web 인터페이스 설정
    /// </summary>
    public WebInterfaceConfiguration Web { get; set; } = new();
    
    /// <summary>
    /// Console 인터페이스 설정
    /// </summary>
    public ConsoleInterfaceConfiguration Console { get; set; } = new();
    
    /// <summary>
    /// API 인터페이스 설정
    /// </summary>
    public APIInterfaceConfiguration API { get; set; } = new();
    
    /// <summary>
    /// 공통 UI 설정
    /// </summary>
    public CommonUIConfiguration Common { get; set; } = new();
}

/// <summary>
/// Web 인터페이스 설정
/// </summary>
public class WebInterfaceConfiguration
{
    /// <summary>
    /// 포트 번호
    /// </summary>
    public int Port { get; set; } = 5000;
    
    /// <summary>
    /// HTTPS 사용 여부
    /// </summary>
    public bool UseHttps { get; set; } = false;
    
    /// <summary>
    /// CORS 허용 도메인
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
    
    /// <summary>
    /// 정적 파일 경로
    /// </summary>
    public string StaticFilesPath { get; set; } = "./wwwroot";
    
    /// <summary>
    /// 세션 타임아웃 (분)
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;
}

/// <summary>
/// Console 인터페이스 설정
/// </summary>
public class ConsoleInterfaceConfiguration
{
    /// <summary>
    /// 대화형 모드 활성화
    /// </summary>
    public bool InteractiveMode { get; set; } = true;
    
    /// <summary>
    /// 배치 모드 활성화
    /// </summary>
    public bool BatchMode { get; set; } = true;
    
    /// <summary>
    /// 로그 레벨
    /// </summary>
    public string LogLevel { get; set; } = "Information";
    
    /// <summary>
    /// 출력 형식 (json, text, table)
    /// </summary>
    public string OutputFormat { get; set; } = "text";
    
    /// <summary>
    /// 색상 출력 사용 여부
    /// </summary>
    public bool UseColors { get; set; } = true;
}

/// <summary>
/// API 인터페이스 설정
/// </summary>
public class APIInterfaceConfiguration
{
    /// <summary>
    /// API 버전
    /// </summary>
    public string Version { get; set; } = "v1";
    
    /// <summary>
    /// 인증 활성화 여부
    /// </summary>
    public bool AuthenticationEnabled { get; set; } = false;
    
    /// <summary>
    /// API 키 헤더 이름
    /// </summary>
    public string ApiKeyHeader { get; set; } = "X-API-Key";
    
    /// <summary>
    /// 요청 제한 (분당)
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;
    
    /// <summary>
    /// Swagger 문서 활성화
    /// </summary>
    public bool SwaggerEnabled { get; set; } = true;
    
    /// <summary>
    /// 응답 압축 사용 여부
    /// </summary>
    public bool CompressionEnabled { get; set; } = true;
}

/// <summary>
/// 공통 UI 설정
/// </summary>
public class CommonUIConfiguration
{
    /// <summary>
    /// 기본 언어
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";
    
    /// <summary>
    /// 지원 언어 목록
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new() { "en", "ko" };
    
    /// <summary>
    /// 테마
    /// </summary>
    public string Theme { get; set; } = "default";
    
    /// <summary>
    /// 디버그 모드
    /// </summary>
    public bool DebugMode { get; set; } = false;
}