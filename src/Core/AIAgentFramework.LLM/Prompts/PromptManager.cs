using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AIAgentFramework.LLM.Prompts;

/// <summary>
/// 프롬프트 관리자 구현
/// </summary>
public class PromptManager : IPromptManager
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromptManager> _logger;
    private readonly string _promptsDirectory;
    private readonly TimeSpan _cacheTtl;
    private readonly Regex _parameterRegex;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="cache">메모리 캐시</param>
    /// <param name="configuration">설정</param>
    /// <param name="logger">로거</param>
    public PromptManager(IMemoryCache cache, IConfiguration configuration, ILogger<PromptManager> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _promptsDirectory = _configuration["Prompts:Directory"] ?? "prompts";
        _cacheTtl = TimeSpan.FromMinutes(_configuration.GetValue("Prompts:CacheTTLMinutes", 30));
        _parameterRegex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

        EnsurePromptsDirectoryExists();
    }

    /// <inheritdoc />
    public async Task<string> LoadPromptAsync(string promptName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptName))
            throw new ArgumentException("Prompt name cannot be null or empty", nameof(promptName));

        try
        {
            var template = await LoadPromptTemplateAsync(promptName, cancellationToken);
            return ProcessTemplate(template, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prompt: {PromptName}", promptName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> LoadPromptTemplateAsync(string promptName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptName))
            throw new ArgumentException("Prompt name cannot be null or empty", nameof(promptName));

        var cacheKey = $"prompt_template_{promptName}";

        // 캐시에서 확인
        if (_cache.TryGetValue(cacheKey, out string? cachedTemplate) && cachedTemplate != null)
        {
            _logger.LogDebug("Loaded prompt template from cache: {PromptName}", promptName);
            return cachedTemplate;
        }

        try
        {
            var promptPath = GetPromptFilePath(promptName);
            
            if (!File.Exists(promptPath))
            {
                throw new FileNotFoundException($"Prompt file not found: {promptPath}");
            }

            var template = await File.ReadAllTextAsync(promptPath, cancellationToken);
            
            // 캐시에 저장
            _cache.Set(cacheKey, template, _cacheTtl);
            
            _logger.LogDebug("Loaded prompt template from file: {PromptName}", promptName);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prompt template: {PromptName}", promptName);
            throw;
        }
    }

    /// <inheritdoc />
    public string ProcessTemplate(string template, Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        if (parameters == null || parameters.Count == 0)
            return template;

        try
        {
            var result = _parameterRegex.Replace(template, match =>
            {
                var parameterName = match.Groups[1].Value;
                
                if (parameters.TryGetValue(parameterName, out var value))
                {
                    return value?.ToString() ?? string.Empty;
                }

                // 파라미터가 없으면 원본 유지 (선택적 파라미터)
                _logger.LogWarning("Parameter not found in template: {ParameterName}", parameterName);
                return match.Value;
            });

            _logger.LogDebug("Processed template with {ParameterCount} parameters", parameters.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process template");
            throw new InvalidOperationException("Failed to process prompt template", ex);
        }
    }

    /// <inheritdoc />
    public void InvalidateCache(string? promptName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(promptName))
            {
                // 전체 캐시 무효화는 구현이 복잡하므로 로그만 남김
                _logger.LogInformation("Full prompt cache invalidation requested");
                // 실제로는 IMemoryCache에서 특정 패턴의 키만 제거하는 것이 어려움
                // 필요시 별도의 캐시 키 추적 메커니즘 구현 필요
            }
            else
            {
                var cacheKey = $"prompt_template_{promptName}";
                _cache.Remove(cacheKey);
                _logger.LogDebug("Invalidated cache for prompt: {PromptName}", promptName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache for prompt: {PromptName}", promptName);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetAvailablePromptsAsync()
    {
        try
        {
            if (!Directory.Exists(_promptsDirectory))
            {
                _logger.LogWarning("Prompts directory does not exist: {Directory}", _promptsDirectory);
                return Task.FromResult<IReadOnlyList<string>>(new List<string>().AsReadOnly());
            }

            var promptFiles = Directory.GetFiles(_promptsDirectory, "*.md", SearchOption.AllDirectories);
            var promptNames = promptFiles
                .Select(file => Path.GetFileNameWithoutExtension(file))
                .Where(name => !string.IsNullOrEmpty(name))
                .OrderBy(name => name)
                .ToList();

            _logger.LogDebug("Found {Count} prompt files", promptNames.Count);
            return Task.FromResult<IReadOnlyList<string>>(promptNames.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available prompts");
            return Task.FromResult<IReadOnlyList<string>>(new List<string>().AsReadOnly());
        }
    }

    /// <inheritdoc />
    public Task<bool> PromptExistsAsync(string promptName)
    {
        if (string.IsNullOrWhiteSpace(promptName))
            return Task.FromResult(false);

        try
        {
            var promptPath = GetPromptFilePath(promptName);
            return Task.FromResult(File.Exists(promptPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check prompt existence: {PromptName}", promptName);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 프롬프트 파일 경로 생성
    /// </summary>
    /// <param name="promptName">프롬프트 이름</param>
    /// <returns>파일 경로</returns>
    private string GetPromptFilePath(string promptName)
    {
        // 보안을 위해 파일명 검증
        if (promptName.Contains("..") || promptName.Contains("/") || promptName.Contains("\\"))
        {
            throw new ArgumentException("Invalid prompt name", nameof(promptName));
        }

        return Path.Combine(_promptsDirectory, $"{promptName}.md");
    }

    /// <summary>
    /// 프롬프트 디렉토리 존재 확인 및 생성
    /// </summary>
    private void EnsurePromptsDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_promptsDirectory))
            {
                Directory.CreateDirectory(_promptsDirectory);
                _logger.LogInformation("Created prompts directory: {Directory}", _promptsDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompts directory: {Directory}", _promptsDirectory);
        }
    }
}

/// <summary>
/// 프롬프트 관리자 확장 메서드
/// </summary>
public static class PromptManagerExtensions
{
    /// <summary>
    /// 역할별 프롬프트 로드
    /// </summary>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="role">역할 (planner, interpreter 등)</param>
    /// <param name="parameters">치환 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 프롬프트</returns>
    public static async Task<string> LoadRolePromptAsync(
        this IPromptManager promptManager, 
        string role, 
        Dictionary<string, object>? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        return await promptManager.LoadPromptAsync(role, parameters, cancellationToken);
    }

    /// <summary>
    /// 시스템 메시지와 사용자 메시지를 결합한 프롬프트 생성
    /// </summary>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="role">역할</param>
    /// <param name="userMessage">사용자 메시지</param>
    /// <param name="additionalParameters">추가 파라미터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>결합된 프롬프트</returns>
    public static async Task<string> CreateCombinedPromptAsync(
        this IPromptManager promptManager,
        string role,
        string userMessage,
        Dictionary<string, object>? additionalParameters = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = additionalParameters ?? new Dictionary<string, object>();
        parameters["user_message"] = userMessage;
        parameters["user_request"] = userMessage; // 호환성을 위해 두 키 모두 설정

        return await promptManager.LoadRolePromptAsync(role, parameters, cancellationToken);
    }

    /// <summary>
    /// 프롬프트 템플릿에서 필요한 파라미터 목록 추출
    /// </summary>
    /// <param name="promptManager">프롬프트 관리자</param>
    /// <param name="promptName">프롬프트 이름</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>필요한 파라미터 목록</returns>
    public static async Task<IReadOnlyList<string>> GetRequiredParametersAsync(
        this IPromptManager promptManager,
        string promptName,
        CancellationToken cancellationToken = default)
    {
        var template = await promptManager.LoadPromptTemplateAsync(promptName, cancellationToken);
        var regex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);
        var matches = regex.Matches(template);
        
        var parameters = matches
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return parameters.AsReadOnly();
    }
}