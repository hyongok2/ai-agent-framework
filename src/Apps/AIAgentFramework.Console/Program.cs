using System.Text;
using AIAgentFramework.Tools.BuiltIn.Echo;
using AIAgentFramework.Tools.BuiltIn.FileReader;
using AIAgentFramework.Tools.BuiltIn.FileWriter;
using AIAgentFramework.Tools.BuiltIn.DirectoryReader;
using AIAgentFramework.Tools.BuiltIn.DirectoryCreator;
using AIAgentFramework.Tools.Models;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.Console.Tests;

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
    Console.WriteLine("║  6. ParameterGenerator 테스트                  ║");
    Console.WriteLine("║  7. Evaluator 테스트                           ║");
    Console.WriteLine("║  8. Summarizer 테스트                          ║");
    Console.WriteLine("║  0. 종료                                       ║");
    Console.WriteLine("╚════════════════════════════════════════════════╝");
    Console.Write("\n선택: ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await OllamaTests.TestOllamaProvider(ollama);
                break;
            case "2":
                await PromptTests.TestPromptRegistry(promptRegistry, toolRegistry, ollama);
                break;
            case "3":
                await LLMFunctionTests.TestToolSelector(promptRegistry, toolRegistry, ollama);
                break;
            case "4":
                await LLMFunctionTests.TestStreaming(promptRegistry, toolRegistry, ollama);
                break;
            case "5":
                await LLMFunctionTests.TestTaskPlanner(promptRegistry, toolRegistry, llmRegistry, ollama);
                break;
            case "6":
                await LLMFunctionTests.TestParameterGenerator(promptRegistry, toolRegistry, ollama);
                break;
            case "7":
                await LLMFunctionTests.TestEvaluator(promptRegistry, ollama);
                break;
            case "8":
                await LLMFunctionTests.TestSummarizer(promptRegistry, ollama);
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
