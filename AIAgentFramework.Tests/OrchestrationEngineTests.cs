using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AIAgentFramework.Tests;

public class OrchestrationEngineTests
{
    private Mock<IRegistry> _mockRegistry;
    private Mock<ILogger<OrchestrationEngine>> _mockLogger;
    private OrchestrationEngine _orchestrationEngine;

    [SetUp]
    public void Setup()
    {
        _mockRegistry = new Mock<IRegistry>();
        _mockLogger = new Mock<ILogger<OrchestrationEngine>>();
        _orchestrationEngine = new OrchestrationEngine(_mockRegistry.Object, _mockLogger.Object);
    }

    [Test]
    public async Task ExecuteAsync_WithValidRequest_ReturnsResult()
    {
        // Arrange
        var userRequest = new UserRequest("테스트 요청");

        // Act
        var result = await _orchestrationEngine.ExecuteAsync(userRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SessionId, Is.Not.Empty);
        Assert.That(result.IsCompleted, Is.True);
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyRequest_HandlesGracefully()
    {
        // Arrange
        var userRequest = new UserRequest("");

        // Act
        var result = await _orchestrationEngine.ExecuteAsync(userRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsCompleted, Is.True);
    }
}