namespace AIAgentFramework.Core.Tools.Attributes;

/// <summary>
/// 플러그인 도구를 표시하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class PluginToolAttribute : Attribute
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
    /// 플러그인 이름
    /// </summary>
    public string PluginName { get; set; } = string.Empty;
    
    /// <summary>
    /// 플러그인 버전
    /// </summary>
    public string PluginVersion { get; set; } = "1.0.0";
    
    /// <summary>
    /// 카테고리
    /// </summary>
    public string Category { get; set; } = "Plugin";
    
    /// <summary>
    /// 입력 스키마 (JSON Schema)
    /// </summary>
    public string? InputSchema { get; set; }
    
    /// <summary>
    /// 출력 스키마 (JSON Schema)
    /// </summary>
    public string? OutputSchema { get; set; }
    
    /// <summary>
    /// 의존성 목록 (쉼표로 구분)
    /// </summary>
    public string Dependencies { get; set; } = string.Empty;
    
    /// <summary>
    /// 필요한 권한 목록 (쉼표로 구분)
    /// </summary>
    public string RequiredPermissions { get; set; } = string.Empty;
    
    /// <summary>
    /// 실행 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// 캐시 가능 여부
    /// </summary>
    public bool IsCacheable { get; set; } = true;
    
    /// <summary>
    /// 캐시 TTL (분)
    /// </summary>
    public int CacheTTLMinutes { get; set; } = 60;
    
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
    /// 최소 프레임워크 버전
    /// </summary>
    public string MinFrameworkVersion { get; set; } = "1.0.0";
    
    /// <summary>
    /// 라이선스
    /// </summary>
    public string License { get; set; } = string.Empty;
    
    /// <summary>
    /// 홈페이지 URL
    /// </summary>
    public string? Homepage { get; set; }
    
    /// <summary>
    /// 플러그인 ID
    /// </summary>
    public string PluginId { get; set; } = string.Empty;
    
    /// <summary>
    /// 패키지 이름
    /// </summary>
    public string PackageName { get; set; } = string.Empty;
    
    /// <summary>
    /// 리포지토리 URL
    /// </summary>
    public string Repository { get; set; } = string.Empty;
    
    /// <summary>
    /// 최대 프레임워크 버전
    /// </summary>
    public string MaxFrameworkVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// 샌드박스 필요 여부
    /// </summary>
    public bool RequiresSandbox { get; set; } = false;
    
    /// <summary>
    /// 네트워크 접근 필요 여부
    /// </summary>
    public bool RequiresNetworkAccess { get; set; } = false;
    
    /// <summary>
    /// 파일 시스템 접근 필요 여부
    /// </summary>
    public bool RequiresFileSystemAccess { get; set; } = false;

    /// <summary>
    /// 생성자
    /// </summary>
    public PluginToolAttribute()
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="pluginName">플러그인 이름</param>
    public PluginToolAttribute(string name, string pluginName)
    {
        Name = name;
        PluginName = pluginName;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="pluginName">플러그인 이름</param>
    /// <param name="description">도구 설명</param>
    public PluginToolAttribute(string name, string pluginName, string description)
    {
        Name = name;
        PluginName = pluginName;
        Description = description;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="description">도구 설명</param>
    /// <param name="version">버전</param>
    /// <param name="author">작성자</param>
    public PluginToolAttribute(string name, string description, string version, string author)
    {
        Name = name;
        Description = description;
        Version = version;
        Author = author;
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
    /// 의존성 목록을 파싱합니다.
    /// </summary>
    /// <returns>의존성 목록</returns>
    public List<string> GetDependencies()
    {
        return ParseCommaSeparatedString(Dependencies);
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
    /// 프레임워크 버전 호환성 확인
    /// </summary>
    /// <param name="frameworkVersion">프레임워크 버전</param>
    /// <returns>호환 여부</returns>
    public bool IsCompatibleWith(string frameworkVersion)
    {
        // 기본 구현: 버전 비교 로직
        return true;
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