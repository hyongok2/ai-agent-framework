namespace AIAgentFramework.Configuration.Models;

/// <summary>
/// 도구 설정 모델
/// </summary>
public class ToolConfiguration
{
    /// <summary>
    /// 플러그인 디렉토리 경로
    /// </summary>
    public string PluginDirectory { get; set; } = "./plugins";
    
    /// <summary>
    /// MCP 엔드포인트 설정
    /// </summary>
    public List<MCPEndpointConfiguration> MCPEndpoints { get; set; } = new();
    
    /// <summary>
    /// Built-In 도구 설정
    /// </summary>
    public Dictionary<string, BuiltInToolConfiguration> BuiltInTools { get; set; } = new();
    
    /// <summary>
    /// 도구 실행 타임아웃 (초)
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// 병렬 실행 최대 개수
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 5;
    
    /// <summary>
    /// 캐시 설정
    /// </summary>
    public ToolCacheConfiguration Cache { get; set; } = new();
}

/// <summary>
/// MCP 엔드포인트 설정
/// </summary>
public class MCPEndpointConfiguration
{
    /// <summary>
    /// 엔드포인트 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 엔드포인트 URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// 인증 토큰
    /// </summary>
    public string? AuthToken { get; set; }
    
    /// <summary>
    /// 연결 타임아웃 (초)
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Built-In 도구 설정
/// </summary>
public class BuiltInToolConfiguration
{
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 도구별 설정
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// 도구 캐시 설정
/// </summary>
public class ToolCacheConfiguration
{
    /// <summary>
    /// 캐시 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 기본 TTL (분)
    /// </summary>
    public int DefaultTTLMinutes { get; set; } = 30;
    
    /// <summary>
    /// 최대 캐시 크기 (MB)
    /// </summary>
    public int MaxSizeMB { get; set; } = 100;
    
    /// <summary>
    /// 도구별 TTL 설정
    /// </summary>
    public Dictionary<string, int> ToolSpecificTTL { get; set; } = new();
}