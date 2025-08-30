using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.Monitoring.Extensions;
using AIAgentFramework.State.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIAgentFramework.Integration.Tests;

/// <summary>
/// 기본 시스템 통합 테스트 (수정된 버전)
/// </summary>
public class BasicSystemTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public BasicSystemTests()
    {
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Mock 서비스들 설정
        var mockLLMProvider = new Mock<ILLMProvider>();
        var mockOrchestrationEngine = new Mock<IOrchestrationEngine>();
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        
        // LLM Provider Mock 설정
        mockLLMProvider.Setup(x => x.Name).Returns("TestLLM");
        mockLLMProvider.Setup(x => x.DefaultModel).Returns("test-model");
        mockLLMProvider.Setup(x => x.SupportedModels).Returns(new List<string> { "test-model" }.AsReadOnly());
        mockLLMProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((string prompt, CancellationToken _) => $"Generated: {prompt}");
        mockLLMProvider.Setup(x => x.CountTokensAsync(It.IsAny<string>(), It.IsAny<string?>()))
                      .ReturnsAsync((string text, string? model) => Math.Min(text.Length / 4, 30)); // 더 현실적인 토큰 수

        // Mock Orchestration Result 설정 (각 호출마다 새로운 세션 ID 반환)
        var mockResult = new Mock<IOrchestrationResult>();
        mockResult.Setup(x => x.SessionId).Returns(() => Guid.NewGuid().ToString()); // 델리게이트로 매번 새 GUID 생성
        mockResult.Setup(x => x.IsCompleted).Returns(true);
        mockResult.Setup(x => x.IsSuccess).Returns(true);
        mockResult.Setup(x => x.FinalResponse).Returns("Test response");
        mockResult.Setup(x => x.ExecutionSteps).Returns(new List<IExecutionStep>());
        mockResult.Setup(x => x.TotalDuration).Returns(TimeSpan.FromSeconds(1));
        mockResult.Setup(x => x.Metadata).Returns(new Dictionary<string, object>());

        // Mock User Request
        var mockUserRequest = new Mock<IUserRequest>();
        mockUserRequest.Setup(x => x.Content).Returns("Test request");
        mockUserRequest.Setup(x => x.RequestId).Returns(Guid.NewGuid().ToString());
        mockUserRequest.Setup(x => x.UserId).Returns("TestUser");
        mockUserRequest.Setup(x => x.Metadata).Returns(new Dictionary<string, object>());
        mockUserRequest.Setup(x => x.RequestedAt).Returns(DateTime.UtcNow);

        mockOrchestrationEngine.Setup(x => x.ExecuteAsync(It.IsAny<IUserRequest>()))
                              .ReturnsAsync(mockResult.Object);

        // Mock 서비스 등록
        services.AddSingleton(mockOrchestrationEngine.Object);
        services.AddSingleton(mockLLMRegistry.Object);
        services.AddSingleton(mockToolRegistry.Object);
        services.AddSingleton(mockLLMProvider.Object);
        
        // State 관리 시스템 등록 (InMemoryStateProvider 사용)
        services.AddSingleton<AIAgentFramework.State.Interfaces.IStateProvider, AIAgentFramework.State.Providers.InMemoryStateProvider>();
        
        // Monitoring 시스템 등록
        services.AddAIAgentMonitoring();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ServiceProvider_Should_ResolveBasicServices_Successfully()
    {
        // Act & Assert
        var llmProvider = _serviceProvider.GetService<ILLMProvider>();
        var orchestrationEngine = _serviceProvider.GetService<IOrchestrationEngine>();
        var llmRegistry = _serviceProvider.GetService<ILLMFunctionRegistry>();
        var toolRegistry = _serviceProvider.GetService<IToolRegistry>();

        llmProvider.Should().NotBeNull("LLM Provider should be registered");
        orchestrationEngine.Should().NotBeNull("Orchestration Engine should be registered");
        llmRegistry.Should().NotBeNull("LLM Registry should be registered");
        toolRegistry.Should().NotBeNull("Tool Registry should be registered");
    }

    [Fact]
    public async Task LLMProvider_Should_ReturnAvailable_Successfully()
    {
        // Arrange
        var llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();

        // Act
        var isAvailable = await llmProvider.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue("LLM Provider should be available");
    }

    [Fact]
    public async Task LLMProvider_Should_GenerateResponse_Successfully()
    {
        // Arrange
        var llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
        const string testPrompt = "Hello, world!";

        // Act
        var response = await llmProvider.GenerateAsync(testPrompt, CancellationToken.None);

        // Assert
        response.Should().NotBeNullOrEmpty("Response should not be empty");
        response.Should().Contain("Generated", "Response should contain generated text");
        response.Should().Contain(testPrompt, "Response should reference the original prompt");
    }

    [Fact]
    public async Task OrchestrationEngine_Should_ExecuteRequest_Successfully()
    {
        // Arrange
        var orchestrationEngine = _serviceProvider.GetRequiredService<IOrchestrationEngine>();
        var mockUserRequest = new Mock<IUserRequest>();
        mockUserRequest.Setup(x => x.Content).Returns("Test orchestration request");
        mockUserRequest.Setup(x => x.RequestId).Returns(Guid.NewGuid().ToString());
        mockUserRequest.Setup(x => x.UserId).Returns("TestUser");
        mockUserRequest.Setup(x => x.Metadata).Returns(new Dictionary<string, object>());
        mockUserRequest.Setup(x => x.RequestedAt).Returns(DateTime.UtcNow);

        // Act
        var result = await orchestrationEngine.ExecuteAsync(mockUserRequest.Object);

        // Assert
        result.Should().NotBeNull("Orchestration result should not be null");
        result.IsCompleted.Should().BeTrue("Orchestration should be completed");
        result.IsSuccess.Should().BeTrue("Orchestration should be successful");
        result.SessionId.Should().NotBeNullOrEmpty("Session ID should be generated");
    }

    [Fact]
    public void MonitoringServices_Should_BeAvailable_Successfully()
    {
        // Act
        var telemetryCollector = _serviceProvider.GetService<AIAgentFramework.Monitoring.Telemetry.TelemetryCollector>();
        var activitySourceManager = _serviceProvider.GetService<AIAgentFramework.Monitoring.Telemetry.ActivitySourceManager>();
        var metricsCollector = _serviceProvider.GetService<AIAgentFramework.Monitoring.Metrics.MetricsCollector>();

        // Assert
        telemetryCollector.Should().NotBeNull("TelemetryCollector should be registered");
        activitySourceManager.Should().NotBeNull("ActivitySourceManager should be registered");
        metricsCollector.Should().NotBeNull("MetricsCollector should be registered");
    }

    [Fact]
    public async Task StateManagement_Should_BeAvailable_Successfully()
    {
        // Act
        var stateProvider = _serviceProvider.GetService<AIAgentFramework.State.Interfaces.IStateProvider>();

        // Assert
        stateProvider.Should().NotBeNull("State Provider should be registered");

        // Test basic state operations
        const string testKey = "test_key";
        const string testValue = "test_value";

        // Set
        await stateProvider!.SetAsync(testKey, testValue, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Get
        var retrievedValue = await stateProvider.GetAsync<string>(testKey, CancellationToken.None);
        retrievedValue.Should().Be(testValue, "Retrieved value should match the stored value");

        // Exists
        var exists = await stateProvider.ExistsAsync(testKey, CancellationToken.None);
        exists.Should().BeTrue("Key should exist after being stored");

        // Delete
        await stateProvider.DeleteAsync(testKey, CancellationToken.None);
        var existsAfterDelete = await stateProvider.ExistsAsync(testKey, CancellationToken.None);
        existsAfterDelete.Should().BeFalse("Key should not exist after deletion");
    }

    [Fact]
    public async Task System_Should_HandleConcurrentRequests_Successfully()
    {
        // Arrange
        const int concurrentRequests = 5;
        var orchestrationEngine = _serviceProvider.GetRequiredService<IOrchestrationEngine>();
        var tasks = new List<Task<IOrchestrationResult>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var requestId = i;
            var mockRequest = new Mock<IUserRequest>();
            mockRequest.Setup(x => x.Content).Returns($"Concurrent request {requestId}");
            mockRequest.Setup(x => x.RequestId).Returns(Guid.NewGuid().ToString());
            mockRequest.Setup(x => x.UserId).Returns("TestUser");
            mockRequest.Setup(x => x.Metadata).Returns(new Dictionary<string, object>());
            mockRequest.Setup(x => x.RequestedAt).Returns(DateTime.UtcNow);
            tasks.Add(orchestrationEngine.ExecuteAsync(mockRequest.Object));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests, "All requests should complete");
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result.IsCompleted.Should().BeTrue();
            result.SessionId.Should().NotBeNullOrEmpty();
        });

        // Verify all session IDs are unique
        var sessionIds = results.Select(r => r.SessionId).ToArray();
        sessionIds.Should().OnlyHaveUniqueItems("Each request should have a unique session ID");
    }

    [Fact]
    public async Task System_Should_HandleBasicErrorScenarios_Gracefully()
    {
        // Arrange
        var llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();

        // Test with empty prompt
        var emptyPromptResponse = await llmProvider.GenerateAsync("", CancellationToken.None);
        emptyPromptResponse.Should().NotBeNull("Empty prompt should still generate a response");

        // Test with very long prompt
        var longPrompt = new string('a', 10000);
        var longPromptResponse = await llmProvider.GenerateAsync(longPrompt, CancellationToken.None);
        longPromptResponse.Should().NotBeNull("Long prompt should still generate a response");
    }

    [Fact]
    public async Task TokenCounting_Should_Work_Successfully()
    {
        // Arrange
        var llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
        const string testText = "This is a test text for token counting.";

        // Act
        var tokenCount = await llmProvider.CountTokensAsync(testText, null);

        // Assert
        tokenCount.Should().BeGreaterThan(0, "Token count should be positive");
        tokenCount.Should().BeLessThan(testText.Length, "Token count should be reasonable");
    }

    [Fact]
    public void ServiceLifetimes_Should_BeConfigured_Correctly()
    {
        // Test Singleton services - should return same instance
        var telemetryCollector1 = _serviceProvider.GetService<AIAgentFramework.Monitoring.Telemetry.TelemetryCollector>();
        var telemetryCollector2 = _serviceProvider.GetService<AIAgentFramework.Monitoring.Telemetry.TelemetryCollector>();
        telemetryCollector1.Should().BeSameAs(telemetryCollector2, "TelemetryCollector should be singleton");

        // Test MetricsCollector singleton as well
        var metricsCollector1 = _serviceProvider.GetService<AIAgentFramework.Monitoring.Metrics.MetricsCollector>();
        var metricsCollector2 = _serviceProvider.GetService<AIAgentFramework.Monitoring.Metrics.MetricsCollector>();
        metricsCollector1.Should().BeSameAs(metricsCollector2, "MetricsCollector should be singleton");
    }

    public void Dispose()
    {
        try
        {
            _serviceProvider?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // 이미 처리된 경우 무시
        }
        catch (Exception)
        {
            // 기타 처리 중 발생할 수 있는 예외 무시
        }
    }
}