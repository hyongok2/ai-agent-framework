namespace AIAgentFramework.Registry.Models;

/// <summary>
/// 컴포넌트 메타데이터 기본 클래스
/// </summary>
public abstract class ComponentMetadata
{
    /// <summary>
    /// 컴포넌트 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 컴포넌트 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 컴포넌트 버전
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// 컴포넌트 작성자
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// 컴포넌트 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// 컴포넌트 카테고리
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 등록 시간
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 컴포넌트 타입
    /// </summary>
    public Type ComponentType { get; set; } = typeof(object);
    
    /// <summary>
    /// 추가 속성
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// LLM 기능 메타데이터
/// </summary>
public class LLMFunctionMetadata : ComponentMetadata
{
    /// <summary>
    /// LLM 기능 역할
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 지원하는 모델 목록
    /// </summary>
    public List<string> SupportedModels { get; set; } = new();
    
    /// <summary>
    /// 필수 파라미터 목록
    /// </summary>
    public List<string> RequiredParameters { get; set; } = new();
    
    /// <summary>
    /// 선택적 파라미터 목록
    /// </summary>
    public List<string> OptionalParameters { get; set; } = new();
    
    /// <summary>
    /// 예상 응답 스키마
    /// </summary>
    public string? ResponseSchema { get; set; }
    
    /// <summary>
    /// 실행 우선순위
    /// </summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// 도구 메타데이터
/// </summary>
public class ToolMetadata : ComponentMetadata
{
    /// <summary>
    /// 도구 타입 (BuiltIn, Plugin, MCP)
    /// </summary>
    public ToolType ToolType { get; set; } = ToolType.BuiltIn;
    
    /// <summary>
    /// 입력 스키마
    /// </summary>
    public string? InputSchema { get; set; }
    
    /// <summary>
    /// 출력 스키마
    /// </summary>
    public string? OutputSchema { get; set; }
    
    /// <summary>
    /// 의존성 목록
    /// </summary>
    public List<string> Dependencies { get; set; } = new();
    
    /// <summary>
    /// 권한 요구사항
    /// </summary>
    public List<string> RequiredPermissions { get; set; } = new();
    
    /// <summary>
    /// 실행 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 캐시 가능 여부
    /// </summary>
    public bool IsCacheable { get; set; } = false;
    
    /// <summary>
    /// 캐시 TTL (분)
    /// </summary>
    public int CacheTTLMinutes { get; set; } = 30;
}

/// <summary>
/// 도구 타입 열거형
/// </summary>
public enum ToolType
{
    /// <summary>
    /// 내장 도구
    /// </summary>
    BuiltIn,
    
    /// <summary>
    /// 플러그인 도구
    /// </summary>
    Plugin,
    
    /// <summary>
    /// MCP 도구
    /// </summary>
    MCP
}

/// <summary>
/// 컴포넌트 등록 정보
/// </summary>
public class ComponentRegistration
{
    /// <summary>
    /// 등록 ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 메타데이터
    /// </summary>
    public ComponentMetadata Metadata { get; set; } = new LLMFunctionMetadata();
    
    /// <summary>
    /// 컴포넌트 인스턴스
    /// </summary>
    public object Instance { get; set; } = new object();
    
    /// <summary>
    /// 등록 소스 (Assembly, Manual, etc.)
    /// </summary>
    public string RegistrationSource { get; set; } = "Manual";
    
    /// <summary>
    /// 등록 시간
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 마지막 사용 시간
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// 사용 횟수
    /// </summary>
    public long UsageCount { get; set; } = 0;
}