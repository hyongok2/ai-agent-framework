using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIAgentFramework.State.Configuration;
using AIAgentFramework.State.Infrastructure;
using AIAgentFramework.State.Interfaces;
using AIAgentFramework.State.Models;
using AIAgentFramework.State.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace AIAgentFramework.State.Tests
{
    [TestFixture]
    public class EnhancedRedisStateProviderTests
    {
        private Mock<RedisConnectionManager> _mockConnectionManager = null!;
        private Mock<RedisBatchClient> _mockBatchClient = null!;
        private Mock<IOptions<RedisStateOptions>> _mockOptions = null!;
        private Mock<ILogger<EnhancedRedisStateProvider>> _mockLogger = null!;
        private RedisStateOptions _options = null!;
        private EnhancedRedisStateProvider _provider = null!;

        [SetUp]
        public void SetUp()
        {
            _options = new RedisStateOptions
            {
                ConnectionString = "localhost:6379",
                Database = 0,
                KeyPrefix = "test:",
                DefaultExpiration = TimeSpan.FromHours(1),
                UseCluster = false,
                EnablePerformanceCounters = true,
                SlowQueryLogThresholdMs = 100
            };

            _mockConnectionManager = new Mock<RedisConnectionManager>(
                Mock.Of<IOptions<RedisStateOptions>>(),
                Mock.Of<ILogger<RedisConnectionManager>>());

            _mockBatchClient = new Mock<RedisBatchClient>(
                _mockConnectionManager.Object,
                Mock.Of<IOptions<RedisStateOptions>>(),
                Mock.Of<ILogger<RedisBatchClient>>());

            _mockOptions = new Mock<IOptions<RedisStateOptions>>();
            _mockOptions.Setup(x => x.Value).Returns(_options);

            _mockLogger = new Mock<ILogger<EnhancedRedisStateProvider>>();

            _provider = new EnhancedRedisStateProvider(
                _mockConnectionManager.Object,
                _mockBatchClient.Object,
                _mockOptions.Object,
                _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
        }

        [Test]
        public async Task GetAsync_ShouldReturnState_WhenStateExists()
        {
            // Arrange
            var sessionId = "test-session";
            var mockDatabase = new Mock<IDatabase>();
            var testState = new TestState { Value = "test-value", Timestamp = DateTime.UtcNow };
            var serializedState = System.Text.Json.JsonSerializer.Serialize(testState);

            mockDatabase.Setup(db => db.StringGetAsync($"test:{sessionId}", CommandFlags.None))
                       .ReturnsAsync(new RedisValue(serializedState));

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(true, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act
            var result = await _provider.GetAsync<TestState>(sessionId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value, Is.EqualTo("test-value"));
        }

        [Test]
        public async Task GetAsync_ShouldReturnNull_WhenStateDoesNotExist()
        {
            // Arrange
            var sessionId = "non-existent-session";
            var mockDatabase = new Mock<IDatabase>();

            mockDatabase.Setup(db => db.StringGetAsync($"test:{sessionId}", CommandFlags.None))
                       .ReturnsAsync(RedisValue.Null);

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(true, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act
            var result = await _provider.GetAsync<TestState>(sessionId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SetAsync_ShouldSaveState_WhenValidInput()
        {
            // Arrange
            var sessionId = "test-session";
            var testState = new TestState { Value = "test-value", Timestamp = DateTime.UtcNow };
            var mockDatabase = new Mock<IDatabase>();

            mockDatabase.Setup(db => db.StringSetAsync(
                $"test:{sessionId}", 
                It.IsAny<RedisValue>(), 
                _options.DefaultExpiration, 
                When.Always, 
                CommandFlags.None))
                .ReturnsAsync(true);

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(false, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _provider.SetAsync(sessionId, testState));
        }

        [Test]
        public async Task GetBatchAsync_ShouldReturnMultipleStates()
        {
            // Arrange
            var sessionIds = new[] { "session1", "session2", "session3" };
            var expectedResults = new Dictionary<string, TestState?>
            {
                ["session1"] = new TestState { Value = "value1", Timestamp = DateTime.UtcNow },
                ["session2"] = new TestState { Value = "value2", Timestamp = DateTime.UtcNow },
                ["session3"] = null
            };

            _mockBatchClient.Setup(bc => bc.GetBatchAsync<TestState>(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResults);

            // Act
            var result = await _provider.GetBatchAsync<TestState>(sessionIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result["session1"]?.Value, Is.EqualTo("value1"));
            Assert.That(result["session2"]?.Value, Is.EqualTo("value2"));
            Assert.That(result["session3"], Is.Null);
        }

        [Test]
        public async Task SetBatchAsync_ShouldSaveMultipleStates()
        {
            // Arrange
            var sessionData = new Dictionary<string, TestState>
            {
                ["session1"] = new TestState { Value = "value1", Timestamp = DateTime.UtcNow },
                ["session2"] = new TestState { Value = "value2", Timestamp = DateTime.UtcNow }
            };

            _mockBatchClient.Setup(bc => bc.SetBatchAsync(
                It.IsAny<IDictionary<string, TestState>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _provider.SetBatchAsync(sessionData));
        }

        [Test]
        public async Task BeginTransactionAsync_ShouldReturnTransaction()
        {
            // Act
            var transaction = await _provider.BeginTransactionAsync();

            // Assert
            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction.State, Is.EqualTo(TransactionState.Active));
            Assert.That(transaction.TransactionId, Is.Not.Empty);

            // Cleanup
            transaction.Dispose();
        }

        [Test]
        public async Task GetStatisticsAsync_ShouldReturnStatistics()
        {
            // Act
            var statistics = await _provider.GetStatisticsAsync();

            // Assert
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics.CollectedAt, Is.Not.EqualTo(default(DateTime)));
        }

        [Test]
        public async Task IsHealthyAsync_ShouldCheckConnectionHealth()
        {
            // Arrange
            _mockConnectionManager.Setup(cm => cm.IsHealthyAsync(It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(true);

            // Act
            var isHealthy = await _provider.IsHealthyAsync();

            // Assert
            Assert.That(isHealthy, Is.True);
        }

        [Test]
        public void GetAsync_ShouldThrowArgumentException_WhenSessionIdIsEmpty()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await _provider.GetAsync<TestState>(""));
        }

        [Test]
        public void SetAsync_ShouldThrowArgumentNullException_WhenStateIsNull()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _provider.SetAsync<TestState>("session", null!));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenConnectionManagerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EnhancedRedisStateProvider(
                null!,
                _mockBatchClient.Object,
                _mockOptions.Object,
                _mockLogger.Object));
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _provider.Dispose());
            
            // Subsequent operations should throw ObjectDisposedException
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await _provider.GetAsync<TestState>("session"));
        }

        private class TestState
        {
            public string Value { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    }
}