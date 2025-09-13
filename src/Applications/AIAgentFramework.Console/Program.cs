using AIAgentFramework.Core.Common.Registry;
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.Core.User;
using AIAgentFramework.LLM.Extensions;
using AIAgentFramework.LLM.Providers;
using AIAgentFramework.Orchestration;
using AIAgentFramework.Registry;
using AIAgentFramework.Registry.Extensions;
using AIAgentFramework.Tools.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

// Configuration 설정 - 프로젝트 폴더 기준으로 설정
var assemblyLocation = Assembly.GetExecutingAssembly().Location;
var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? Directory.GetCurrentDirectory();

var configuration = new ConfigurationBuilder()
    .SetBasePath(assemblyDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// DI Container 설정
var services = new ServiceCollection();

// Configuration 등록
services.AddSingleton<IConfiguration>(configuration);

// HTTP Client Factory 등록
services.AddHttpClient();

// Logging 설정
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Framework 서비스 등록
services.AddRegistryWithAutoRegistration(
    typeof(Program).Assembly,              // 현재 어셈블리
    typeof(ILLMFunction).Assembly         // LLM Functions가 있는 어셈블리
);
services.AddAllTools();                       // Tools 시스템 (모든 도구 포함)
services.AddLLMServicesFromConfiguration(configuration); // LLM Provider 등록
services.AddOrchestration();                  // Orchestration 엔진

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Registry 초기화 수행
try
{
    var registryInitializer = serviceProvider.GetService<IRegistryInitializer>();
    if (registryInitializer != null)
    {
        logger.LogInformation("Registry 초기화 시작...");
        var registeredCount = await registryInitializer.InitializeAsync();
        logger.LogInformation("Registry 초기화 완료: {Count}개 컴포넌트 등록됨", registeredCount);
    }
    else
    {
        logger.LogWarning("Registry 초기화 서비스를 찾을 수 없습니다.");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Registry 초기화 실패");
}

// LLM Provider 가져오기 (DI로부터)
var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();

var baseUrl = configuration["LLM:Local:BaseUrl"] ?? "http://192.168.25.50:11434";
var defaultModel = configuration["LLM:Local:DefaultModel"] ?? "gpt-oss:20b";

// Orchestration Engine 가져오기
var orchestrationEngine = serviceProvider.GetRequiredService<IOrchestrationEngine>();

logger.LogInformation("LLM Provider 초기화 완료 (DI로부터)");
logger.LogInformation("설정 - Base URL: {BaseUrl}", baseUrl);
logger.LogInformation("설정 - Default Model: {Model}", defaultModel);
logger.LogInformation("Provider Type: {ProviderType}", llmProvider.GetType().Name);

Console.WriteLine("=========================================");
Console.WriteLine("     AI Agent Framework Console v1.0     ");
Console.WriteLine("=========================================");
Console.WriteLine();
Console.WriteLine("🚀 AI Agent Framework 준비됨!");
Console.WriteLine($"   LLM Provider: {llmProvider.GetType().Name}");
Console.WriteLine($"   Orchestration: {orchestrationEngine.GetType().Name}");
Console.WriteLine();
Console.WriteLine("명령어:");
Console.WriteLine("  exit        - 프로그램 종료");
Console.WriteLine("  help        - 도움말 표시");
Console.WriteLine("  info        - 시스템 정보 표시");
Console.WriteLine("  test        - LLM 연결 테스트");
Console.WriteLine("  models      - 사용 가능한 모델 목록");
Console.WriteLine("  functions   - LLM Functions 목록");
Console.WriteLine("  chat        - 채팅 모드 시작");
Console.WriteLine("  orchestrate - 오케스트레이션 테스트");
Console.WriteLine("  workflow    - 워크플로우 실행");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.ToLower() == "exit")
    {
        Console.WriteLine("프로그램을 종료합니다...");
        break;
    }

    if (input.ToLower() == "help")
    {
        Console.WriteLine("\n사용 가능한 명령어:");
        Console.WriteLine("  exit        - 프로그램 종료");
        Console.WriteLine("  help        - 도움말 표시");
        Console.WriteLine("  info        - 시스템 정보 표시");
        Console.WriteLine("  test        - LLM 연결 테스트");
        Console.WriteLine("  models      - 사용 가능한 모델 목록");
        Console.WriteLine("  functions   - LLM Functions 목록");
        Console.WriteLine("  chat        - 채팅 모드 시작");
        Console.WriteLine("  orchestrate - 오케스트레이션 테스트");
        Console.WriteLine("  workflow    - 워크플로우 실행");
        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "info")
    {
        Console.WriteLine("\n시스템 정보:");
        Console.WriteLine($"  .NET 버전: {Environment.Version}");
        Console.WriteLine($"  OS: {Environment.OSVersion}");
        Console.WriteLine($"  프로세서 수: {Environment.ProcessorCount}");
        Console.WriteLine();
        Console.WriteLine("프로젝트 구조:");
        Console.WriteLine("  Core:");
        Console.WriteLine("    - AIAgentFramework.Core: 핵심 추상화");
        Console.WriteLine("    - AIAgentFramework.LLM: LLM Provider (OpenAI, Claude)");
        Console.WriteLine("    - AIAgentFramework.Tools: 도구 시스템");
        Console.WriteLine("    - AIAgentFramework.Orchestration: 워크플로우 엔진");
        Console.WriteLine("    - AIAgentFramework.Registry: 컴포넌트 레지스트리");
        Console.WriteLine();
        Console.WriteLine("  Infrastructure:");
        Console.WriteLine("    - AIAgentFramework.State: 상태 관리 (Redis, InMemory)");
        Console.WriteLine("    - AIAgentFramework.Monitoring: 모니터링");
        Console.WriteLine("    - AIAgentFramework.Configuration: 설정");
        Console.WriteLine();
        Console.WriteLine("API 연결 상태:");
        Console.WriteLine($"  Local LLM: {baseUrl} ({defaultModel})");
        Console.WriteLine($"  Orchestration Engine: {orchestrationEngine.GetType().Name}");

        // Registry 상태 정보 추가
        try
        {
            var registry = serviceProvider.GetService<IAdvancedRegistry>();
            if (registry != null)
            {
                var status = registry.GetRegistryStatus();
                Console.WriteLine();
                Console.WriteLine("Registry 상태:");
                Console.WriteLine($"  총 컴포넌트: {status.TotalComponents}개");
                Console.WriteLine($"  LLM Functions: {status.LLMFunctionCount}개");
                Console.WriteLine($"  Tools: {status.ToolCount}개");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registry 상태 조회 실패: {ex.Message}");
        }

        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "orchestrate")
    {
        Console.WriteLine("\n오케스트레이션 테스트를 시작합니다...");

        try
        {
            // UserRequest 생성 (IUserRequest 인터페이스 구현)
            var userRequest = new SimpleUserRequest
            {
                Content = "파일 시스템에서 현재 디렉토리의 파일 목록을 조회하고, 그 결과를 요약해주세요.",
                RequestId = Guid.NewGuid().ToString(),
                UserId = "console-user",
                RequestedAt = DateTime.UtcNow
            };

            Console.WriteLine($"사용자 요청: {userRequest.Content}");
            Console.WriteLine("오케스트레이션 엔진 실행 중...");

            var result = await orchestrationEngine.ExecuteAsync(userRequest);

            Console.WriteLine($"실행 결과:");
            Console.WriteLine($"  성공: {result.IsSuccess}");
            Console.WriteLine($"  완료: {result.IsCompleted}");
            Console.WriteLine($"  실행 시간: {result.TotalDuration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  최종 응답: {result.FinalResponse}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"  오류 메시지: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오케스트레이션 실패: {ex.Message}");
            logger.LogError(ex, "Orchestration failed");
        }

        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "workflow")
    {
        Console.WriteLine("\n워크플로우 실행을 시작합니다...");
        Console.Write("원하는 작업을 입력하세요: ");

        var workflowInput = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(workflowInput))
        {
            Console.WriteLine("작업이 입력되지 않았습니다.");
            continue;
        }

        try
        {
            var userRequest = new SimpleUserRequest
            {
                Content = workflowInput,
                RequestId = Guid.NewGuid().ToString(),
                UserId = "console-user",
                RequestedAt = DateTime.UtcNow
            };

            Console.WriteLine("워크플로우 실행 중...");
            var result = await orchestrationEngine.ExecuteAsync(userRequest);

            Console.WriteLine($"\n워크플로우 결과:");
            Console.WriteLine($"  성공: {result.IsSuccess}");
            Console.WriteLine($"  완료: {result.IsCompleted}");
            Console.WriteLine($"  실행 시간: {result.TotalDuration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  응답:\n{result.FinalResponse}");

            if (result.ExecutionSteps?.Count > 0)
            {
                Console.WriteLine($"  실행 단계: {result.ExecutionSteps.Count}개");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"워크플로우 실행 실패: {ex.Message}");
            logger.LogError(ex, "Workflow execution failed");
        }

        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "test")
    {
        Console.WriteLine("\nLLM 연결 테스트를 시작합니다...");

        try
        {
            // 가용성 확인
            var isAvailable = await llmProvider.IsAvailableAsync();
            Console.WriteLine($"연결 가능: {(isAvailable ? "✅ 성공" : "❌ 실패")}");

            if (isAvailable)
            {
                // 간단한 테스트 프롬프트 전송
                var testPrompt = "Hello! Please respond with a simple greeting.";
                Console.WriteLine($"테스트 프롬프트: {testPrompt}");
                Console.WriteLine("응답 대기 중...");

                var response = await llmProvider.GenerateAsync(testPrompt);
                Console.WriteLine($"응답: {response}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"테스트 실패: {ex.Message}");
        }

        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "models")
    {
        Console.WriteLine("\n사용 가능한 모델 목록을 조회 중...");

        try
        {
            var models = llmProvider.SupportedModels;
            Console.WriteLine($"총 {models.Count}개의 모델이 있습니다:");

            foreach (var model in models)
            {
                Console.WriteLine($"  - {model}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모델 목록 조회 실패: {ex.Message}");
        }

        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "functions")
    {
        Console.WriteLine("\n등록된 LLM Functions 목록을 조회 중...");

        try
        {
            var registry = serviceProvider.GetService<IAdvancedRegistry>();
            if (registry != null)
            {
                var functionNames = new[]
                {
                    "generator", "interpreter", "summarizer", "planner",
                    "tool_parameter_setter", "analyzer", "completion_checker"
                };

                Console.WriteLine($"총 {functionNames.Length}개의 LLM Functions가 정의되어 있습니다:");

                foreach (var functionName in functionNames)
                {
                    var function = registry.GetLLMFunction(functionName);
                    if (function != null)
                    {
                        Console.WriteLine($"  ✅ {function.Name} - {function.Description}");
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ {functionName} - 등록되지 않음");
                    }
                }
            }
            else
            {
                Console.WriteLine("Registry 서비스를 찾을 수 없습니다.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LLM Functions 목록 조회 실패: {ex.Message}");
        }

        Console.WriteLine();
        continue;
    }

    if (input.ToLower() == "chat")
    {
        Console.WriteLine("\n채팅 모드를 시작합니다. 'quit'를 입력하면 종료됩니다.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("You: ");
            var chatInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(chatInput))
                continue;

            if (chatInput.ToLower() == "quit")
            {
                Console.WriteLine("채팅 모드를 종료합니다.");
                break;
            }

            try
            {
                Console.WriteLine("AI: 응답 중...");
                var response = await llmProvider.GenerateAsync(chatInput);
                Console.WriteLine($"AI: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        continue;
    }

    // 기본 프롬프트 처리 (채팅 모드가 아닌 경우)
    try
    {
        Console.WriteLine("처리 중...");
        var response = await llmProvider.GenerateAsync(input);
        Console.WriteLine($"응답: {response}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"오류: {ex.Message}");
    }

    Console.WriteLine();
}

// SimpleUserRequest 구현
public class SimpleUserRequest : IUserRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = "console-user";
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}