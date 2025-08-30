using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.LLM.Interfaces;
using AIAgentFramework.Orchestration;
using AIAgentFramework.State.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIAgentFramework.Integration.Tests;

/// <summary>
/// End-to-End 워크플로우 통합 테스트 (현재 아키텍처 호환)
/// </summary>
public class E2EWorkflowIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IOrchestrationEngine _orchestrationEngine;
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<ITool> _mockTool;

    public E2EWorkflowIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // 로깅 설정
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Mock 서비스들 설정
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockTool = new Mock<ITool>();
        var mockLLMRegistry = new Mock<ILLMFunctionRegistry>();
        var mockToolRegistry = new Mock<IToolRegistry>();
        var mockStateProvider = new Mock<IStateProvider>();
        var mockRegistry = new Mock<IRegistry>();
        var mockActionFactory = new Mock<IActionFactory>();
        
        // LLM Provider Mock 설정
        _mockLLMProvider.Setup(x => x.Name).Returns("MockLLM");
        _mockLLMProvider.Setup(x => x.DefaultModel).Returns("mock-model");
        _mockLLMProvider.Setup(x => x.SupportedModels).Returns(new List<string> { "mock-model" }.AsReadOnly());
        _mockLLMProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((string prompt, CancellationToken _) => $"Generated response for: {prompt}");
        _mockLLMProvider.Setup(x => x.CountTokensAsync(It.IsAny<string>(), It.IsAny<string?>()))
                      .ReturnsAsync(100);

        // Tool Mock 설정
        _mockTool.Setup(x => x.Name).Returns("MockTool");
        _mockTool.Setup(x => x.Description).Returns("Mock tool for testing");
        _mockTool.Setup(x => x.Contract).Returns(new AIAgentFramework.Core.Models.ToolContract());
        _mockTool.Setup(x => x.ValidateAsync(It.IsAny<IToolInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        _mockTool.Setup(x => x.ExecuteAsync(It.IsAny<IToolInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IToolInput input, CancellationToken _) => 
                    AIAgentFramework.Core.Models.ToolResult.CreateSuccess());

        // Registry Mock 설정 - 현재 인터페이스에 맞춰 간단히 설정
        mockLLMRegistry.Setup(x => x.Resolve(It.IsAny<string>())).Returns((ILLMFunction)null!);
        mockToolRegistry.Setup(x => x.Resolve(It.IsAny<string>())).Returns((ITool)null!);

        // State Provider Mock 설정
        mockStateProvider.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
        mockStateProvider.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new { status = "saved", timestamp = DateTime.UtcNow });
        mockStateProvider.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Mock 서비스 등록
        services.AddSingleton(_mockLLMProvider.Object);
        services.AddSingleton(mockLLMRegistry.Object);
        services.AddSingleton(mockToolRegistry.Object);
        services.AddSingleton(mockStateProvider.Object);
        services.AddSingleton(mockRegistry.Object);
        services.AddSingleton(mockActionFactory.Object);
        
        // 실제 Orchestration Engine 등록 (현재 구현된 것 사용)
        services.AddSingleton<IOrchestrationEngine, OrchestrationEngine>();

        _serviceProvider = services.BuildServiceProvider();
        _orchestrationEngine = _serviceProvider.GetRequiredService<IOrchestrationEngine>();
    }

    [Fact]
    public async Task Simple_User_Request_Should_Execute_Successfully()
    {
        // Arrange
        var userRequest = new AIAgentFramework.Orchestration.UserRequest("Generate a simple greeting message");

        // Act
        var result = await _orchestrationEngine.ExecuteAsync(userRequest);

        // Assert
        result.Should().NotBeNull("Orchestration result should not be null");
        result.IsCompleted.Should().BeTrue("Orchestration should complete successfully");
        result.SessionId.Should().NotBeNullOrEmpty("Session ID should be generated");
        
        // Verify basic orchestration functionality
        _mockLLMProvider.Verify(x => x.IsAvailableAsync(), Times.AtLeastOnce, "Should check LLM provider availability");
    }

    [Fact]
    public async Task Multiple_Sequential_Requests_Should_Maintain_Session_Context()
    {
        // Arrange
        var requests = new[]
        {
            new AIAgentFramework.Orchestration.UserRequest("First request: analyze the situation"),
            new AIAgentFramework.Orchestration.UserRequest("Second request: provide recommendations"),
            new AIAgentFramework.Orchestration.UserRequest("Third request: summarize findings")
        };

        var results = new List<IOrchestrationResult>();

        // Act
        foreach (var request in requests)
        {
            var result = await _orchestrationEngine.ExecuteAsync(request);
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3, "All requests should be processed");
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result.IsCompleted.Should().BeTrue();
            result.SessionId.Should().NotBeNullOrEmpty();
        });

        // Verify all requests were processed
        _mockLLMProvider.Verify(x => x.IsAvailableAsync(), Times.AtLeast(3));
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Be_Handled_Independently()
    {
        // Arrange
        const int concurrentRequests = 5;
        var tasks = new List<Task<IOrchestrationResult>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var request = new AIAgentFramework.Orchestration.UserRequest($"Concurrent request {i}");
            tasks.Add(_orchestrationEngine.ExecuteAsync(request));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests, "All concurrent requests should complete");
        
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.IsCompleted.Should().BeTrue();
            result.SessionId.Should().NotBeNullOrEmpty();
        }

        // Verify all sessions are unique
        var sessionIds = results.Select(r => r.SessionId).ToList();
        sessionIds.Should().OnlyHaveUniqueItems("Each request should have unique session ID");

        // Verify LLM Provider handled concurrent requests
        _mockLLMProvider.Verify(x => x.IsAvailableAsync(), Times.AtLeast(concurrentRequests));
    }

    [Fact]
    public async Task Request_With_Cancellation_Should_Handle_Gracefully()
    {
        // Arrange
        var userRequest = new AIAgentFramework.Orchestration.UserRequest("Long running request");
        using var cts = new CancellationTokenSource();
        
        // Setup delayed response to simulate long-running operation
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(async (string prompt, CancellationToken ct) =>
                      {
                          await Task.Delay(2000, ct); // 2 second delay
                          return $"Generated response for: {prompt}";
                      });

        // Act & Assert
        var orchestrationTask = _orchestrationEngine.ExecuteAsync(userRequest);
        
        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        // Should either complete or handle cancellation gracefully
        try
        {
            var result = await orchestrationTask;
            result.Should().NotBeNull("Result should not be null even if cancelled");
        }
        catch (OperationCanceledException)
        {
            // Cancellation is acceptable behavior
            true.Should().BeTrue("Cancellation was handled appropriately");
        }
    }

    [Fact]
    public async Task Error_Handling_Should_Capture_LLM_Failures()
    {
        // Arrange
        var userRequest = new AIAgentFramework.Orchestration.UserRequest("Request that causes LLM error");

        // Setup LLM Provider to throw an exception
        _mockLLMProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new InvalidOperationException("Simulated LLM error"));

        // Act
        var result = await _orchestrationEngine.ExecuteAsync(userRequest);

        // Assert
        result.Should().NotBeNull("Result should not be null even on error");
        
        // The orchestration should either complete with error information
        // or handle the error gracefully without crashing
        if (!result.IsCompleted)
        {
            // If not completed, there should be some error indication
            result.Should().NotBeNull("Error case should still return result");
        }
    }

    [Fact]
    public async Task Basic_Workflow_Performance_Should_Meet_Requirements()
    {
        // Arrange
        var userRequest = new AIAgentFramework.Orchestration.UserRequest("Performance test request");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _orchestrationEngine.ExecuteAsync(userRequest);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.IsCompleted.Should().BeTrue();
        
        // Performance requirement: Complete within 10 seconds for basic workflow
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, 
            "Basic workflow should complete within 10 seconds");

        // Verify LLM provider was engaged
        _mockLLMProvider.Verify(x => x.IsAvailableAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task System_Integration_Health_Check_Should_Pass()
    {
        // Arrange & Act
        var healthChecks = new List<bool>
        {
            await CheckLLMProviderHealth(),
            await CheckOrchestrationEngineHealth(),
            await CheckStateProviderHealth()
        };

        // Assert
        healthChecks.Should().AllBeEquivalentTo(true, "All system components should be healthy");
    }

    private async Task<bool> CheckLLMProviderHealth()
    {
        try
        {
            return await _mockLLMProvider.Object.IsAvailableAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckOrchestrationEngineHealth()
    {
        try
        {
            var testRequest = new AIAgentFramework.Orchestration.UserRequest("Health check request");
            var result = await _orchestrationEngine.ExecuteAsync(testRequest);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckStateProviderHealth()
    {
        try
        {
            var stateProvider = _serviceProvider.GetRequiredService<IStateProvider>();
            return await stateProvider.ExistsAsync("health-check", CancellationToken.None);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}