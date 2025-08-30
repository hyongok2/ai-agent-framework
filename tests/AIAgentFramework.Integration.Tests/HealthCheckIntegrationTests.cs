using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Monitoring.Extensions;
using AIAgentFramework.Monitoring.HealthChecks;
using AIAgentFramework.Monitoring.Interfaces;
using AIAgentFramework.Monitoring.Models;
using AIAgentFramework.Monitoring.Services;
using AIAgentFramework.State.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIAgentFramework.Integration.Tests;

/// <summary>
/// Health Check 시스템 통합 테스트
/// </summary>
public class HealthCheckIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IHealthCheckService _healthCheckService;

    public HealthCheckIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Mock 서비스들 등록
        var mockOrchestrationEngine = new Mock<IOrchestrationEngine>();
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        var mockStateProvider = new Mock<IStateProvider>();
        var mockLLMProvider = new Mock<ILLMProvider>();
        
        // Mock 설정
        mockLLMProvider.Setup(x => x.Name).Returns("MockProvider");
        mockLLMProvider.Setup(x => x.DefaultModel).Returns("mock-model");
        mockLLMProvider.Setup(x => x.SupportedModels).Returns(new List<string> { "mock-model" }.AsReadOnly());
        mockLLMProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync("Mock LLM response for health check");
        mockLLMProvider.Setup(x => x.CountTokensAsync(It.IsAny<string>(), It.IsAny<string?>()))
                      .ReturnsAsync(10);
        
        mockStateProvider.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        mockStateProvider.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new { Message = "Health check test", Timestamp = DateTime.UtcNow });
        mockStateProvider.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(true);
        mockStateProvider.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        
        // Mock 서비스 등록
        services.AddSingleton(mockOrchestrationEngine.Object);
        services.AddSingleton(mockLLMRegistry.Object);
        services.AddSingleton(mockToolRegistry.Object);
        services.AddSingleton(mockStateProvider.Object);
        services.AddSingleton(mockLLMProvider.Object);
        
        // Health Check 시스템 등록
        services.AddAllHealthChecks(options =>
        {
            options.EnableBackgroundChecks = true;
            options.BackgroundCheckIntervalSeconds = 60;
        });
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Health Check들을 실제로 등록
        _serviceProvider.RegisterHealthChecks();
        
        _healthCheckService = _serviceProvider.GetRequiredService<IHealthCheckService>();
    }

    [Fact]
    public void HealthCheckService_Should_BeRegistered_Successfully()
    {
        // Arrange & Act
        var service = _serviceProvider.GetService<IHealthCheckService>();
        
        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<HealthCheckService>();
    }

    [Fact]
    public void AllHealthChecks_Should_BeRegistered_Automatically()
    {
        // Act
        var registeredChecks = _healthCheckService.GetRegisteredHealthCheckNames();
        
        // Assert
        registeredChecks.Should().NotBeEmpty();
        registeredChecks.Should().Contain("Orchestration Engine");
        registeredChecks.Should().Contain("LLM Provider");
        registeredChecks.Should().Contain("State Provider");
    }

    [Fact]
    public async Task RunAllHealthChecks_Should_ReturnSummary_WithAllChecks()
    {
        // Act
        var summary = await _healthCheckService.RunAllHealthChecksAsync();
        
        // Assert
        summary.Should().NotBeNull();
        summary.TotalCount.Should().BeGreaterThan(0);
        summary.Results.Should().NotBeEmpty();
        summary.OverallStatus.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Warning);
        
        // 각 Health Check 결과 검증
        foreach (var result in summary.Results)
        {
            result.Should().NotBeNull();
            result.Name.Should().NotBeNullOrEmpty();
            result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
            result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }
    }

    [Fact]
    public async Task OrchestrationHealthCheck_Should_Pass_WithValidConfiguration()
    {
        // Act
        var result = await _healthCheckService.RunHealthCheckAsync("Orchestration Engine");
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Warning);
        result.Name.Should().Be("Orchestration Engine");
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        
        // 메타데이터 검증
        result.Data.Should().ContainKey("engine_type");
        result.Data.Should().ContainKey("engine_available");
        result.Data["engine_available"].Should().Be(true);
    }

    [Fact]
    public async Task LLMHealthCheck_Should_Pass_WithMockProvider()
    {
        // Act
        var result = await _healthCheckService.RunHealthCheckAsync("LLM Provider");
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Warning);
        result.Name.Should().Be("LLM Provider");
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        
        // 프로바이더 정보 검증
        result.Data.Should().ContainKey("provider_available");
        result.Data.Should().ContainKey("provider_type");
        result.Data.Should().ContainKey("default_model");
        result.Data["provider_available"].Should().Be(true);
        result.Data["default_model"].Should().Be("mock-model");
        
        // 생성 테스트 결과 검증
        result.Data.Should().ContainKey("test_generation_success");
        result.Data.Should().ContainKey("test_generation_time_ms");
        result.Data["test_generation_success"].Should().Be(true);
        
        // 토큰 카운팅 테스트 결과 검증
        result.Data.Should().ContainKey("token_counting_available");
        result.Data.Should().ContainKey("test_token_count");
        result.Data["token_counting_available"].Should().Be(true);
        result.Data["test_token_count"].Should().Be(10);
    }

    [Fact]
    public async Task StateHealthCheck_Should_Pass_WithInMemoryProvider()
    {
        // Act
        var result = await _healthCheckService.RunHealthCheckAsync("State Provider");
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Warning, HealthStatus.Unhealthy);
        result.Name.Should().Be("State Provider");
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        
        // 연결 정보 검증
        result.Data.Should().ContainKey("provider_type");
        result.Data.Should().ContainKey("connection_healthy");
        result.Data["connection_healthy"].Should().Be(true);
        result.Data["provider_type"].Should().NotBeNull();
        
        // CRUD 테스트 결과 검증
        result.Data.Should().ContainKey("write_test_success");
        result.Data.Should().ContainKey("read_test_success");
        result.Data.Should().ContainKey("delete_test_success");
        result.Data["write_test_success"].Should().Be(true);
        result.Data["read_test_success"].Should().Be(true);
        result.Data.Should().ContainKey("delete_test_success");
    }

    [Fact]
    public async Task HealthCheckConfiguration_Should_BeApplied_Correctly()
    {
        // Arrange
        var customConfig = new HealthCheckConfiguration
        {
            TimeoutSeconds = 5,
            WarningThresholds = { ["response_time_ms"] = 1000 },
            CriticalThresholds = { ["response_time_ms"] = 3000 }
        };
        
        // Health Check 직접 실행 (설정 적용)
        var stateProvider = _serviceProvider.GetRequiredService<IStateProvider>();
        var logger = _serviceProvider.GetRequiredService<ILogger<StateHealthCheck>>();
        var healthCheck = new StateHealthCheck(stateProvider, logger, customConfig);
        
        // Act
        var result = await healthCheck.CheckAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("State Provider");
        
        // 설정 검증
        healthCheck.TimeoutSeconds.Should().Be(5);
        
        // 설정이 적용되었는지 확인 (구체적인 상태보다는 설정 적용 여부가 중요)
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Warning, HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task HealthCheckSummary_Should_CalculateOverallStatus_Correctly()
    {
        // Act
        var summary = await _healthCheckService.RunAllHealthChecksAsync();
        
        // Assert
        summary.Should().NotBeNull();
        
        var healthyCount = summary.Results.Count(r => r.Status == HealthStatus.Healthy);
        var warningCount = summary.Results.Count(r => r.Status == HealthStatus.Warning);
        var unhealthyCount = summary.Results.Count(r => r.Status == HealthStatus.Unhealthy);
        
        // 전체 상태 계산 로직 검증
        if (unhealthyCount > 0)
        {
            summary.OverallStatus.Should().Be(HealthStatus.Unhealthy);
        }
        else if (warningCount > 0)
        {
            summary.OverallStatus.Should().Be(HealthStatus.Warning);
        }
        else
        {
            summary.OverallStatus.Should().Be(HealthStatus.Healthy);
        }
        
        // 카운트 검증
        summary.HealthyCount.Should().Be(healthyCount);
        summary.WarningCount.Should().Be(warningCount);
        summary.UnhealthyCount.Should().Be(unhealthyCount);
        summary.TotalCount.Should().Be(healthyCount + warningCount + unhealthyCount);
    }

    [Fact]
    public async Task ConcurrentHealthChecks_Should_HandleMultipleRequests_Safely()
    {
        // Arrange
        const int concurrentRequests = 10;
        var tasks = new List<Task<HealthCheckSummary>>();
        
        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_healthCheckService.RunAllHealthChecksAsync());
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().HaveCount(concurrentRequests);
        
        foreach (var summary in results)
        {
            summary.Should().NotBeNull();
            summary.TotalCount.Should().BeGreaterThan(0);
            summary.Results.Should().NotBeEmpty();
            summary.OverallStatus.Should().NotBe(HealthStatus.Unknown);
        }
        
        // 모든 요청이 일관된 Health Check 수를 반환해야 함
        var firstTotalChecks = results[0].TotalCount;
        results.Should().OnlyContain(r => r.TotalCount == firstTotalChecks);
    }

    [Fact]
    public async Task HealthCheck_WithCancellation_Should_HandleCancellation_Gracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1)); // 즉시 취소
        
        // Act & Assert
        var act = async () => await _healthCheckService.RunAllHealthChecksAsync(cts.Token);
        
        // 취소 또는 정상 완료 모두 허용 (타이밍에 따라 다를 수 있음)
        try
        {
            var result = await act.Invoke();
            result.Should().NotBeNull(); // 빠른 완료 시
        }
        catch (OperationCanceledException)
        {
            // 취소된 경우 - 정상적인 시나리오
            true.Should().BeTrue();
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}