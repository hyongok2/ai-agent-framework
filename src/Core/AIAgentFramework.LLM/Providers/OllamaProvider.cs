using System.Net.Http.Json;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Providers.Ollama;

namespace AIAgentFramework.LLM.Providers;

/// <summary>
/// Ollama LLM Provider
/// 로컬 Ollama 서버와 통신
/// </summary>
public class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public string ProviderName => "Ollama";
    public IReadOnlyList<string> SupportedModels { get; }

    public OllamaProvider(string baseUrl, string defaultModel = "llama3.1:8b")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _defaultModel = defaultModel;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        SupportedModels = new[] { defaultModel };
    }

    public async Task<string> CallAsync(
        string prompt,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(prompt, modelName);
        var response = await SendRequestAsync(request, cancellationToken);
        return response.Response;
    }

    public async IAsyncEnumerable<string> CallStreamAsync(
        string prompt,
        string modelName,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(prompt, modelName, streaming: true);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = ParseStreamChunk(line);
            if (chunk != null && !string.IsNullOrEmpty(chunk.Response))
            {
                yield return chunk.Response;
            }

            if (chunk?.Done == true)
                break;
        }
    }

    private OllamaRequest CreateRequest(string prompt, string modelName, bool streaming = false)
    {
        return new OllamaRequest
        {
            Model = modelName ?? _defaultModel,
            Prompt = prompt,
            Stream = streaming
        };
    }

    private async Task<OllamaResponse> SendRequestAsync(
        OllamaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
            cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException("Ollama 응답이 비어있습니다.");
    }

    private OllamaResponse? ParseStreamChunk(string line)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<OllamaResponse>(line);
        }
        catch
        {
            return null;
        }
    }
}
