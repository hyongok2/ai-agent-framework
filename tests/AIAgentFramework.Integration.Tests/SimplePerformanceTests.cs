using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.Monitoring.Extensions;
using AIAgentFramework.State.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AIAgentFramework.Integration.Tests;

/// <summary>
/// 간단한 성능 테스트 (BasicSystemTests 기반)
/// </summary>
public class SimplePerformanceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILLMProvider> _mockLLMProvider;

    public SimplePerformanceTests()
    {
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Mock 서비스들 설정
        _mockLLMProvider = new Mock<ILLMProvider>();
        var mockOrchestrationEngine = new Mock<IOrchestrationEngine>();
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        
        // LLM Provider 성능 테스트용 Mock 설정
        _mockLLMProvider.Setup(x => x.Name).Returns("PerformanceLLM");
        _mockLLMProvider.Setup(x => x.DefaultModel).Returns("perf-model");
        _mockLLMProvider.Setup(x => x.SupportedModels).Returns(new List<string> { "perf-model" }.AsReadOnly());
        _mockLLMProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        
        // 성능 시뮬레이션: 10-50ms 응답 시간 (빠른 테스트를 위해 단축)
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(async (string prompt, CancellationToken ct) =>
                      {
                          var delay = Random.Shared.Next(10, 50);
                          await Task.Delay(delay, ct);
                          return $"Performance test response for: {prompt.Substring(0, Math.Min(20, prompt.Length))}";
                      });
        
        _mockLLMProvider.Setup(x => x.CountTokensAsync(It.IsAny<string>(), It.IsAny<string?>()))
                      .Returns(async (string text, string? _) =>
                      {
                          await Task.Delay(1); // 토큰 카운팅 지연 시뮬레이션
                          return Math.Min(text.Length / 4, 100); // 대략적인 토큰 수
                      });

        // Mock Orchestration Result 설정 (각 호출마다 새로운 세션 ID 반환)
        var mockResult = new Mock<IOrchestrationResult>();
        mockResult.Setup(x => x.SessionId).Returns(() => Guid.NewGuid().ToString());
        mockResult.Setup(x => x.IsCompleted).Returns(true);
        mockResult.Setup(x => x.IsSuccess).Returns(true);
        mockResult.Setup(x => x.FinalResponse).Returns("Performance test response");
        mockResult.Setup(x => x.ExecutionSteps).Returns(new List<IExecutionStep>());
        mockResult.Setup(x => x.TotalDuration).Returns(TimeSpan.FromMilliseconds(Random.Shared.Next(50, 200)));
        mockResult.Setup(x => x.Metadata).Returns(new Dictionary<string, object>());

        mockOrchestrationEngine.Setup(x => x.ExecuteAsync(It.IsAny<IUserRequest>()))
                              .ReturnsAsync(mockResult.Object);

        // Mock 서비스 등록
        services.AddSingleton(mockOrchestrationEngine.Object);
        services.AddSingleton(mockLLMRegistry.Object);
        services.AddSingleton(mockToolRegistry.Object);
        services.AddSingleton(_mockLLMProvider.Object);
        
        // State 관리 시스템 등록 (InMemoryStateProvider 사용)
        services.AddSingleton<AIAgentFramework.State.Interfaces.IStateProvider, AIAgentFramework.State.Providers.InMemoryStateProvider>();
        
        // Monitoring 시스템 등록
        services.AddAIAgentMonitoring();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task LLM_Performance_Should_Handle_Multiple_Requests()
    {
        // Arrange
        const int requestCount = 10;
        const int maxResponseTimeMs = 200;
        var llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
        
        // Act
        var tasks = new List<Task<(long responseTime, string result)>>();
        
        for (int i = 0; i < requestCount; i++)
        {
            var requestId = i;
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await llmProvider.GenerateAsync($"Test request {requestId}", CancellationToken.None);
                stopwatch.Stop();
                
                return (stopwatch.ElapsedMilliseconds, result);
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(requestCount);
        results.Should().AllSatisfy(result => 
        {
            result.result.Should().NotBeNullOrEmpty();
            result.result.Should().Contain("Performance test response");
            result.responseTime.Should().BeLessThan(maxResponseTimeMs);
        });
        
        var avgResponseTime = results.Average(r => r.responseTime);
        avgResponseTime.Should().BeLessThan(100, "Average response time should be reasonable");
    }

    [Fact]
    public async Task State_Provider_Performance_Should_Handle_Concurrent_Operations()
    {
        // Arrange
        const int operationsCount = 20;
        const int maxOperationTimeMs = 100;
        var stateProvider = _serviceProvider.GetRequiredService<AIAgentFramework.State.Interfaces.IStateProvider>();
        
        var keys = Enumerable.Range(0, operationsCount)
                           .Select(i => $"perf_test_key_{i}")
                           .ToList();
        
        // Act - Write Performance Test
        var writeStopwatch = Stopwatch.StartNew();
        var writeTasks = keys.Select(key => 
            stateProvider.SetAsync(key, $"value_{key}", TimeSpan.FromMinutes(5), CancellationToken.None));
        
        await Task.WhenAll(writeTasks);
        writeStopwatch.Stop();
        
        var avgWriteTime = (double)writeStopwatch.ElapsedMilliseconds / operationsCount;
        avgWriteTime.Should().BeLessThan(maxOperationTimeMs, 
            "Average write time should be under 100ms per operation");
        
        // Act - Read Performance Test
        var readStopwatch = Stopwatch.StartNew();
        var readTasks = keys.Select(key => 
            stateProvider.GetAsync<string>(key, CancellationToken.None));
        
        var readResults = await Task.WhenAll(readTasks);
        readStopwatch.Stop();
        
        var avgReadTime = (double)readStopwatch.ElapsedMilliseconds / operationsCount;
        avgReadTime.Should().BeLessThan(maxOperationTimeMs, 
            "Average read time should be under 100ms per operation");
        
        // Verify all operations succeeded
        readResults.Should().HaveCount(operationsCount);
        readResults.Should().AllSatisfy(result => result.Should().NotBeNull());
    }

    [Fact]
    public async Task Orchestration_Performance_Should_Handle_Concurrent_Requests()
    {
        // Arrange
        const int concurrentRequests = 5;
        const int maxResponseTimeMs = 300;
        var orchestrationEngine = _serviceProvider.GetRequiredService<IOrchestrationEngine>();
        
        // Act
        var tasks = new List<Task<(long responseTime, IOrchestrationResult result)>>();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            var requestId = i;
            tasks.Add(Task.Run(async () =>
            {
                var mockRequest = new Mock<IUserRequest>();
                mockRequest.Setup(x => x.Content).Returns($"Performance request {requestId}");
                mockRequest.Setup(x => x.RequestId).Returns(Guid.NewGuid().ToString());
                mockRequest.Setup(x => x.UserId).Returns("PerformanceUser");
                mockRequest.Setup(x => x.Metadata).Returns(new Dictionary<string, object>());
                mockRequest.Setup(x => x.RequestedAt).Returns(DateTime.UtcNow);
                
                var stopwatch = Stopwatch.StartNew();
                var result = await orchestrationEngine.ExecuteAsync(mockRequest.Object);
                stopwatch.Stop();
                
                return (stopwatch.ElapsedMilliseconds, result);
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(result => 
        {
            result.result.Should().NotBeNull();
            result.result.IsCompleted.Should().BeTrue();
            result.result.IsSuccess.Should().BeTrue();
            result.responseTime.Should().BeLessThan(maxResponseTimeMs);
        });
        
        // Verify all session IDs are unique
        var sessionIds = results.Select(r => r.result.SessionId).ToArray();
        sessionIds.Should().OnlyHaveUniqueItems("Each request should have a unique session ID");
        
        var avgResponseTime = results.Average(r => r.responseTime);
        avgResponseTime.Should().BeLessThan(150, "Average orchestration response time should be reasonable");
    }

    [Fact]
    public async Task Memory_Usage_Should_Remain_Stable_During_Operations()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        const int iterations = 50; // Reduced for faster test
        var stateProvider = _serviceProvider.GetRequiredService<AIAgentFramework.State.Interfaces.IStateProvider>();
        
        // Act
        var tasks = new List<Task>();
        
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(stateProvider.SetAsync($"memory_test_{i}", $"data_{i}", TimeSpan.FromMinutes(1), CancellationToken.None));
            
            if (i % 10 == 0)
            {
                await Task.Delay(1); // 짧은 휴식
            }
        }
        
        await Task.WhenAll(tasks);
        
        // 강제 가비지 컬렉션
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePercent = (double)memoryIncrease / initialMemory * 100;
        
        // 메모리 증가율이 100% 미만이어야 함 (메모리 누수 없음)
        memoryIncreasePercent.Should().BeLessThan(100, 
            "Memory usage should not increase excessively during operations");
    }

    [Fact]
    public async Task Token_Counting_Performance_Should_Be_Fast()
    {
        // Arrange
        const int tokenCountRequests = 20;
        const int maxTokenCountTimeMs = 50;
        var llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
        var testTexts = Enumerable.Range(0, tokenCountRequests)
                                .Select(i => $"This is test text number {i} for token counting performance evaluation.")
                                .ToList();
        
        // Act
        var tasks = testTexts.Select(async text =>
        {
            var stopwatch = Stopwatch.StartNew();
            var tokenCount = await llmProvider.CountTokensAsync(text, null);
            stopwatch.Stop();
            
            return new { tokenCount, responseTime = stopwatch.ElapsedMilliseconds };
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(tokenCountRequests);
        results.Should().AllSatisfy(result => 
        {
            result.tokenCount.Should().BeGreaterThan(0);
            result.responseTime.Should().BeLessThan(maxTokenCountTimeMs);
        });
        
        var avgResponseTime = results.Average(r => r.responseTime);
        avgResponseTime.Should().BeLessThan(20, "Average token counting time should be very fast");
    }

    public void Dispose()
    {
        try
        {
            _serviceProvider?.Dispose();
        }
        catch (Exception)
        {
            // 처리 중 발생할 수 있는 예외 무시
        }
    }
}