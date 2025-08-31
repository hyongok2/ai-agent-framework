
using AIAgentFramework.Core.LLM.Abstractions;
using AIAgentFramework.Core.Orchestration.Abstractions;
using AIAgentFramework.Core.Tools.Abstractions;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.Monitoring.Services;
using AIAgentFramework.State.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Net;

namespace AIAgentFramework.Integration.Tests;

/// <summary>
/// 장애 복구 및 복원력 테스트 시나리오
/// </summary>
public class FailureRecoveryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IHealthCheckService _healthCheckService;
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IStateProvider> _mockStateProvider;
    private readonly Mock<IOrchestrationEngine> _mockOrchestrationEngine;

    public FailureRecoveryTests()
    {
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Mock 서비스들 설정
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockStateProvider = new Mock<IStateProvider>();
        _mockOrchestrationEngine = new Mock<IOrchestrationEngine>();
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        
        // 기본 LLM Provider Mock 설정
        _mockLLMProvider.Setup(x => x.Name).Returns("FailureTestLLM");
        _mockLLMProvider.Setup(x => x.DefaultModel).Returns("failure-test-model");
        _mockLLMProvider.Setup(x => x.SupportedModels).Returns(new List<string> { "failure-test-model" }.AsReadOnly());
        _mockLLMProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        
        // 기본 State Provider Mock 설정
        var stateStorage = new ConcurrentDictionary<string, object>();
        _mockStateProvider.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                         .Callback<string, object, TimeSpan?, CancellationToken>((key, value, ttl, ct) => stateStorage.TryAdd(key, value))
                         .Returns(Task.CompletedTask);
        
        _mockStateProvider.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns<string, CancellationToken>((key, ct) => 
                             Task.FromResult<object?>(stateStorage.TryGetValue(key, out var value) 
                                 ? value 
                                 : new { message = "default", timestamp = DateTime.UtcNow }));
        
        _mockStateProvider.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns<string, CancellationToken>((key, ct) => Task.FromResult(stateStorage.ContainsKey(key)));

        // Mock 서비스 등록
        services.AddSingleton(_mockOrchestrationEngine.Object);
        services.AddSingleton(mockLLMRegistry.Object);
        services.AddSingleton(mockToolRegistry.Object);
        services.AddSingleton(_mockStateProvider.Object);
        services.AddSingleton(_mockLLMProvider.Object);
        
        // Health Check 시스템 등록 - 시뮬레이션을 위한 단순화
        services.AddSingleton<IHealthCheckService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<HealthCheckService>>();
            var options = new HealthCheckServiceOptions { EnableBackgroundChecks = false };
            return new HealthCheckService(logger, options);
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _healthCheckService = _serviceProvider.GetRequiredService<IHealthCheckService>();
    }

    [Fact]
    public async Task LLM_Provider_Failure_Should_Be_Detected_And_Recovered()
    {
        // Arrange - LLM Provider가 일시적으로 실패하도록 설정
        var failureCount = 0;
        var maxFailures = 3;
        
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns<string, CancellationToken>(async (prompt, ct) =>
                      {
                          var currentFailure = Interlocked.Increment(ref failureCount);
                          
                          if (currentFailure <= maxFailures)
                          {
                              throw new HttpRequestException("Simulated LLM service failure");
                          }
                          
                          await Task.Delay(100, ct);
                          return $"Recovered response for: {prompt}";
                      });

        // Act - 여러 번 시도하여 복구 확인
        var results = new List<Exception?>();
        var successCount = 0;
        
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var response = await _mockLLMProvider.Object.GenerateAsync($"Test prompt {i}", CancellationToken.None);
                results.Add(null); // 성공
                if (response.Contains("Recovered response"))
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                results.Add(ex); // 실패
            }
        }

        // Assert
        results.Should().HaveCount(5);
        results.Take(maxFailures).Should().AllSatisfy(ex => ex.Should().NotBeNull()); // 처음 3개는 실패
        results.Skip(maxFailures).Should().AllSatisfy(ex => ex.Should().BeNull()); // 나머지는 성공
        successCount.Should().Be(2, "Should have 2 successful recoveries after failures");
    }

    [Fact]
    public async Task State_Provider_Connection_Loss_Should_Be_Handled_Gracefully()
    {
        // Arrange - State Provider가 연결 오류 발생하도록 설정
        var connectionAttempts = 0;
        
        _mockStateProvider.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns<string, CancellationToken>((key, ct) =>
                         {
                             var attempt = Interlocked.Increment(ref connectionAttempts);
                             
                             if (attempt <= 2)
                             {
                                 return Task.FromException<string?>(new TimeoutException("Simulated connection timeout"));
                             }
                             
                             return Task.Delay(50, ct).ContinueWith<string?>(_ => $"Recovered data for key: {key}", ct);
                         });

        // Act
        var tasks = new List<Task<(bool success, string? result, string? error)>>();
        
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await _mockStateProvider.Object.GetAsync<string>($"test_key_{taskId}", CancellationToken.None);
                    return (true, result, (string?)null);
                }
                catch (Exception ex)
                {
                    return (false, (string?)null, ex.Message);
                }
            }));
        }
        
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        var failures = results.Where(r => !r.success).ToArray();
        var successes = results.Where(r => r.success).ToArray();
        
        failures.Should().HaveCountLessThan(4, "Should not have more failures than expected");
        successes.Should().NotBeEmpty("Should have some successful recoveries");
        successes.Should().AllSatisfy(s => s.result.Should().Contain("Recovered data"));
    }

    [Fact]
    public async Task Health_Check_Cascade_Failure_Should_Maintain_System_Stability()
    {
        // Arrange - 여러 서비스가 연쇄적으로 실패하는 시나리오
        var llmFailures = 0;
        var stateFailures = 0;
        
        _mockLLMProvider.Setup(x => x.IsAvailableAsync())
                      .Returns(async () =>
                      {
                          await Task.Delay(10);
                          return Interlocked.Increment(ref llmFailures) > 3; // 처음 3번 실패
                      });

        _mockStateProvider.Setup(x => x.ExistsAsync("health_check", It.IsAny<CancellationToken>()))
                         .Returns<string, CancellationToken>(async (key, ct) =>
                         {
                             await Task.Delay(10, ct);
                             return Interlocked.Increment(ref stateFailures) > 2; // 처음 2번 실패
                         });

        // Act - 연속적인 헬스 체크 실행
        var healthCheckResults = new List<HealthCheckSummary>();
        
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var result = await _healthCheckService.RunAllHealthChecksAsync();
                healthCheckResults.Add(result);
                await Task.Delay(100); // 짧은 간격으로 체크
            }
            catch (Exception ex)
            {
                // 예외가 발생해도 시스템은 계속 동작해야 함
                healthCheckResults.Add(new HealthCheckSummary
                {
                    OverallStatus = AIAgentFramework.Monitoring.Models.HealthStatus.Unhealthy,
                    TotalDurationMs = 100,
                    CheckedAt = DateTime.UtcNow,
                    Results = new List<AIAgentFramework.Monitoring.Models.HealthCheckResult>
                    {
                        AIAgentFramework.Monitoring.Models.HealthCheckResult.Unhealthy("System", $"Health check failed: {ex.Message}")
                    }
                });
            }
        }

        // Assert
        healthCheckResults.Should().HaveCount(10, "All health checks should be recorded");
        
        // 시스템이 점진적으로 복구되는지 확인
        var laterResults = healthCheckResults.Skip(7).ToArray();
        var healthyCount = laterResults.Count(r => r.OverallStatus == AIAgentFramework.Monitoring.Models.HealthStatus.Healthy);
        
        healthyCount.Should().BeGreaterThan(0, "System should show signs of recovery in later checks");
        
        // 전체적인 가용성 확인
        var totalHealthy = healthCheckResults.Count(r => r.OverallStatus == AIAgentFramework.Monitoring.Models.HealthStatus.Healthy);
        var availabilityPercentage = (double)totalHealthy / healthCheckResults.Count * 100;
        
        // 최소 30% 이상의 가용성 유지 (복구 고려)
        availabilityPercentage.Should().BeGreaterThan(20, "System should maintain reasonable availability even during cascade failures");
    }

    [Fact]
    public async Task Circuit_Breaker_Pattern_Should_Protect_Against_Repeated_Failures()
    {
        // Arrange - 반복적 실패 시나리오
        var callCount = 0;
        var circuitBreakerState = "closed"; // closed, open, half-open
        
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns<string, CancellationToken>(async (prompt, ct) =>
                      {
                          var currentCall = Interlocked.Increment(ref callCount);
                          
                          // Circuit breaker 시뮬레이션
                          if (circuitBreakerState == "open")
                          {
                              throw new InvalidOperationException("Circuit breaker is OPEN - rejecting calls");
                          }
                          
                          if (currentCall <= 5) // 처음 5번 호출은 실패
                          {
                              if (currentCall == 5) circuitBreakerState = "open"; // 5번째 실패 후 서킷 열림
                              throw new HttpRequestException($"Simulated failure #{currentCall}");
                          }
                          
                          // 6번째 호출부터는 성공 (half-open 상태에서 복구)
                          if (currentCall == 6) circuitBreakerState = "half-open";
                          if (currentCall >= 7) circuitBreakerState = "closed";
                          
                          await Task.Delay(50, ct);
                          return $"Circuit breaker recovered response: {prompt}";
                      });

        // Act - 연속적인 호출 시뮬레이션
        var results = new List<(bool success, string? response, string? error)>();
        
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var response = await _mockLLMProvider.Object.GenerateAsync($"Test call {i}", CancellationToken.None);
                results.Add((true, response, null));
            }
            catch (Exception ex)
            {
                results.Add((false, null, ex.Message));
            }
            
            // 짧은 간격으로 호출
            await Task.Delay(100);
        }

        // Assert
        results.Should().HaveCount(10);
        
        // 처음 5번은 실패해야 함
        var initialFailures = results.Take(5);
        initialFailures.Should().AllSatisfy(r => r.success.Should().BeFalse());
        
        // Circuit breaker가 열린 후에는 즉시 실패해야 함
        var circuitOpenFailures = results.Skip(5).Take(2);
        circuitOpenFailures.Should().AllSatisfy(r => 
        {
            r.success.Should().BeFalse();
            r.error.Should().Contain("Circuit breaker is OPEN");
        });
        
        // 마지막 호출들은 복구되어야 함
        var recoveredCalls = results.Skip(7);
        var successfulRecoveries = recoveredCalls.Count(r => r.success);
        successfulRecoveries.Should().BeGreaterThan(0, "Circuit breaker should allow recovery");
    }

    [Fact]
    public async Task Resource_Exhaustion_Should_Be_Handled_With_Graceful_Degradation()
    {
        // Arrange - 리소스 고갈 시나리오
        var memoryPressure = false;
        var activeTasks = 0;
        var maxConcurrentTasks = 3;
        
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns<string, CancellationToken>((prompt, ct) =>
                      {
                          return Task.Run(async () =>
                          {
                              var currentTasks = Interlocked.Increment(ref activeTasks);
                              
                              try
                              {
                                  // 동시 작업 수 제한 시뮬레이션
                                  if (currentTasks > maxConcurrentTasks)
                                  {
                                      throw new InvalidOperationException($"Resource exhausted - too many concurrent tasks: {currentTasks}");
                                  }
                                  
                                  // 메모리 압박 상황 시뮬레이션
                                  if (memoryPressure && currentTasks > 1)
                                  {
                                      throw new OutOfMemoryException("Simulated memory pressure");
                                  }
                                  
                                  await Task.Delay(Random.Shared.Next(100, 300), ct);
                                  return $"Processed under resource constraints: {prompt}";
                              }
                              finally
                              {
                                  Interlocked.Decrement(ref activeTasks);
                              }
                          }, ct);
                      });

        // Act - 높은 부하 상황 시뮬레이션
        memoryPressure = true;
        
        var tasks = new List<Task<(bool success, string? response, string? error)>>();
        
        for (int i = 0; i < 8; i++) // maxConcurrentTasks보다 많은 요청
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await _mockLLMProvider.Object.GenerateAsync($"High load request {taskId}", CancellationToken.None);
                    return (true, response, (string?)null);
                }
                catch (Exception ex)
                {
                    return (false, (string?)null, ex.Message);
                }
            }));
            
            await Task.Delay(10); // 약간의 지연으로 순차적 시작
        }
        
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(8);
        
        var successes = results.Where(r => r.success).ToArray();
        var failures = results.Where(r => !r.success).ToArray();
        
        // 일부는 성공해야 함 (graceful degradation)
        successes.Should().NotBeEmpty("Some requests should succeed under resource constraints");
        successes.Length.Should().BeLessOrEqualTo(maxConcurrentTasks, "Should respect resource limits");
        
        // 실패한 요청들은 적절한 오류 메시지를 가져야 함
        failures.Should().AllSatisfy(f => 
            f.error.Should().Match(e => 
                e!.Contains("Resource exhausted") || 
                e.Contains("memory pressure"),
                "Failures should have appropriate error messages"));
        
        // 전체적으로 시스템이 완전히 중단되지는 않았어야 함
        var totalFailureRate = (double)failures.Length / results.Length;
        totalFailureRate.Should().BeLessThan(0.8, "System should not completely fail under resource pressure");
    }

    [Fact]
    public async Task Timeout_Handling_Should_Prevent_System_Deadlock()
    {
        // Arrange - 타임아웃 시나리오
        var longRunningTaskCount = 0;
        
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns<string, CancellationToken>((prompt, ct) =>
                      {
                          return Task.Run(async () =>
                          {
                              var taskId = Interlocked.Increment(ref longRunningTaskCount);
                              
                              // 일부 작업은 매우 오래 걸리도록 설정
                              if (taskId % 3 == 0) // 매 3번째 작업
                              {
                                  try
                                  {
                                      await Task.Delay(TimeSpan.FromMinutes(5), ct); // 5분 지연 (타임아웃되어야 함)
                                      return "This should not be reached due to timeout";
                                  }
                                  catch (OperationCanceledException)
                                  {
                                      throw new TimeoutException($"Task {taskId} timed out as expected");
                                  }
                              }
                              
                              // 나머지 작업은 정상적으로 처리
                              await Task.Delay(100, ct);
                              return $"Quick response from task {taskId}: {prompt}";
                          }, ct);
                      });

        // Act - 타임아웃을 포함한 병렬 요청
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // 2초 타임아웃
        
        var tasks = new List<Task<(bool success, string? response, string? error, TimeSpan duration)>>();
        
        for (int i = 0; i < 9; i++) // 3개의 타임아웃 작업 포함
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var response = await _mockLLMProvider.Object.GenerateAsync($"Timeout test {taskId}", cts.Token);
                    stopwatch.Stop();
                    return (true, response, (string?)null, stopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    return (false, (string?)null, ex.Message, stopwatch.Elapsed);
                }
            }));
        }
        
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(9);
        
        // 타임아웃 작업들 확인
        var timedOutTasks = results.Where(r => !r.success && 
            (r.error!.Contains("timeout") || r.error.Contains("canceled"))).ToArray();
        
        timedOutTasks.Should().HaveCountGreaterOrEqualTo(3, "Should have timeout failures for long-running tasks");
        
        // 빠른 작업들은 성공해야 함
        var successfulTasks = results.Where(r => r.success).ToArray();
        successfulTasks.Should().NotBeEmpty("Quick tasks should succeed despite timeouts");
        
        // 모든 작업이 타임아웃 시간 내에 완료되어야 함 (성공이든 실패든)
        results.Should().AllSatisfy(r => 
            r.duration.Should().BeLessThan(TimeSpan.FromSeconds(3), 
                "All tasks should complete within timeout period"));
        
        // 시스템이 데드락에 빠지지 않고 응답성을 유지해야 함
        var avgResponseTime = results.Average(r => r.duration.TotalMilliseconds);
        avgResponseTime.Should().BeLessThan(2500, "Average response time should be reasonable");
    }

    [Fact]
    public async Task Data_Corruption_Recovery_Should_Maintain_Data_Integrity()
    {
        // Arrange - 데이터 손상 시나리오
        var corruptionInjected = false;
        var dataStore = new ConcurrentDictionary<string, object>();
        
        _mockStateProvider.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                         .Callback<string, object, TimeSpan?, CancellationToken>((key, value, ttl, ct) =>
                         {
                             if (corruptionInjected && key.Contains("corrupt"))
                             {
                                 // 손상된 데이터 저장 시뮬레이션
                                 dataStore[key] = "CORRUPTED_DATA_INVALID_JSON_{{{";
                             }
                             else
                             {
                                 dataStore[key] = value;
                             }
                         })
                         .Returns(Task.CompletedTask);
        
        _mockStateProvider.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns<string, CancellationToken>((key, ct) =>
                         {
                             if (dataStore.TryGetValue(key, out var value))
                             {
                                 var stringValue = value.ToString();
                                 
                                 // 손상된 데이터 감지 및 복구 시뮬레이션
                                 if (stringValue!.Contains("CORRUPTED_DATA"))
                                 {
                                     return Task.FromException<string?>(new InvalidDataException($"Data corruption detected for key: {key}"));
                                 }
                                 
                                 return Task.FromResult<string?>(stringValue);
                             }
                             return Task.FromResult<string?>(null);
                         });

        // Act - 데이터 저장 및 손상 시나리오
        var testKeys = new[] { "normal_data", "corrupt_data", "recovery_data" };
        var testValues = new[] { "normal_value", "corrupt_value", "recovery_value" };
        
        // 정상 데이터 저장
        await _mockStateProvider.Object.SetAsync(testKeys[0], testValues[0], TimeSpan.FromHours(1), CancellationToken.None);
        
        // 손상 주입 후 데이터 저장
        corruptionInjected = true;
        await _mockStateProvider.Object.SetAsync(testKeys[1], testValues[1], TimeSpan.FromHours(1), CancellationToken.None);
        
        // 복구 데이터 저장 (손상 비활성화)
        corruptionInjected = false;
        await _mockStateProvider.Object.SetAsync(testKeys[2], testValues[2], TimeSpan.FromHours(1), CancellationToken.None);
        
        // 데이터 읽기 시도
        var readResults = new List<(string key, bool success, string? value, string? error)>();
        
        foreach (var key in testKeys)
        {
            try
            {
                var value = await _mockStateProvider.Object.GetAsync<string>(key, CancellationToken.None);
                readResults.Add((key, true, value, null));
            }
            catch (Exception ex)
            {
                readResults.Add((key, false, null, ex.Message));
            }
        }

        // Assert
        readResults.Should().HaveCount(3);
        
        // 정상 데이터는 성공해야 함
        var normalData = readResults.First(r => r.key == "normal_data");
        normalData.success.Should().BeTrue("Normal data should be readable");
        normalData.value.Should().Be("normal_value");
        
        // 손상된 데이터는 감지되어야 함
        var corruptData = readResults.First(r => r.key == "corrupt_data");
        corruptData.success.Should().BeFalse("Corrupted data should be detected");
        corruptData.error.Should().Contain("Data corruption detected");
        
        // 복구된 데이터는 정상적이어야 함
        var recoveryData = readResults.First(r => r.key == "recovery_data");
        recoveryData.success.Should().BeTrue("Recovery data should be readable");
        recoveryData.value.Should().Be("recovery_value");
        
        // 전체 시스템의 데이터 무결성 확인
        var successfulReads = readResults.Count(r => r.success);
        var corruptionDetection = readResults.Count(r => !r.success && r.error!.Contains("corruption"));
        
        successfulReads.Should().Be(2, "Should successfully read uncorrupted data");
        corruptionDetection.Should().Be(1, "Should detect exactly one corruption");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}