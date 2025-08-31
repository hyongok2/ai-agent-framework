namespace AIAgentFramework.Core.LLM.Attributes;

/// <summary>
/// LLM 기능을 표시하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class LLMFunctionAttribute : Attribute
{
    /// <summary>
    /// 기능 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 기능 역할
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 기능 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 카테고리
    /// </summary>
    public string Category { get; set; } = "LLM";
    
    /// <summary>
    /// 우선순위 (높을수록 우선)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// 지원하는 모델 목록 (쉼표로 구분)
    /// </summary>
    public string SupportedModels { get; set; } = string.Empty;
    
    /// <summary>
    /// 필수 파라미터 목록 (쉼표로 구분)
    /// </summary>
    public string RequiredParameters { get; set; } = string.Empty;
    
    /// <summary>
    /// 선택적 파라미터 목록 (쉼표로 구분)
    /// </summary>
    public string OptionalParameters { get; set; } = string.Empty;
    
    /// <summary>
    /// 응답 스키마 (JSON Schema)
    /// </summary>
    public string? ResponseSchema { get; set; }
    
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
    /// 생성자
    /// </summary>
    public LLMFunctionAttribute()
    {
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">기능 이름</param>
    /// <param name="role">기능 역할</param>
    public LLMFunctionAttribute(string name, string role)
    {
        Name = name;
        Role = role;
    }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">기능 이름</param>
    /// <param name="role">기능 역할</param>
    /// <param name="description">기능 설명</param>
    public LLMFunctionAttribute(string name, string role, string description)
    {
        Name = name;
        Role = role;
        Description = description;
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
    /// 지원 모델 목록을 파싱합니다.
    /// </summary>
    /// <returns>지원 모델 목록</returns>
    public List<string> GetSupportedModels()
    {
        return ParseCommaSeparatedString(SupportedModels);
    }

    /// <summary>
    /// 필수 파라미터 목록을 파싱합니다.
    /// </summary>
    /// <returns>필수 파라미터 목록</returns>
    public List<string> GetRequiredParameters()
    {
        return ParseCommaSeparatedString(RequiredParameters);
    }

    /// <summary>
    /// 선택적 파라미터 목록을 파싱합니다.
    /// </summary>
    /// <returns>선택적 파라미터 목록</returns>
    public List<string> GetOptionalParameters()
    {
        return ParseCommaSeparatedString(OptionalParameters);
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