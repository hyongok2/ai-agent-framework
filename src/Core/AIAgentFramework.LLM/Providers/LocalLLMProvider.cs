
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// Local LLM Provider 구현 (Ollama, LM Studio 등)
/// </summary>
public class LocalLLMProvider : LLMProviderBase
{
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="httpClient">HTTP 클라이언트</param>
    /// <param name="logger">로거</param>
    /// <param name="baseUrl">로컬 LLM 서버 URL</param>
    /// <param name="defaultModel">기본 모델명</param>
    public LocalLLMProvider(HttpClient httpClient, ILogger<LocalLLMProvider> logger, string baseUrl, string? defaultModel = null)
        : base(httpClient, logger)
    {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _defaultModel = defaultModel ?? "llama2";
        
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public override string Name => "LocalLLM";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedModels => new[]
    {
        "llama2",
        "llama2:13b",
        "llama2:70b",
        "codellama",
        "codellama:13b",
        "mistral",
        "mixtral",
        "phi",
        "gemma"
    };

    /// <inheritdoc />
    public override string DefaultModel => _defaultModel;

    /// <inheritdoc />
    public override async Task<string> GenerateAsync(string prompt, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        try
        {
            // Ollama API 형식으로 요청
            var requestBody = new
            {
                model = model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    max_tokens = 4000
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
            {
                Content = content
            };

            SetRequestHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                HandleApiError(response, responseContent);
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var generatedText = responseJson.GetProperty("response").GetString();

            _logger.LogDebug("Generated text using local model {Model}: {Length} characters", model, generatedText?.Length ?? 0);

            return generatedText ?? string.Empty;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is UnauthorizedAccessException))
        {
            _logger.LogError(ex, "Failed to generate text using local model: {Model}", model);
            throw new InvalidOperationException($"Failed to generate text: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var requestBody = new
        {
            model = DefaultModel,
            prompt = prompt,
            stream = true,
            options = new
            {
                temperature = 0.7,
                top_p = 0.9,
                max_tokens = 4000
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
        {
            Content = content
        };

        SetRequestHeaders(request);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            HandleApiError(response, errorContent);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (!string.IsNullOrWhiteSpace(line))
            {
                JsonElement jsonElement;
                try
                {
                    jsonElement = JsonSerializer.Deserialize<JsonElement>(line);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response: {Line}", line);
                    continue;
                }
                
                if (jsonElement.TryGetProperty("response", out var responseElement))
                {
                    var chunk = responseElement.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        yield return chunk;
                    }
                }

                // 완료 확인
                if (jsonElement.TryGetProperty("done", out var doneElement) && 
                    doneElement.GetBoolean())
                {
                    yield break;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> CheckAvailabilityAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/tags");
            SetRequestHeaders(request);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local LLM availability check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public override async Task<int> CountTokensAsync(string text, string? model = null)
    {
        // 로컬 LLM의 경우 정확한 토큰 계산이 어려우므로 추정 사용
        return await Task.FromResult(EstimateTokenCount(text));
    }

    /// <summary>
    /// 사용 가능한 모델 목록 조회
    /// </summary>
    /// <returns>모델 목록</returns>
    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/tags");
            SetRequestHeaders(request);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get available models from local LLM server");
                return SupportedModels;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (json.TryGetProperty("models", out var modelsElement))
            {
                var models = new List<string>();
                foreach (var model in modelsElement.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var nameElement))
                    {
                        var modelName = nameElement.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            models.Add(modelName);
                        }
                    }
                }
                return models.AsReadOnly();
            }

            return SupportedModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available models from local LLM server");
            return SupportedModels;
        }
    }

    /// <summary>
    /// HTTP 클라이언트 설정
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 로컬 LLM은 더 오래 걸릴 수 있음
    }

    /// <inheritdoc />
    protected override void SetRequestHeaders(HttpRequestMessage request)
    {
        base.SetRequestHeaders(request);
        // 로컬 LLM은 일반적으로 인증이 필요하지 않음
    }
}