using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Orchestration.Context;
using AIAgentFramework.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AIAgentFramework.Tests;

public class ContextManagerTests
{
    private Mock<ILogger<ContextManager>> _mockLogger;
    private Mock<IRegistry> _mockRegistry;
    private ContextManager _contextManager;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ContextManager>>();
        _mockRegistry = new Mock<IRegistry>();
        _contextManager = new ContextManager(_mockLogger.Object, _mockRegistry.Object);
    }

    [Test]
    public void CreateContext_ReturnsValidContext()
    {
        // Arrange
        var userRequest = "테스트 요청";

        // Act
        var context = _contextManager.CreateContext(userRequest);

        // Assert
        Assert.That(context, Is.Not.Null);
        Assert.That((context as OrchestrationContext)?.UserRequest, Is.EqualTo(userRequest));
        Assert.That(context.IsCompleted, Is.False);
        Assert.That(context.SessionId, Is.Not.Empty);
    }

    [Test]
    public void GetContext_WithValidSessionId_ReturnsContext()
    {
        // Arrange
        var context = _contextManager.CreateContext("테스트");
        var sessionId = context.SessionId;

        // Act
        var retrievedContext = _contextManager.GetContext(sessionId);

        // Assert
        Assert.That(retrievedContext, Is.Not.Null);
        Assert.That(retrievedContext.SessionId, Is.EqualTo(sessionId));
    }

    [Test]
    public void GetContext_WithInvalidSessionId_ReturnsNull()
    {
        // Act
        var context = _contextManager.GetContext("invalid-session-id");

        // Assert
        Assert.That(context, Is.Null);
    }
}