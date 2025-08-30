using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.Monitoring.Services;
using AIAgentFramework.Monitoring.Extensions;
using AIAgentFramework.State.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AIAgentFramework.Integration.Tests;

/// <summary>
/// 성능 및 부하 테스트 시나리오
/// </summary>
public class PerformanceLoadTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IHealthCheckService _healthCheckService;
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IStateProvider> _mockStateProvider;

    public PerformanceLoadTests()
    {
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Mock 서비스들 설정
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockStateProvider = new Mock<IStateProvider>();
        var mockOrchestrationEngine = new Mock<IOrchestrationEngine>();
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        
        // LLM Provider 성능 테스트용 Mock 설정
        _mockLLMProvider.Setup(x => x.Name).Returns("PerformanceLLM");
        _mockLLMProvider.Setup(x => x.DefaultModel).Returns("perf-model");
        _mockLLMProvider.Setup(x => x.SupportedModels).Returns(new List<string> { "perf-model" }.AsReadOnly());
        _mockLLMProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        
        // 성능 시뮬레이션: 50-200ms 응답 시간
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(async (string prompt, CancellationToken ct) =>
                      {
                          var delay = Random.Shared.Next(50, 200);
                          await Task.Delay(delay, ct);
                          return $"Performance test response for: {prompt.Substring(0, Math.Min(50, prompt.Length))}";
                      });
        
        _mockLLMProvider.Setup(x => x.CountTokensAsync(It.IsAny<string>(), It.IsAny<string?>()))
                      .Returns(async (string text, string? _) =>
                      {
                          await Task.Delay(5); // 토큰 카운팅 지연 시뮬레이션
                          return text.Length / 4; // 대략적인 토큰 수
                      });
        
        // State Provider 성능 테스트용 Mock 설정
        var stateStorage = new ConcurrentDictionary<string, object>();
        _mockStateProvider.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                         .Returns((string key, object value, TimeSpan ttl, CancellationToken ct) =>
                         {
                             stateStorage.TryAdd(key, value);
                             return Task.CompletedTask;
                         });
        
        _mockStateProvider.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((string key, CancellationToken ct) =>
                         {
                             stateStorage.TryGetValue(key, out var value);
                             return value ?? new { message = "default", timestamp = DateTime.UtcNow };
                         });
        
        _mockStateProvider.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((string key, CancellationToken ct) => stateStorage.ContainsKey(key));
        
        _mockStateProvider.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns((string key, CancellationToken ct) =>
                         {
                             stateStorage.TryRemove(key, out _);
                             return Task.CompletedTask;
                         });

        // Mock 서비스 등록
        services.AddSingleton(mockOrchestrationEngine.Object);
        services.AddSingleton(mockLLMRegistry.Object);
        services.AddSingleton(mockToolRegistry.Object);
        services.AddSingleton(_mockStateProvider.Object);
        services.AddSingleton(_mockLLMProvider.Object);
        
        // Health Check 시스템 등록
        services.AddAllHealthChecks(options =>
        {
            options.EnableBackgroundChecks = false; // 부하 테스트 중 비활성화
            options.BackgroundCheckIntervalSeconds = 300;
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.RegisterHealthChecks();
        _healthCheckService = _serviceProvider.GetRequiredService<IHealthCheckService>();
    }

    [Fact]
    public async Task HealthCheck_Performance_Should_Complete_Within_Timeout()
    {
        // Arrange
        const int iterations = 100;
        const int maxResponseTimeMs = 1000;
        
        // Act
        var tasks = new List<Task<long>>();
        
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(MeasureHealthCheckPerformance());
        }
        
        var responseTimes = await Task.WhenAll(tasks);
        
        // Assert
        responseTimes.Should().HaveCount(iterations);
        responseTimes.Should().AllSatisfy(time => time.Should().BeLessThan(maxResponseTimeMs));
        
        // 성능 통계
        var avgTime = responseTimes.Average();
        var maxTime = responseTimes.Max();
        var minTime = responseTimes.Min();
        var p95Time = responseTimes.OrderBy(x => x).Skip((int)(iterations * 0.95)).First();
        
        avgTime.Should().BeLessThan(500, "Average response time should be under 500ms");
        p95Time.Should().BeLessThan(800, "95th percentile should be under 800ms");
        maxTime.Should().BeLessThan(maxResponseTimeMs, "Maximum response time should be under 1000ms");
    }

    [Fact]
    public async Task Concurrent_HealthChecks_Should_Handle_Heavy_Load()
    {
        // Arrange
        const int concurrentRequests = 50;
        const int maxResponseTimeMs = 2000;
        
        // Act
        var tasks = new List<Task>();
        var responseTimes = new ConcurrentBag<long>();
        var exceptions = new ConcurrentBag<Exception>();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var responseTime = await MeasureHealthCheckPerformance();
                    responseTimes.Add(responseTime);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        exceptions.Should().BeEmpty("No exceptions should occur during concurrent load");
        responseTimes.Should().HaveCount(concurrentRequests);
        responseTimes.Should().AllSatisfy(time => time.Should().BeLessThan(maxResponseTimeMs));
        
        // 동시성 성능 검증
        var avgTime = responseTimes.Average();
        avgTime.Should().BeLessThan(1000, "Average response time under load should be reasonable");
    }

    [Fact]
    public async Task Memory_Usage_Should_Remain_Stable_Under_Load()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        const int iterations = 1000;
        
        // Act
        var tasks = new List<Task>();
        
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(_healthCheckService.RunAllHealthChecksAsync());
            
            // 매 100번째마다 가비지 컬렉션 체크
            if (i % 100 == 0)
            {
                await Task.Delay(10); // 짧은 휴식
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
        
        // 메모리 증가율이 50% 미만이어야 함 (메모리 누수 없음)
        memoryIncreasePercent.Should().BeLessThan(50, 
            "Memory usage should not increase significantly under load");
    }

    [Fact]
    public async Task Sustained_Load_Should_Maintain_Performance_Over_Time()
    {
        // Arrange
        const int testDurationSeconds = 30;
        const int requestsPerSecond = 10;
        const int maxResponseTimeMs = 1500;
        
        var stopwatch = Stopwatch.StartNew();
        var responseTimes = new ConcurrentBag<long>();
        var completedRequests = 0;
        var tasks = new List<Task>();
        
        // Act - 지속적인 부하 생성
        while (stopwatch.ElapsedMilliseconds < testDurationSeconds * 1000)
        {
            for (int i = 0; i < requestsPerSecond; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var responseTime = await MeasureHealthCheckPerformance();
                    responseTimes.Add(responseTime);
                    Interlocked.Increment(ref completedRequests);
                }));
            }
            
            await Task.Delay(1000); // 1초 대기
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var totalExpectedRequests = testDurationSeconds * requestsPerSecond;
        var actualThroughput = (double)completedRequests / testDurationSeconds;
        
        completedRequests.Should().BeGreaterThan((int)(totalExpectedRequests * 0.9), 
            "Should complete at least 90% of expected requests");
        
        actualThroughput.Should().BeGreaterThan(requestsPerSecond * 0.8, 
            "Throughput should be at least 80% of target");
        
        // 지속적인 성능 검증
        var avgResponseTime = responseTimes.Average();
        avgResponseTime.Should().BeLessThan(maxResponseTimeMs, 
            "Average response time should remain stable under sustained load");
    }

    [Fact]
    public async Task State_Provider_Performance_Should_Handle_High_Volume()
    {
        // Arrange
        const int operationsCount = 1000;
        const int maxOperationTimeMs = 100;
        
        var keys = Enumerable.Range(0, operationsCount)
                           .Select(i => $"perf_test_key_{i}")
                           .ToList();
        
        var values = keys.Select(key => new { id = key, timestamp = DateTime.UtcNow, data = "test data" })
                        .ToList();
        
        // Act & Assert - Write Performance
        var writeStopwatch = Stopwatch.StartNew();
        var writeTasks = keys.Zip(values, (key, value) => 
            _mockStateProvider.Object.SetAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None));
        
        await Task.WhenAll(writeTasks);
        writeStopwatch.Stop();
        
        var avgWriteTime = (double)writeStopwatch.ElapsedMilliseconds / operationsCount;
        avgWriteTime.Should().BeLessThan(maxOperationTimeMs, 
            "Average write time should be under 100ms per operation");
        
        // Act & Assert - Read Performance
        var readStopwatch = Stopwatch.StartNew();
        var readTasks = keys.Select(key => 
            _mockStateProvider.Object.GetAsync<object>(key, CancellationToken.None));
        
        var readResults = await Task.WhenAll(readTasks);
        readStopwatch.Stop();
        
        var avgReadTime = (double)readStopwatch.ElapsedMilliseconds / operationsCount;
        avgReadTime.Should().BeLessThan(maxOperationTimeMs, 
            "Average read time should be under 100ms per operation");
        
        // 모든 읽기 작업이 성공했는지 확인
        readResults.Should().HaveCount(operationsCount);
        readResults.Should().AllSatisfy(result => result.Should().NotBeNull());
    }

    [Fact]
    public async Task LLM_Provider_Performance_Should_Handle_Concurrent_Requests()
    {
        // Arrange
        const int concurrentRequests = 20;
        const int maxResponseTimeMs = 500;
        const string testPrompt = "Performance test prompt for concurrent execution";
        
        // Act
        var tasks = new List<Task<(long responseTime, string result)>>();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            var requestId = i;
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await _mockLLMProvider.Object.GenerateAsync(
                    $"{testPrompt} - Request {requestId}", CancellationToken.None);
                stopwatch.Stop();
                
                return (stopwatch.ElapsedMilliseconds, result);
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(concurrentRequests);
        
        // 모든 응답이 성공적으로 생성되었는지 확인
        results.Should().AllSatisfy(result => 
        {
            result.result.Should().NotBeNullOrEmpty();
            result.result.Should().Contain("Performance test response");
        });
        
        // 응답 시간 검증
        var responseTimes = results.Select(r => r.responseTime).ToArray();
        var avgResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        
        avgResponseTime.Should().BeLessThan(maxResponseTimeMs, 
            "Average LLM response time should be reasonable");
        
        maxResponseTime.Should().BeLessThan(maxResponseTimeMs * 2, 
            "Maximum LLM response time should not be excessive");
    }

    [Fact]
    public async Task Error_Recovery_Performance_Should_Be_Resilient()
    {
        // Arrange
        const int totalRequests = 100;
        const int errorRate = 20; // 20% 오류율
        var successCount = 0;
        var errorCount = 0;
        
        // LLM Provider를 20% 확률로 오류 발생하도록 설정
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns<string, CancellationToken>(async (prompt, ct) =>
                      {
                          if (Random.Shared.Next(100) < errorRate)
                          {
                              throw new InvalidOperationException("Simulated error for resilience test");
                          }
                          
                          await Task.Delay(Random.Shared.Next(50, 150), ct);
                          return $"Success response for: {prompt}";
                      });
        
        // Act
        var tasks = new List<Task>();
        
        for (int i = 0; i < totalRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await _healthCheckService.RunHealthCheckAsync("LLM Provider");
                    if (result.Status == AIAgentFramework.Monitoring.Models.HealthStatus.Healthy)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        var totalProcessed = successCount + errorCount;
        totalProcessed.Should().Be(totalRequests, "All requests should be processed");
        
        var actualSuccessRate = (double)successCount / totalRequests * 100;
        actualSuccessRate.Should().BeGreaterThan(70, 
            "Success rate should be reasonable even with simulated errors");
        
        // 오류가 발생해도 시스템이 계속 동작해야 함
        errorCount.Should().BeGreaterThan(0, "Some errors should have occurred for this test to be valid");
        successCount.Should().BeGreaterThan(0, "Some successes should have occurred despite errors");
    }

    private async Task<long> MeasureHealthCheckPerformance()
    {
        var stopwatch = Stopwatch.StartNew();
        await _healthCheckService.RunAllHealthChecksAsync();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}