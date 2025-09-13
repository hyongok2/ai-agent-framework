using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAgentFramework.LLM.Services;

/// <summary>
/// 프롬프트 템플릿 관리 서비스
/// </summary>
public class PromptTemplateService
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _templateCache = new();
    private readonly string _templatesBasePath;

    /// <summary>
    /// 변수 치환 패턴 (예: {UserRequest})
    /// </summary>
    private static readonly Regex VariablePattern = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    public PromptTemplateService(ILogger logger, string? templatesBasePath = null)
    {
        _logger = logger;

        // 기본 템플릿 경로 설정 (어셈블리 위치 기반)
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
        _templatesBasePath = templatesBasePath ??
                            Path.Combine(assemblyDirectory, "..", "..", "..", "..", "Templates");

        _logger.LogInformation("PromptTemplateService initialized with base path: {BasePath}", _templatesBasePath);
    }

    /// <summary>
    /// 템플릿 로드 (캐싱 지원)
    /// </summary>
    /// <param name="templateName">템플릿 이름 (예: "Functions/generator")</param>
    /// <returns>템플릿 내용</returns>
    public async Task<string> LoadTemplateAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

        // 캐시 확인
        var cacheKey = templateName.ToLowerInvariant();
        if (_templateCache.TryGetValue(cacheKey, out var cachedTemplate))
        {
            _logger.LogDebug("Template loaded from cache: {TemplateName}", templateName);
            return cachedTemplate;
        }

        // 파일에서 로드
        var templatePath = Path.Combine(_templatesBasePath, $"{templateName}.txt");

        if (!File.Exists(templatePath))
        {
            var errorMessage = $"Template file not found: {templatePath}";
            _logger.LogError(errorMessage);
            throw new FileNotFoundException(errorMessage, templatePath);
        }

        try
        {
            var templateContent = await File.ReadAllTextAsync(templatePath);

            // 캐시에 저장
            _templateCache[cacheKey] = templateContent;

            _logger.LogInformation("Template loaded successfully: {TemplateName}", templateName);
            return templateContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load template: {TemplateName}", templateName);
            throw;
        }
    }

    /// <summary>
    /// 템플릿 처리 (변수 치환)
    /// </summary>
    /// <param name="template">템플릿 내용</param>
    /// <param name="variables">치환할 변수 딕셔너리</param>
    /// <returns>처리된 프롬프트</returns>
    public string ProcessTemplate(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
            throw new ArgumentException("Template cannot be null or empty", nameof(template));

        if (variables == null)
            variables = new Dictionary<string, string>();

        var result = VariablePattern.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;

            if (variables.TryGetValue(variableName, out var value))
            {
                return value ?? string.Empty;
            }

            // 변수가 없으면 경고 로그하고 빈 문자열로 치환
            _logger.LogWarning("Template variable not found: {VariableName}", variableName);
            return $"[{variableName} NOT FOUND]";
        });

        _logger.LogDebug("Template processed successfully. Original length: {OriginalLength}, Result length: {ResultLength}",
            template.Length, result.Length);

        return result;
    }

    /// <summary>
    /// 템플릿 로드 및 처리 (원스톱 메서드)
    /// </summary>
    /// <param name="templateName">템플릿 이름</param>
    /// <param name="variables">치환할 변수</param>
    /// <returns>처리된 프롬프트</returns>
    public async Task<string> LoadAndProcessTemplateAsync(string templateName, Dictionary<string, string> variables)
    {
        var template = await LoadTemplateAsync(templateName);
        return ProcessTemplate(template, variables);
    }

    /// <summary>
    /// 캐시 클리어
    /// </summary>
    public void ClearCache()
    {
        _templateCache.Clear();
        _logger.LogInformation("Template cache cleared");
    }

    /// <summary>
    /// 사용 가능한 템플릿 목록 조회
    /// </summary>
    /// <returns>템플릿 목록</returns>
    public async Task<List<string>> GetAvailableTemplatesAsync()
    {
        var templates = new List<string>();

        if (!Directory.Exists(_templatesBasePath))
        {
            _logger.LogWarning("Templates directory not found: {BasePath}", _templatesBasePath);
            return templates;
        }

        try
        {
            var templateFiles = Directory.GetFiles(_templatesBasePath, "*.txt", SearchOption.AllDirectories);

            foreach (var filePath in templateFiles)
            {
                var relativePath = Path.GetRelativePath(_templatesBasePath, filePath);
                var templateName = Path.ChangeExtension(relativePath, null);
                templates.Add(templateName.Replace('\\', '/'));
            }

            _logger.LogInformation("Found {TemplateCount} templates", templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available templates");
        }

        await Task.CompletedTask; // 비동기 메서드 유지를 위한 placeholder
        return templates;
    }

    /// <summary>
    /// 템플릿에서 사용된 변수 목록 추출
    /// </summary>
    /// <param name="template">템플릿 내용</param>
    /// <returns>변수 이름 목록</returns>
    public List<string> ExtractVariables(string template)
    {
        if (string.IsNullOrEmpty(template))
            return new List<string>();

        var matches = VariablePattern.Matches(template);
        var variables = new HashSet<string>();

        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value);
        }

        return variables.ToList();
    }
}