using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIAgentFramework.State.Configuration;
using AIAgentFramework.State.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace AIAgentFramework.State.Tests
{
    [TestFixture]
    public class RedisBatchClientTests
    {
        private Mock<RedisConnectionManager> _mockConnectionManager = null!;
        private Mock<IOptions<RedisStateOptions>> _mockOptions = null!;
        private Mock<ILogger<RedisBatchClient>> _mockLogger = null!;
        private RedisStateOptions _options = null!;
        private RedisBatchClient _batchClient = null!;

        [SetUp]
        public void SetUp()
        {
            _options = new RedisStateOptions
            {
                ConnectionString = "localhost:6379",
                Database = 0,
                KeyPrefix = "test:",
                DefaultExpiration = TimeSpan.FromHours(1),
                PipelineBatchSize = 50,
                UseCompression = false
            };

            _mockConnectionManager = new Mock<RedisConnectionManager>(
                Mock.Of<IOptions<RedisStateOptions>>(),
                Mock.Of<ILogger<RedisConnectionManager>>());

            _mockOptions = new Mock<IOptions<RedisStateOptions>>();
            _mockOptions.Setup(x => x.Value).Returns(_options);

            _mockLogger = new Mock<ILogger<RedisBatchClient>>();

            _batchClient = new RedisBatchClient(
                _mockConnectionManager.Object,
                _mockOptions.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task GetBatchAsync_ShouldReturnEmptyDictionary_WhenNoSessionIds()
        {
            // Arrange
            var emptySessionIds = Enumerable.Empty<string>();

            // Act
            var result = await _batchClient.GetBatchAsync<TestState>(emptySessionIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetBatchAsync_ShouldReturnMultipleValues_WhenSessionIdsProvided()
        {
            // Arrange
            var sessionIds = new[] { "session1", "session2", "session3" };
            var mockDatabase = new Mock<IDatabase>();
            var testState1 = new TestState { Value = "value1", Timestamp = DateTime.UtcNow };
            var testState2 = new TestState { Value = "value2", Timestamp = DateTime.UtcNow };

            // Mock Redis responses
            mockDatabase.Setup(db => db.StringGetAsync("test:session1", CommandFlags.None))
                       .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(testState1));
            mockDatabase.Setup(db => db.StringGetAsync("test:session2", CommandFlags.None))
                       .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(testState2));
            mockDatabase.Setup(db => db.StringGetAsync("test:session3", CommandFlags.None))
                       .ReturnsAsync(RedisValue.Null);

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(true, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act
            var result = await _batchClient.GetBatchAsync<TestState>(sessionIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result["session1"]?.Value, Is.EqualTo("value1"));
            Assert.That(result["session2"]?.Value, Is.EqualTo("value2"));
            Assert.That(result["session3"], Is.Null);
        }

        [Test]
        public async Task SetBatchAsync_ShouldReturnImmediately_WhenNoData()
        {
            // Arrange
            var emptyData = new Dictionary<string, TestState>();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _batchClient.SetBatchAsync(emptyData));
        }

        [Test]
        public async Task SetBatchAsync_ShouldExecuteBatch_WhenDataProvided()
        {
            // Arrange
            var sessionData = new Dictionary<string, TestState>
            {
                ["session1"] = new TestState { Value = "value1", Timestamp = DateTime.UtcNow },
                ["session2"] = new TestState { Value = "value2", Timestamp = DateTime.UtcNow }
            };

            var mockDatabase = new Mock<IDatabase>();
            var mockBatch = new Mock<IBatch>();

            mockDatabase.Setup(db => db.CreateBatch()).Returns(mockBatch.Object);
            mockBatch.Setup(b => b.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                    .Returns(Task.FromResult(true));

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(false, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _batchClient.SetBatchAsync(sessionData));

            // Verify batch execution
            mockBatch.Verify(b => b.Execute(), Times.Once);
        }

        [Test]
        public async Task DeleteBatchAsync_ShouldReturnImmediately_WhenNoSessionIds()
        {
            // Arrange
            var emptySessionIds = Enumerable.Empty<string>();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _batchClient.DeleteBatchAsync(emptySessionIds));
        }

        [Test]
        public async Task DeleteBatchAsync_ShouldDeleteKeys_WhenSessionIdsProvided()
        {
            // Arrange
            var sessionIds = new[] { "session1", "session2" };
            var mockDatabase = new Mock<IDatabase>();

            mockDatabase.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                       .ReturnsAsync(2);

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(false, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _batchClient.DeleteBatchAsync(sessionIds));

            // Verify deletion
            mockDatabase.Verify(db => db.KeyDeleteAsync(
                It.Is<RedisKey[]>(keys => keys.Length == 2 && 
                                         keys.Contains("test:session1") && 
                                         keys.Contains("test:session2")), 
                It.IsAny<CommandFlags>()), Times.Once);
        }

        [Test]
        public async Task ExistsBatchAsync_ShouldReturnEmptyDictionary_WhenNoSessionIds()
        {
            // Arrange
            var emptySessionIds = Enumerable.Empty<string>();

            // Act
            var result = await _batchClient.ExistsBatchAsync(emptySessionIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ExistsBatchAsync_ShouldReturnExistenceMap_WhenSessionIdsProvided()
        {
            // Arrange
            var sessionIds = new[] { "session1", "session2", "session3" };
            var mockDatabase = new Mock<IDatabase>();

            // Mock responses: session1 exists (1), session2 doesn't exist (0), session3 exists (1)
            mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                       .ReturnsAsync(new long[] { 1, 0, 1 });

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(true, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act
            var result = await _batchClient.ExistsBatchAsync(sessionIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result["session1"], Is.True);
            Assert.That(result["session2"], Is.False);
            Assert.That(result["session3"], Is.True);
        }

        [Test]
        public async Task ExecutePipelineAsync_ShouldReturnEmptyArray_WhenNoOperations()
        {
            // Arrange
            var emptyOperations = Enumerable.Empty<Func<IDatabase, Task<string>>>();

            // Act
            var result = await _batchClient.ExecutePipelineAsync(emptyOperations);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task ExecutePipelineAsync_ShouldExecuteOperations_WhenOperationsProvided()
        {
            // Arrange
            var mockDatabase = new Mock<IDatabase>();
            var operations = new List<Func<IDatabase, Task<string>>>
            {
                db => Task.FromResult("result1"),
                db => Task.FromResult("result2"),
                db => Task.FromResult("result3")
            };

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(false, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act
            var results = await _batchClient.ExecutePipelineAsync(operations);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo("result1"));
            Assert.That(results[1], Is.EqualTo("result2"));
            Assert.That(results[2], Is.EqualTo("result3"));
        }

        [Test]
        public async Task ExecuteAtomicBatchAsync_ShouldExecuteLuaScript()
        {
            // Arrange
            var luaScript = "return redis.call('GET', KEYS[1])";
            var keys = new RedisKey[] { "test:session1" };
            var values = new RedisValue[] { "value1" };
            var expectedResult = new RedisResult("script_result");

            var mockDatabase = new Mock<IDatabase>();
            mockDatabase.Setup(db => db.ScriptEvaluateAsync(luaScript, keys, values, It.IsAny<CommandFlags>()))
                       .ReturnsAsync(expectedResult);

            _mockConnectionManager.Setup(cm => cm.GetDatabaseAsync(false, It.IsAny<System.Threading.CancellationToken>()))
                                  .ReturnsAsync(mockDatabase.Object);

            // Act
            var result = await _batchClient.ExecuteAtomicBatchAsync(luaScript, keys, values);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
            mockDatabase.Verify(db => db.ScriptEvaluateAsync(luaScript, keys, values, It.IsAny<CommandFlags>()), Times.Once);
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenConnectionManagerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RedisBatchClient(
                null!,
                _mockOptions.Object,
                _mockLogger.Object));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RedisBatchClient(
                _mockConnectionManager.Object,
                null!,
                _mockLogger.Object));
        }

        private class TestState
        {
            public string Value { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    }
}