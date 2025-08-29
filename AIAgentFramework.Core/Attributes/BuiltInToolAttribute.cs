namespace AIAgentFramework.Core.Attributes;

/// <summary>
/// Built-In 도구를 표시하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class BuiltInToolAttribute : Attribute
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
    /// 도구 버전
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// 작성자
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// 카테고리
    /// </summary>
    public string Category { get; set; } = "Tool";
    
    /// <summary>
    /// 태그 (쉼표로 구분)
    /// </summary>
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// 입력 스키마 (JSON Schema)
    /// </summary>
    public string InputSchema { get; set; } = string.Empty;
    
    /// <summary>
    /// 출력 스키마 (JSON Schema)
    /// </summary>
    public string OutputSchema { get; set; } = string.Empty;
    
    /// <summary>
    /// 의존성 (쉼표로 구분)
    /// </summary>
    public string Dependencies { get; set; } = string.Empty;
    
    /// <summary>
    /// 필요한 권한 (쉼표로 구분)
    /// </summary>
    public string RequiredPermissions { get; set; } = string.Empty;
    
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
    
    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 실행 우선순위 (높을수록 우선)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 생성자
    /// </summary>
    public BuiltInToolAttribute()
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    public BuiltInToolAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="description">도구 설명</param>
    public BuiltInToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 이름</param>
    /// <param name="description">도구 설명</param>
    /// <param name="category">카테고리</param>
    public BuiltInToolAttribute(string name, string description, string category)
    {
        Name = name;
        Description = description;
        Category = category;
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