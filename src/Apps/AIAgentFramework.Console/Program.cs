using System.Text;
using System.Text.Json;
using AIAgentFramework.Tools.BuiltIn.Echo;
using AIAgentFramework.Tools.BuiltIn.FileReader;
using AIAgentFramework.Tools.BuiltIn.FileWriter;
using AIAgentFramework.Tools.BuiltIn.DirectoryReader;
using AIAgentFramework.Tools.BuiltIn.DirectoryCreator;
using AIAgentFramework.Tools.Models;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Services.ToolSelection;
using AIAgentFramework.LLM.Services.Planning;
using CoreModels = AIAgentFramework.Core.Models;

// 콘솔 UTF-8 인코딩 설정
Console.OutputEncoding = Encoding.UTF8;

// ========================================
// 공통 초기화
// ========================================

var ollama = new OllamaProvider("http://192.168.25.50:11434", "gpt-oss:20b");
var toolRegistry = new ToolRegistry();

// 기본 Tool 등록
toolRegistry.Register(new EchoTool());
toolRegistry.Register(new FileReaderTool());
toolRegistry.Register(new FileWriterTool());
toolRegistry.Register(new DirectoryReaderTool());
toolRegistry.Register(new DirectoryCreatorTool());

var templatesPath = @"c:\src\work\ai\ai-agent-framework\src\Core\AIAgentFramework.LLM\Templates";
var promptRegistry = new PromptRegistry(templatesPath);

var llmRegistry = new LLMRegistry();

// ========================================
// 메인 메뉴
// ========================================

while (true)
{
    Console.Clear();
    Console.WriteLine("╔════════════════════════════════════════════════╗");
    Console.WriteLine("║   AI Agent Framework - 테스트 메뉴             ║");
    Console.WriteLine("╠════════════════════════════════════════════════╣");
    Console.WriteLine("║  1. Ollama Provider 테스트                     ║");
    Console.WriteLine("║  2. PromptRegistry 테스트                      ║");
    Console.WriteLine("║  3. ToolSelectorFunction 테스트                ║");
    Console.WriteLine("║  4. Streaming 테스트                           ║");
    Console.WriteLine("║  5. TaskPlanner 테스트                         ║");
    Console.WriteLine("║  0. 종료                                        ║");
    Console.WriteLine("╚════════════════════════════════════════════════╝");
    Console.Write("\n선택: ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await TestOllamaProvider(ollama);
                break;
            case "2":
                await TestPromptRegistry(promptRegistry, toolRegistry, ollama);
                break;
            case "3":
                await TestToolSelector(promptRegistry, toolRegistry, ollama);
                break;
            case "4":
                await TestStreaming(promptRegistry, toolRegistry, ollama);
                break;
            case "5":
                await TestTaskPlanner(promptRegistry, toolRegistry, llmRegistry, ollama);
                break;
            case "0":
                Console.WriteLine("\n프로그램을 종료합니다.");
                return;
            default:
                Console.WriteLine("\n잘못된 선택입니다.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n오류 발생: {ex.Message}");
    }

    Console.WriteLine("\n\n계속하려면 아무 키나 누르세요...");
    Console.ReadKey();
}

// ========================================
// 테스트 메서드들
// ========================================

static async Task TestOllamaProvider(OllamaProvider ollama)
{
    Console.Clear();
    Console.WriteLine("=== AI Agent Framework - Ollama Provider 테스트 ===\n");

    Console.WriteLine($"Provider: {ollama.ProviderName}");
    Console.WriteLine($"Supported Models: {string.Join(", ", ollama.SupportedModels)}\n");

    Console.WriteLine("--- 테스트 1: 일반 호출 ---");
    var ollamaResponse = await ollama.CallAsync("Say hello in Korean!", "gpt-oss:20b");
    Console.WriteLine($"응답: {ollamaResponse}\n");

    Console.WriteLine("--- 테스트 2: 스트리밍 호출 ---");
    Console.Write("응답: ");
    await foreach (var chunk in ollama.CallStreamAsync("한국어로 간단히 AI를 설명해줘 (3문장)", "gpt-oss:20b"))
    {
        Console.Write(chunk);
    }
    Console.WriteLine("\n");

    Console.WriteLine("=== Ollama Provider 테스트 완료 ===");
}

static async Task TestPromptRegistry(PromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
{
    Console.Clear();
    Console.WriteLine("=== AI Agent Framework - PromptRegistry 테스트 ===\n");

    Console.WriteLine($"등록된 프롬프트 수: {promptRegistry.GetAllPrompts().Count}");
    foreach (var promptDef in promptRegistry.GetAllPrompts())
    {
        Console.WriteLine($"  - {promptDef.Name} ({promptDef.Role}): {promptDef.Metadata.Description}");
    }
    Console.WriteLine();

    var toolSelectionPrompt = promptRegistry.GetPrompt("tool-selection");
    Console.WriteLine($"프롬프트: {toolSelectionPrompt?.Template.Substring(0, 100)}...");
    Console.WriteLine($"변수: {string.Join(", ", toolSelectionPrompt?.Variables ?? new List<string>())}\n");

    var variables = new Dictionary<string, object>
    {
        ["TOOLS"] = toolRegistry.GetToolDescriptionsForLLM(),
        ["USER_INPUT"] = "c:\\test-data\\sample.txt 파일을 읽어줘"
    };

    var validation = promptRegistry.ValidateVariables("tool-selection", variables);
    Console.WriteLine($"변수 검증: {(validation.IsValid ? "성공" : validation.ErrorMessage)}\n");

    var renderedPrompt = toolSelectionPrompt!.Render(variables);

    Console.WriteLine("--- LLM Tool 선택 테스트 ---");
    var toolSelectionResponse = await ollama.CallAsync(renderedPrompt, "llama3.1:8b");
    Console.WriteLine($"LLM 응답:\n{toolSelectionResponse}\n");

    Console.WriteLine("=== PromptRegistry 테스트 완료 ===");
}

static async Task TestToolSelector(PromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
{
    Console.Clear();
    Console.WriteLine("=== AI Agent Framework - ToolSelectorFunction 테스트 ===\n");

    var toolSelectorFunction = new ToolSelectorFunction(
        promptRegistry,
        ollama,
        toolRegistry
    );

    var context = new LLMContext
    {
        UserInput = "c:\\test-data\\sample.txt 파일을 읽어줘"
    };

    Console.WriteLine($"사용자 요청: {context.UserInput}\n");
    Console.WriteLine("--- ToolSelectorFunction 실행 중... ---\n");

    var llmResult = await toolSelectorFunction.ExecuteAsync(context);
    var toolSelection = (ToolSelectionResult)llmResult.ParsedData!;

    Console.WriteLine($"선택된 Tool: {toolSelection.ToolName}");
    Console.WriteLine($"파라미터: {toolSelection.Parameters}");
    Console.WriteLine($"LLM Role: {llmResult.Role}");
    Console.WriteLine($"원본 응답:\n{llmResult.RawResponse}\n");

    Console.WriteLine("=== ToolSelectorFunction 테스트 완료 ===");
}

static async Task TestStreaming(PromptRegistry promptRegistry, ToolRegistry toolRegistry, OllamaProvider ollama)
{
    Console.Clear();
    Console.WriteLine("=== AI Agent Framework - Streaming 테스트 ===\n");

    var streamingOptions = new LLMFunctionOptions
    {
        ModelName = "gpt-oss:20b",
        EnableStreaming = true,
        TimeoutMs = 60000
    };

    var streamingToolSelector = new ToolSelectorFunction(
        promptRegistry,
        ollama,
        toolRegistry,
        streamingOptions
    );

    Console.WriteLine($"스트리밍 지원: {streamingToolSelector.SupportsStreaming}");
    Console.WriteLine($"모델: {streamingOptions.ModelName}\n");

    var streamingContext = new LLMContext
    {
        UserInput = "안녕이라고 메시지 출력해줘"
    };

    Console.WriteLine($"사용자 요청: {streamingContext.UserInput}\n");
    Console.WriteLine("--- 스트리밍 응답 수신 중... ---");
    Console.Write("응답: ");

    var fullResponse = new StringBuilder();
    await foreach (var chunk in streamingToolSelector.ExecuteStreamAsync(streamingContext))
    {
        if (!chunk.IsFinal && !string.IsNullOrEmpty(chunk.Content))
        {
            Console.Write(chunk.Content);
            fullResponse.Append(chunk.Content);
        }
        else if (chunk.IsFinal)
        {
            Console.WriteLine($"\n\n누적 토큰: {chunk.AccumulatedTokens}");
            Console.WriteLine($"총 청크 수: {chunk.Index}");
        }
    }

    Console.WriteLine("\n\n=== Streaming 테스트 완료 ===");
}

static async Task TestTaskPlanner(PromptRegistry promptRegistry, ToolRegistry toolRegistry, LLMRegistry llmRegistry, OllamaProvider ollama)
{
    Console.Clear();
    Console.WriteLine("=== AI Agent Framework - TaskPlanner 테스트 ===\n");

    var taskPlanner = new TaskPlannerFunction(
        promptRegistry,
        ollama,
        toolRegistry,
        llmRegistry
    );

    var planningContext = new LLMContext
    {
        UserInput = "c:\\test-data 폴더의 모든 txt 파일을 읽고, 각 파일의 내용을 요약한 다음, 결과를 summary.md 파일로 저장해줘"
    };

    Console.WriteLine($"사용자 요청: {planningContext.UserInput}\n");
    Console.WriteLine("--- TaskPlanner 실행 중... ---\n");

    var planResult = await taskPlanner.ExecuteAsync(planningContext);
    var plan = (PlanningResult)planResult.ParsedData!;

    Console.WriteLine($"📋 계획 요약: {plan.Summary}\n");
    Console.WriteLine($"✅ 실행 가능: {plan.IsExecutable}");
    Console.WriteLine($"⏱️  예상 시간: {plan.TotalEstimatedSeconds}초\n");

    if (plan.Steps.Count > 0)
    {
        Console.WriteLine("📝 실행 단계:");
        foreach (var step in plan.Steps)
        {
            Console.WriteLine($"\n  [{step.StepNumber}] {step.Description}");
            Console.WriteLine($"      Tool: {step.ToolName}");
            Console.WriteLine($"      Parameters: {step.Parameters}");
            if (!string.IsNullOrEmpty(step.OutputVariable))
            {
                Console.WriteLine($"      Output → {step.OutputVariable}");
            }
            if (step.DependsOn.Count > 0)
            {
                Console.WriteLine($"      Depends on: {string.Join(", ", step.DependsOn)}");
            }
            if (step.EstimatedSeconds.HasValue)
            {
                Console.WriteLine($"      Est. time: {step.EstimatedSeconds}초");
            }
        }
    }

    if (plan.Constraints.Count > 0)
    {
        Console.WriteLine($"\n⚠️  제약사항:");
        foreach (var constraint in plan.Constraints)
        {
            Console.WriteLine($"  - {constraint}");
        }
    }

    if (!plan.IsExecutable && !string.IsNullOrEmpty(plan.ExecutionBlocker))
    {
        Console.WriteLine($"\n❌ 실행 불가 이유:\n{plan.ExecutionBlocker}");
    }

    Console.WriteLine($"\n\n원본 LLM 응답:\n{planResult.RawResponse}\n");

    Console.WriteLine("=== TaskPlanner 테스트 완료 ===");
}
