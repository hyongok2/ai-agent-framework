using AIAgentFramework.Tools.BuiltIn;
using CoreModels = AIAgentFramework.Core.Models;

Console.WriteLine("=== AI Agent Framework - EchoTool 테스트 ===\n");

// 1. EchoTool 생성
var echoTool = new EchoTool();

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

Console.WriteLine("=== 모든 테스트 완료 ===");
