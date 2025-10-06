using AIAgentFramework.Core.Models;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.Execution.Services;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Services.Evaluation;
using AIAgentFramework.LLM.Services.ParameterGeneration;
using AIAgentFramework.LLM.Services.Planning;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.BuiltIn.FileReader;
using AIAgentFramework.Tools.BuiltIn.FileWriter;
using AIAgentFramework.Tools.BuiltIn.Echo;
using AIAgentFramework.Tools.BuiltIn.TextTransformer;
using AIAgentFramework.Tools.Models;

try { Console.Clear(); } catch { } // Ignore in piped mode
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   AI Agent Framework - Orchestrator 테스트               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// 1. 기본 인프라 설정
var ollama = new OllamaProvider("http://192.168.25.50:11434", "gpt-oss:20b");
var templatesPath = @"c:\src\work\ai\ai-agent-framework\src\Core\AIAgentFramework.LLM\Templates";
var promptRegistry = new PromptRegistry(templatesPath);

var toolRegistry = new ToolRegistry();
toolRegistry.Register(new FileReaderTool());
toolRegistry.Register(new FileWriterTool());
toolRegistry.Register(new EchoTool());
toolRegistry.Register(new TextTransformerTool());

var llmRegistry = new LLMRegistry();

// 2. LLM Functions 등록
var plannerOptions = new LLMFunctionOptions
{
    EnableStreaming = true,
    ModelName = "gpt-oss:20b"
};

var planner = new TaskPlannerFunction(promptRegistry, ollama, toolRegistry, llmRegistry, plannerOptions);
var parameterGenerator = new ParameterGeneratorFunction(promptRegistry, ollama, plannerOptions);
var evaluator = new EvaluatorFunction(promptRegistry, ollama, plannerOptions);

llmRegistry.Register(planner);
llmRegistry.Register(parameterGenerator);
llmRegistry.Register(evaluator);

// 3. Execution 구성요소 설정
var executableResolver = new ExecutableResolver(toolRegistry, llmRegistry);
var parameterProcessor = new ParameterProcessor(parameterGenerator);
var toolExecutor = new ToolStepExecutor();
var llmExecutor = new LLMFunctionStepExecutor();

var planExecutor = new PlanExecutor(
    executableResolver,
    parameterProcessor,
    toolExecutor,
    llmExecutor);

// 4. Orchestrator 생성
var orchestrator = new AgentOrchestrator(llmRegistry, planExecutor);

Console.WriteLine("✅ Orchestrator 초기화 완료\n");

// 5. 테스트 시나리오
await RunScenario1(orchestrator);
Console.WriteLine("\n" + new string('─', 60) + "\n");
await RunScenario2(orchestrator);

Console.WriteLine("\n\n✅ 모든 테스트 완료!");

// ============================================================
// 테스트 시나리오들
// ============================================================

static async Task RunScenario1(AgentOrchestrator orchestrator)
{
    Console.WriteLine("📋 시나리오 1: 간단한 작업 (파일 읽기 및 내용 출력)");
    Console.WriteLine();

    var context = AgentContext.Create("test-user");
    var userInput = "c:\\test-data\\sample.txt 파일을 읽고 내용을 Echo 도구로 출력해줘";

    Console.WriteLine($"사용자 요청: {userInput}");
    Console.WriteLine();

    var result = await orchestrator.ExecuteAsync(userInput, context);

    Console.WriteLine("--- 결과 ---");
    Console.WriteLine($"성공 여부: {result.IsSuccess}");

    if (result is AIAgentFramework.Execution.Models.OrchestratorResult orchResult)
    {
        if (!orchResult.IsSuccess)
        {
            Console.WriteLine($"❌ 오류: {orchResult.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"계획 요약: {orchResult.PlanSummary}");
            Console.WriteLine($"실행 요약: {orchResult.ExecutionSummary}");
            Console.WriteLine($"평가 점수: {orchResult.EvaluationScore}점");
            Console.WriteLine($"평가 내용: {orchResult.EvaluationSummary}");

            if (orchResult.Improvements?.Count > 0)
            {
                Console.WriteLine($"\n개선 권장사항:");
                foreach (var improvement in orchResult.Improvements)
                {
                    Console.WriteLine($"  - {improvement}");
                }
            }
        }
    }
}

static async Task RunScenario2(AgentOrchestrator orchestrator)
{
    Console.WriteLine("📋 시나리오 2: 복잡한 작업 (스트리밍)");
    Console.WriteLine();

    var context = AgentContext.Create("test-user");
    var userInput = "c:\\test-data\\sample.txt 파일을 읽고, 내용을 대문자로 변환해서 c:\\test-data\\output-upper.txt에 저장해줘";

    Console.WriteLine($"사용자 요청: {userInput}");
    Console.WriteLine();
    Console.WriteLine("--- 실시간 스트리밍 출력 ---");

    await foreach (var chunk in orchestrator.ExecuteStreamAsync(userInput, context))
    {
        if (!string.IsNullOrEmpty(chunk.Content))
        {
            Console.Write(chunk.Content);
        }
    }
}
