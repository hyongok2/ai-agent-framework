using AIAgentFramework.LLM.Providers;

namespace AIAgentFramework.Console.Tests;

public static class OllamaTests
{
    public static async Task TestOllamaProvider(OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - Ollama Provider 테스트 ===\n");

        System.Console.WriteLine($"Provider: {ollama.ProviderName}");
        System.Console.WriteLine($"Supported Models: {string.Join(", ", ollama.SupportedModels)}\n");

        System.Console.WriteLine("--- 테스트 1: 일반 호출 ---");
        var ollamaResponse = await ollama.CallAsync("Say hello in Korean!", "gpt-oss:20b");
        System.Console.WriteLine($"응답: {ollamaResponse}\n");

        System.Console.WriteLine("--- 테스트 2: 스트리밍 호출 ---");
        System.Console.Write("응답: ");
        await foreach (var chunk in ollama.CallStreamAsync("한국어로 간단히 AI를 설명해줘 (3문장)", "gpt-oss:20b"))
        {
            System.Console.Write(chunk);
        }
        System.Console.WriteLine("\n");

        System.Console.WriteLine("=== Ollama Provider 테스트 완료 ===");
    }
}
