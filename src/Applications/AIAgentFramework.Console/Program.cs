
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddOrchestration();

var serviceProvider = services.BuildServiceProvider();
var orchestrationEngine = serviceProvider.GetRequiredService<IOrchestrationEngine>();

Console.WriteLine("AI Agent Framework Console");
Console.WriteLine("명령어: exit (종료), help (도움말)");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(input))
        continue;
        
    if (input.ToLower() == "exit")
        break;
        
    if (input.ToLower() == "help")
    {
        Console.WriteLine("사용 가능한 명령어:");
        Console.WriteLine("  exit - 프로그램 종료");
        Console.WriteLine("  help - 도움말 표시");
        Console.WriteLine("  기타 - AI 에이전트에게 요청 전송");
        continue;
    }
    
    try
    {
        var userRequest = new UserRequest(input);
        var result = await orchestrationEngine.ExecuteAsync(userRequest);
        
        Console.WriteLine($"결과: {result.FinalResponse}");
        Console.WriteLine($"성공: {result.IsSuccess}");
        Console.WriteLine($"실행 시간: {result.TotalDuration.TotalMilliseconds:F0}ms");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"오류: {ex.Message}");
    }
}
