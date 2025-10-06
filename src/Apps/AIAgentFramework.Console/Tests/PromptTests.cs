using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.Tools.Models;

namespace AIAgentFramework.Console.Tests;

public static class PromptTests
{
    public static async Task TestPromptRegistry(IPromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
    {
        System.Console.Clear();
        System.Console.WriteLine("=== AI Agent Framework - PromptRegistry 테스트 ===\n");

        System.Console.WriteLine($"등록된 프롬프트 수: {promptRegistry.GetAllPrompts().Count}");
        foreach (var promptDef in promptRegistry.GetAllPrompts())
        {
            System.Console.WriteLine($"  - {promptDef.Name} ({promptDef.Role}): {promptDef.Metadata.Description}");
        }
        System.Console.WriteLine();

        var toolSelectionPrompt = promptRegistry.GetPrompt("tool-selection");
        System.Console.WriteLine($"프롬프트: {toolSelectionPrompt?.Template.Substring(0, 100)}...");
        System.Console.WriteLine($"변수: {string.Join(", ", toolSelectionPrompt?.Variables ?? new List<string>())}\n");

        var variables = new Dictionary<string, object>
        {
            ["TOOLS"] = toolRegistry.GetToolDescriptionsForLLM(),
            ["USER_INPUT"] = "c:\\test-data\\sample.txt 파일을 읽어줘"
        };

        var validation = promptRegistry.ValidateVariables("tool-selection", variables);
        System.Console.WriteLine($"변수 검증: {(validation.IsValid ? "성공" : validation.ErrorMessage)}\n");

        var renderedPrompt = toolSelectionPrompt!.Render(variables);

        System.Console.WriteLine("--- LLM Tool 선택 테스트 ---");
        var toolSelectionResponse = await ollama.CallAsync(renderedPrompt, "llama3.1:8b");
        System.Console.WriteLine($"LLM 응답:\n{toolSelectionResponse}\n");

        System.Console.WriteLine("=== PromptRegistry 테스트 완료 ===");
    }
}
