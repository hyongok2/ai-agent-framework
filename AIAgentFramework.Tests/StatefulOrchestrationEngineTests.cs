using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIAgentFramework.Core.Interfaces;
using AIAgentFramework.Orchestration;
using AIAgentFramework.Orchestration.Engines;
using AIAgentFramework.State.Interfaces;
using AIAgentFramework.State.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AIAgentFramework.Tests
{
    [TestFixture]
    public class StatefulOrchestrationEngineTests
    {
        private Mock<IOrchestrationEngine> _mockBaseEngine;
        private IStateProvider _stateProvider;
        private Mock<ILogger<StatefulOrchestrationEngine>> _mockLogger;
        private Mock<ILogger<InMemoryStateProvider>> _mockStateLogger;
        private StatefulOrchestrationEngine _statefulEngine;

        [SetUp]
        public void Setup()
        {
            _mockBaseEngine = new Mock<IOrchestrationEngine>();
            _mockStateLogger = new Mock<ILogger<InMemoryStateProvider>>();
            _stateProvider = new InMemoryStateProvider(_mockStateLogger.Object);
            _mockLogger = new Mock<ILogger<StatefulOrchestrationEngine>>();
            
            _statefulEngine = new StatefulOrchestrationEngine(
                _mockBaseEngine.Object,
                _stateProvider,
                _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _stateProvider?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_WithNewRequest_ShouldCallBaseEngineAndSaveState()
        {
            // Arrange
            var request = new UserRequest("Hello World")
            {
                RequestId = "test-session-001",
                UserId = "test-user"
            };

            var expectedResult = new Mock<IOrchestrationResult>();
            expectedResult.Setup(r => r.SessionId).Returns(request.RequestId);
            expectedResult.Setup(r => r.IsSuccess).Returns(true);
            expectedResult.Setup(r => r.IsCompleted).Returns(true);
            expectedResult.Setup(r => r.FinalResponse).Returns("Test response");
            expectedResult.Setup(r => r.ErrorMessage).Returns((string?)null);
            expectedResult.Setup(r => r.TotalDuration).Returns(TimeSpan.FromSeconds(1));
            expectedResult.Setup(r => r.Metadata).Returns(new Dictionary<string, object>());

            _mockBaseEngine.Setup(x => x.ExecuteAsync(It.IsAny<IUserRequest>()))
                          .ReturnsAsync(expectedResult.Object);

            // Act
            var result = await _statefulEngine.ExecuteAsync(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SessionId, Is.EqualTo(request.RequestId));
            Assert.That(result.IsSuccess, Is.True);

            // Verify base engine was called
            _mockBaseEngine.Verify(x => x.ExecuteAsync(It.IsAny<IUserRequest>()), Times.Once);

            // Verify state was saved
            var hasState = await _statefulEngine.HasSessionStateAsync(request.RequestId);
            Assert.That(hasState, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_WhenBaseEngineThrows_ShouldSaveFailureState()
        {
            // Arrange
            var request = new UserRequest("Error test")
            {
                RequestId = "test-session-error",
                UserId = "test-user"
            };

            var expectedException = new InvalidOperationException("Test error");
            _mockBaseEngine.Setup(x => x.ExecuteAsync(It.IsAny<IUserRequest>()))
                          .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _statefulEngine.ExecuteAsync(request));

            Assert.That(exception.Message, Is.EqualTo("Test error"));

            // Verify failure state was saved
            var hasState = await _statefulEngine.HasSessionStateAsync(request.RequestId);
            Assert.That(hasState, Is.True);
        }

        [Test]
        public async Task ContinueAsync_ShouldCallBaseEngineAndSaveState()
        {
            // Arrange
            var context = new Mock<IOrchestrationContext>();
            context.Setup(c => c.SessionId).Returns("test-session-continue");

            var expectedResult = new Mock<IOrchestrationResult>();
            expectedResult.Setup(r => r.SessionId).Returns("test-session-continue");
            expectedResult.Setup(r => r.IsSuccess).Returns(true);
            expectedResult.Setup(r => r.IsCompleted).Returns(true);
            expectedResult.Setup(r => r.FinalResponse).Returns("Continued response");
            expectedResult.Setup(r => r.ErrorMessage).Returns((string?)null);
            expectedResult.Setup(r => r.TotalDuration).Returns(TimeSpan.FromSeconds(2));
            expectedResult.Setup(r => r.Metadata).Returns(new Dictionary<string, object>());

            _mockBaseEngine.Setup(x => x.ContinueAsync(It.IsAny<IOrchestrationContext>()))
                          .ReturnsAsync(expectedResult.Object);

            // Act
            var result = await _statefulEngine.ContinueAsync(context.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SessionId, Is.EqualTo("test-session-continue"));
            Assert.That(result.IsSuccess, Is.True);

            // Verify base engine was called
            _mockBaseEngine.Verify(x => x.ContinueAsync(It.IsAny<IOrchestrationContext>()), Times.Once);

            // Verify state was saved
            var hasState = await _statefulEngine.HasSessionStateAsync("test-session-continue");
            Assert.That(hasState, Is.True);
        }

        [Test]
        public async Task ClearSessionStateAsync_ShouldRemoveState()
        {
            // Arrange
            var sessionId = "test-session-clear";
            var request = new UserRequest("Clear test") { RequestId = sessionId };

            var mockResult = new Mock<IOrchestrationResult>();
            mockResult.Setup(r => r.SessionId).Returns(sessionId);
            mockResult.Setup(r => r.IsSuccess).Returns(true);
            mockResult.Setup(r => r.IsCompleted).Returns(true);
            mockResult.Setup(r => r.Metadata).Returns(new Dictionary<string, object>());

            _mockBaseEngine.Setup(x => x.ExecuteAsync(It.IsAny<IUserRequest>()))
                          .ReturnsAsync(mockResult.Object);

            // First create some state
            await _statefulEngine.ExecuteAsync(request);

            // Verify state exists
            var hasStateBefore = await _statefulEngine.HasSessionStateAsync(sessionId);
            Assert.That(hasStateBefore, Is.True);

            // Act
            await _statefulEngine.ClearSessionStateAsync(sessionId);

            // Assert
            var hasStateAfter = await _statefulEngine.HasSessionStateAsync(sessionId);
            Assert.That(hasStateAfter, Is.False);
        }

        [Test]
        public async Task HasSessionStateAsync_WithExistingState_ShouldReturnTrue()
        {
            // Arrange
            var sessionId = "test-session-exists";
            var request = new UserRequest("Exists test") { RequestId = sessionId };

            var mockResult = new Mock<IOrchestrationResult>();
            mockResult.Setup(r => r.SessionId).Returns(sessionId);
            mockResult.Setup(r => r.IsSuccess).Returns(true);
            mockResult.Setup(r => r.IsCompleted).Returns(true);
            mockResult.Setup(r => r.Metadata).Returns(new Dictionary<string, object>());

            _mockBaseEngine.Setup(x => x.ExecuteAsync(It.IsAny<IUserRequest>()))
                          .ReturnsAsync(mockResult.Object);

            await _statefulEngine.ExecuteAsync(request);

            // Act
            var hasState = await _statefulEngine.HasSessionStateAsync(sessionId);

            // Assert
            Assert.That(hasState, Is.True);
        }

        [Test]
        public async Task HasSessionStateAsync_WithNonExistingState_ShouldReturnFalse()
        {
            // Arrange
            var sessionId = "non-existing-session";

            // Act
            var hasState = await _statefulEngine.HasSessionStateAsync(sessionId);

            // Assert
            Assert.That(hasState, Is.False);
        }

        [Test]
        public async Task IsHealthyAsync_ShouldReturnStateProviderHealth()
        {
            // Act
            var isHealthy = await _statefulEngine.IsHealthyAsync();

            // Assert
            Assert.That(isHealthy, Is.True);
        }

        [Test]
        public async Task GetStateStatisticsAsync_ShouldReturnProviderStatistics()
        {
            // Act
            var statistics = await _statefulEngine.GetStateStatisticsAsync();

            // Assert
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics.CollectedAt, Is.Not.EqualTo(default(DateTime)));
        }

        [Test]
        public async Task CleanupExpiredStatesAsync_ShouldReturnCleanedCount()
        {
            // Act
            var cleanedCount = await _statefulEngine.CleanupExpiredStatesAsync();

            // Assert
            Assert.That(cleanedCount, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Constructor_WithNullBaseEngine_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new StatefulOrchestrationEngine(null!, _stateProvider, _mockLogger.Object));
        }

        [Test]
        public void Constructor_WithNullStateProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new StatefulOrchestrationEngine(_mockBaseEngine.Object, null!, _mockLogger.Object));
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new StatefulOrchestrationEngine(_mockBaseEngine.Object, _stateProvider, null!));
        }

        [Test]
        public void ExecuteAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _statefulEngine.ExecuteAsync(null!));
        }

        [Test]
        public void ContinueAsync_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _statefulEngine.ContinueAsync(null!));
        }

        [Test]
        public void ClearSessionStateAsync_WithNullSessionId_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _statefulEngine.ClearSessionStateAsync(null!));
        }

        [Test]
        public void ClearSessionStateAsync_WithEmptySessionId_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _statefulEngine.ClearSessionStateAsync(string.Empty));
        }
    }
}