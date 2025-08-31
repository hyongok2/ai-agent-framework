namespace AIAgentFramework.Core.Tools.Attributes;

/// <summary>
/// MCP(Model Context Protocol) 도구를 표시하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class MCPToolAttribute : Attribute
{
    /// <summary>
    /// 도구 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 도구 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// MCP 서버 엔드포인트
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// MCP 서버 이름
    /// </summary>
    public string ServerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 카테고리
    /// </summary>
    public string Category { get; set; } = "MCP";
    
    /// <summary>
    /// 입력 스키마 (JSON Schema)
    /// </summary>
    public string? InputSchema { get; set; }
    
    /// <summary>
    /// 출력 스키마 (JSON Schema)
    /// </summary>
    public string? OutputSchema { get; set; }
    
    /// <summary>
    /// 연결 타임아웃 (초)
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;
    
    /// <summary>
    /// 실행 타임아웃 (초)
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// 캐시 가능 여부
    /// </summary>
    public bool IsCacheable { get; set; } = true;
    
    /// <summary>
    /// 캐시 TTL (분)
    /// </summary>
    public int CacheTTLMinutes { get; set; } = 30;
    
    /// <summary>
    /// 인증 토큰 필요 여부
    /// </summary>
    public bool RequiresAuth { get; set; } = false;
    
    /// <summary>
    /// 태그 목록 (쉼표로 구분)
    /// </summary>
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// 작성자
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// 버전
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// MCP 프로토콜 버전
    /// </summary>
    public string MCPVersion { get; set; } = "1.0";
    
    /// <summary>
    /// SSL/TLS 사용 여부
    /// </summary>
    public bool UseSSL { get; set; } = true;
    
    /// <summary>
    /// 필요한 권한 목록 (쉼표로 구분)
    /// </summary>
    public string RequiredPermissions { get; set; } = string.Empty;
    
    /// <summary>
    /// 요청 타임아웃 (초)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 프로토콜 버전
    /// </summary>
    public string ProtocolVersion { get; set; } = "2024-11-05";
    
    /// <summary>
    /// 인증 토큰 환경 변수
    /// </summary>
    public string AuthTokenEnvVar { get; set; } = string.Empty;
    
    /// <summary>
    /// 지원되는 전송 방식 (쉼표로 구분)
    /// </summary>
    public string SupportedTransports { get; set; } = "http,websocket";
    
    /// <summary>
    /// 연결 풀링 사용 여부
    /// </summary>
    public bool UseConnectionPooling { get; set; } = true;
    
    /// <summary>
    /// 최대 동시 연결 수
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 10;
    
    /// <summary>
    /// 하트비트 간격 (초)
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 생성자
    /// </summary>
    public MCPToolAttribute()
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="endpoint">MCP 엔드포인트</param>
    public MCPToolAttribute(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="endpoint">MCP 엔드포인트</param>
    /// <param name="serverName">MCP 서버 이름</param>
    public MCPToolAttribute(string name, string endpoint, string serverName)
    {
        Name = name;
        Endpoint = endpoint;
        ServerName = serverName;
    }

    /// <summary>
    /// 태그 목록을 파싱합니다.
    /// </summary>
    /// <returns>태그 목록</returns>
    public List<string> GetTags()
    {
        return ParseCommaSeparatedString(Tags);
    }
    
    /// <summary>
    /// 필요한 권한 목록을 파싱합니다.
    /// </summary>
    /// <returns>권한 목록</returns>
    public List<string> GetRequiredPermissions()
    {
        return ParseCommaSeparatedString(RequiredPermissions);
    }
    
    /// <summary>
    /// 지원되는 전송 방식 목록을 파싱합니다.
    /// </summary>
    /// <returns>전송 방식 목록</returns>
    public List<string> GetSupportedTransports()
    {
        return ParseCommaSeparatedString(SupportedTransports);
    }

    /// <summary>
    /// 쉼표로 구분된 문자열을 파싱합니다.
    /// </summary>
    /// <param name="input">입력 문자열</param>
    /// <returns>파싱된 문자열 목록</returns>
    private static List<string> ParseCommaSeparatedString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToList();
    }
}