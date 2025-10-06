using AIAgentFramework.ChatBot.Services;
using AIAgentFramework.Core.Abstractions;
using AIAgentFramework.Core.Models;
using AIAgentFramework.Core.Services;
using AIAgentFramework.Core.Services.ChatHistory;
using AIAgentFramework.Execution.Abstractions;
using AIAgentFramework.Execution.Services;
using AIAgentFramework.LLM.Abstractions;
using AIAgentFramework.LLM.Models;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.LLM.Services.Analysis;
using AIAgentFramework.LLM.Services.Classification;
using AIAgentFramework.LLM.Services.Conversation;
using AIAgentFramework.LLM.Services.Conversion;
using AIAgentFramework.LLM.Services.Evaluation;
using AIAgentFramework.LLM.Services.Explanation;
using AIAgentFramework.LLM.Services.Extraction;
using AIAgentFramework.LLM.Services.Generation;
using AIAgentFramework.LLM.Services.ParameterGeneration;
using AIAgentFramework.LLM.Services.Planning;
using AIAgentFramework.LLM.Services.Reasoning;
using AIAgentFramework.LLM.Services.Refinement;
using AIAgentFramework.LLM.Services.Summarization;
using AIAgentFramework.LLM.Services.Validation;
using AIAgentFramework.Tools.Abstractions;
using AIAgentFramework.Tools.BuiltIn.DirectoryCreator;
using AIAgentFramework.Tools.BuiltIn.DirectoryReader;
using AIAgentFramework.Tools.BuiltIn.Echo;
using AIAgentFramework.Tools.BuiltIn.FileReader;
using AIAgentFramework.Tools.BuiltIn.FileWriter;
using AIAgentFramework.Tools.BuiltIn.PowerShellExecutor;
using AIAgentFramework.Tools.BuiltIn.TextTransformer;
using AIAgentFramework.Tools.Models;

try { Console.Clear(); } catch { }
Console.WriteLine("==========================================================");
Console.WriteLine("🤖 AI Agent Framework ChatBot");
Console.WriteLine("==========================================================\n");

// 로거 설정
var logger = new FileLogger("logs");

// 기본 인프라 설정
var ollama = new OllamaProvider("http://192.168.25.50:11434", "gpt-oss:20b");
var templatesPath = @"c:\src\work\ai\ai-agent-framework\src\Core\AIAgentFramework.LLM\Templates";
var promptRegistry = new PromptRegistry(templatesPath);

var toolRegistry = new ToolRegistry();
toolRegistry.Register(new FileReaderTool());
toolRegistry.Register(new FileWriterTool());
toolRegistry.Register(new EchoTool());
toolRegistry.Register(new DirectoryReaderTool());
toolRegistry.Register(new DirectoryCreatorTool());
toolRegistry.Register(new TextTransformerTool());
toolRegistry.Register(new PowerShellExecutorTool());

var llmRegistry = new LLMRegistry();

// LLM Functions 등록
var llmOptions = new LLMFunctionOptions
{
    EnableStreaming = true,
    ModelName = "gpt-oss:20b"
};

// Core LLM Functions
var planner = new TaskPlannerFunction(promptRegistry, ollama, toolRegistry, llmRegistry, llmOptions, logger);
var parameterGenerator = new ParameterGeneratorFunction(promptRegistry, ollama, llmOptions, logger);
var evaluator = new EvaluatorFunction(promptRegistry, ollama, llmOptions, logger);
var conversationalist = new ConversationFunction(promptRegistry, ollama, llmOptions, logger);

// Analysis & Processing Functions
var analyzer = new AnalyzerFunction(promptRegistry, ollama, llmOptions, logger);
var summarizer = new SummarizerFunction(promptRegistry, ollama, llmOptions, logger);
var converter = new ConverterFunction(promptRegistry, ollama, llmOptions, logger);
var generator = new GeneratorFunction(promptRegistry, ollama, llmOptions, logger);
var refiner = new RefinerFunction(promptRegistry, ollama, llmOptions, logger);
var extractor = new ExtractorFunction(promptRegistry, ollama, llmOptions, logger);
var classifier = new ClassifierFunction(promptRegistry, ollama, llmOptions, logger);
var validator = new ValidatorFunction(promptRegistry, ollama, llmOptions, logger);
var reasoner = new ReasonerFunction(promptRegistry, ollama, llmOptions, logger);
var explainer = new ExplainerFunction(promptRegistry, ollama, llmOptions, logger);

// Register all LLM Functions
llmRegistry.Register(planner);
llmRegistry.Register(parameterGenerator);
llmRegistry.Register(evaluator);
llmRegistry.Register(conversationalist);
llmRegistry.Register(analyzer);
llmRegistry.Register(summarizer);
llmRegistry.Register(converter);
llmRegistry.Register(generator);
llmRegistry.Register(refiner);
llmRegistry.Register(extractor);
llmRegistry.Register(classifier);
llmRegistry.Register(validator);
llmRegistry.Register(reasoner);
llmRegistry.Register(explainer);

// Execution 구성요소 설정
var executableResolver = new ExecutableResolver(toolRegistry, llmRegistry);
var parameterProcessor = new ParameterProcessor(parameterGenerator);
var toolExecutor = new ToolStepExecutor(logger);
var llmExecutor = new LLMFunctionStepExecutor();

var planExecutor = new PlanExecutor(
    executableResolver,
    parameterProcessor,
    toolExecutor,
    llmExecutor);

// Orchestrator 생성
var orchestrator = new AgentOrchestrator(llmRegistry, planExecutor);

// Chat History
var historyStore = new InMemoryChatHistoryStore();

// Chat Services
var renderer = new StreamingRenderer();
var chatService = new ChatService(orchestrator, historyStore, renderer);

Console.WriteLine("Commands:");
Console.WriteLine("  /exit    - Exit the chatbot");
Console.WriteLine("  /clear   - Start a new session");
Console.WriteLine("  /history - Show conversation history");
Console.WriteLine("==========================================================\n");

var context = new AgentContext();
await chatService.StartNewSessionAsync();

while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        renderer.RenderSystemMessage("Goodbye!");
        break;
    }

    if (input.Equals("/clear", StringComparison.OrdinalIgnoreCase))
    {
        await chatService.StartNewSessionAsync();
        context = new AgentContext();
        Console.Clear();
        continue;
    }

    if (input.Equals("/history", StringComparison.OrdinalIgnoreCase))
    {
        var history = await chatService.GetHistoryAsync();
        renderer.RenderSystemMessage($"Conversation History ({history.Count} messages):");
        foreach (var msg in history)
        {
            Console.ForegroundColor = msg.Role == MessageRole.User ? ConsoleColor.Yellow : ConsoleColor.Magenta;
            Console.WriteLine($"{msg.Role}: {msg.Content}");
            Console.ResetColor();
        }
        continue;
    }

    try
    {
        await chatService.ProcessUserInputAsync(input, context);
    }
    catch (Exception ex)
    {
        renderer.RenderError($"Error: {ex.Message}");
    }
}
