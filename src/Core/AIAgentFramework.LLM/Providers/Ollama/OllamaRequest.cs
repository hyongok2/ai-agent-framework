using System.Text.Json.Serialization;

namespace AIAgentFramework.LLM.Providers.Ollama;

/// <summary>
/// Ollama API 요청 모델
/// </summary>
internal class OllamaRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}
