using AIAgentFramework.Tools.BuiltIn.Echo;
using AIAgentFramework.Tools.BuiltIn.FileReader;
using AIAgentFramework.Tools.Models;
using CoreModels = AIAgentFramework.Core.Models;

Console.WriteLine("=== AI Agent Framework - ToolRegistry 테스트 ===\n");

// 1. ToolRegistry 생성
var registry = new ToolRegistry();

// 2. Tool 등록
var echoTool = new EchoTool();
var fileReaderTool = new FileReaderTool();

registry.Register(echoTool);
registry.Register(fileReaderTool);

Console.WriteLine($"등록된 Tool 수: {registry.GetAllTools().Count}");

// 3. 이름으로 Tool 조회
var foundTool = registry.GetTool("echo");
Console.WriteLine($"조회된 Tool: {foundTool?.Metadata.Name ?? "없음"}");

// 4. 타입별 Tool 조회
var builtInTools = registry.GetToolsByType(AIAgentFramework.Tools.Abstractions.ToolType.BuiltIn);
Console.WriteLine($"\nBuilt-In Tools: {builtInTools.Count}개");
foreach (var tool in builtInTools)
{
    Console.WriteLine($"  - {tool.Metadata.Name}: {tool.Metadata.Description}");
}

// 5. LLM용 Tool 설명 생성
Console.WriteLine("\n--- LLM용 Tool 설명 (JSON) ---");
var toolDescriptions = registry.GetToolDescriptionsForLLM();
Console.WriteLine(toolDescriptions);

Console.WriteLine("\n=== ToolRegistry 테스트 완료 ===\n");
Console.WriteLine("=".PadRight(50, '='));
Console.WriteLine();

Console.WriteLine("=== AI Agent Framework - EchoTool 테스트 ===\n");

Console.WriteLine($"도구 이름: {echoTool.Metadata.Name}");
Console.WriteLine($"도구 설명: {echoTool.Metadata.Description}");
Console.WriteLine($"도구 타입: {echoTool.Metadata.Type}");
Console.WriteLine($"파라미터 필요: {echoTool.Contract.RequiresParameters}\n");

// 2. 실행 컨텍스트 생성
var context = CoreModels.ExecutionContext.Create(userId: "test-user");

// 3. Tool 실행 - 성공 케이스
Console.WriteLine("--- 테스트 1: 정상 입력 ---");
var input1 = "안녕하세요! AI Agent Framework입니다.";
var result1 = await echoTool.ExecuteAsync(input1, context);

Console.WriteLine($"성공 여부: {result1.IsSuccess}");
Console.WriteLine($"반환 데이터: {result1.Data}");
Console.WriteLine($"소요 시간: {result1.Duration.TotalMilliseconds}ms\n");

// 4. Tool 실행 - 실패 케이스
Console.WriteLine("--- 테스트 2: null 입력 (실패 예상) ---");
var result2 = await echoTool.ExecuteAsync(null, context);

Console.WriteLine($"성공 여부: {result2.IsSuccess}");
Console.WriteLine($"에러 메시지: {result2.ErrorMessage}");
Console.WriteLine($"소요 시간: {result2.Duration.TotalMilliseconds}ms\n");

// 5. JSON 변환 테스트
Console.WriteLine("--- 테스트 3: JSON 변환 ---");
var input3 = new { Message = "JSON 테스트", Value = 42 };
var result3 = await echoTool.ExecuteAsync(input3, context);

Console.WriteLine($"JSON 결과:\n{result3.ToJson()}\n");

Console.WriteLine("=== EchoTool 테스트 완료 ===\n");

// ========================================
// FileReaderTool 테스트
// ========================================

Console.WriteLine("=== AI Agent Framework - FileReaderTool 테스트 ===\n");

Console.WriteLine($"도구 이름: {fileReaderTool.Metadata.Name}");
Console.WriteLine($"도구 설명: {fileReaderTool.Metadata.Description}");
Console.WriteLine($"도구 타입: {fileReaderTool.Metadata.Type}\n");

// 2. 테스트 1: 파일 읽기 성공
Console.WriteLine("--- 테스트 1: 파일 읽기 성공 ---");
var testFilePath = @"c:\src\work\ai\ai-agent-framework\test-data\sample.txt";
Console.WriteLine($"파일 경로: {testFilePath}");
var fileResult1 = await fileReaderTool.ExecuteAsync(testFilePath, context);

Console.WriteLine($"성공 여부: {fileResult1.IsSuccess}");
if (fileResult1.IsSuccess)
{
    Console.WriteLine($"결과:\n{fileResult1.ToJson()}\n");
}

// 3. 테스트 2: 파일 없음 (실패 예상)
Console.WriteLine("--- 테스트 2: 존재하지 않는 파일 (실패 예상) ---");
var fileResult2 = await fileReaderTool.ExecuteAsync("non-existent-file.txt", context);

Console.WriteLine($"성공 여부: {fileResult2.IsSuccess}");
Console.WriteLine($"에러 메시지: {fileResult2.ErrorMessage}\n");

// 4. 테스트 3: null 입력 (실패 예상)
Console.WriteLine("--- 테스트 3: null 입력 (실패 예상) ---");
var fileResult3 = await fileReaderTool.ExecuteAsync(null, context);

Console.WriteLine($"성공 여부: {fileResult3.IsSuccess}");
Console.WriteLine($"에러 메시지: {fileResult3.ErrorMessage}\n");

Console.WriteLine("=== 모든 테스트 완료 ===");
