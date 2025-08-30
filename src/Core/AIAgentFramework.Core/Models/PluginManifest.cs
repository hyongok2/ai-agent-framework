namespace AIAgentFramework.Core.Models;

/// <summary>
/// 플러그인 매니페스트 모델
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// 플러그인 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 플러그인 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 플러그인 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 플러그인 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// 플러그인 작성자
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// 플러그인 작성자 이메일
    /// </summary>
    public string? AuthorEmail { get; set; }
    
    /// <summary>
    /// 플러그인 홈페이지 URL
    /// </summary>
    public string? Homepage { get; set; }
    
    /// <summary>
    /// 플러그인 라이선스
    /// </summary>
    public string? License { get; set; }
    
    /// <summary>
    /// 어셈블리 파일 경로
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 메인 클래스 타입명
    /// </summary>
    public string MainClass { get; set; } = string.Empty;
    
    /// <summary>
    /// 의존성 목록
    /// </summary>
    public List<PluginDependency> Dependencies { get; set; } = new();
    
    /// <summary>
    /// 플러그인 설정 스키마
    /// </summary>
    public string? ConfigurationSchema { get; set; }
    
    /// <summary>
    /// 기본 설정값
    /// </summary>
    public Dictionary<string, object> DefaultConfiguration { get; set; } = new();
    
    /// <summary>
    /// 플러그인 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// 최소 프레임워크 버전
    /// </summary>
    public string? MinFrameworkVersion { get; set; }
    
    /// <summary>
    /// 최대 프레임워크 버전
    /// </summary>
    public string? MaxFrameworkVersion { get; set; }
    
    /// <summary>
    /// 플러그인 생성 일시
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 플러그인 업데이트 일시
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 플러그인 의존성 모델
/// </summary>
public class PluginDependency
{
    /// <summary>
    /// 의존성 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 의존성 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// 의존성 타입 (NuGet, Assembly, Plugin 등)
    /// </summary>
    public string Type { get; set; } = "NuGet";
    
    /// <summary>
    /// 필수 여부
    /// </summary>
    public bool Required { get; set; } = true;
    
    /// <summary>
    /// 의존성 설명
    /// </summary>
    public string? Description { get; set; }
}