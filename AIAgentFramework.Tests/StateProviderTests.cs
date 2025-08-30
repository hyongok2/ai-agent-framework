using System;
using System.Threading;
using System.Threading.Tasks;
using AIAgentFramework.State.Interfaces;
using AIAgentFramework.State.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AIAgentFramework.Tests
{
    [TestFixture]
    public class StateProviderTests
    {
        private Mock<ILogger<InMemoryStateProvider>> _mockLogger;
        private IStateProvider _stateProvider;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<InMemoryStateProvider>>();
            _stateProvider = new InMemoryStateProvider(_mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _stateProvider?.Dispose();
        }

        [Test]
        public async Task SetAsync_And_GetAsync_ShouldWorkCorrectly()
        {
            // Arrange
            var sessionId = "test-session-001";
            var testData = new TestStateData { Id = 1, Name = "Test Data", Value = 42.5 };

            // Act
            await _stateProvider.SetAsync(sessionId, testData);
            var retrievedData = await _stateProvider.GetAsync<TestStateData>(sessionId);

            // Assert
            Assert.That(retrievedData, Is.Not.Null);
            Assert.That(retrievedData.Id, Is.EqualTo(testData.Id));
            Assert.That(retrievedData.Name, Is.EqualTo(testData.Name));
            Assert.That(retrievedData.Value, Is.EqualTo(testData.Value));
        }

        [Test]
        public async Task ExistsAsync_WithExistingSession_ShouldReturnTrue()
        {
            // Arrange
            var sessionId = "test-session-002";
            var testData = new TestStateData { Id = 2, Name = "Test Data 2", Value = 100.0 };

            // Act
            await _stateProvider.SetAsync(sessionId, testData);
            var exists = await _stateProvider.ExistsAsync(sessionId);

            // Assert
            Assert.That(exists, Is.True);
        }

        [Test]
        public async Task ExistsAsync_WithNonExistingSession_ShouldReturnFalse()
        {
            // Arrange
            var sessionId = "non-existing-session";

            // Act
            var exists = await _stateProvider.ExistsAsync(sessionId);

            // Assert
            Assert.That(exists, Is.False);
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveState()
        {
            // Arrange
            var sessionId = "test-session-003";
            var testData = new TestStateData { Id = 3, Name = "Test Data 3", Value = 200.0 };

            // Act
            await _stateProvider.SetAsync(sessionId, testData);
            var existsBeforeDelete = await _stateProvider.ExistsAsync(sessionId);
            
            await _stateProvider.DeleteAsync(sessionId);
            var existsAfterDelete = await _stateProvider.ExistsAsync(sessionId);
            var retrievedData = await _stateProvider.GetAsync<TestStateData>(sessionId);

            // Assert
            Assert.That(existsBeforeDelete, Is.True);
            Assert.That(existsAfterDelete, Is.False);
            Assert.That(retrievedData, Is.Null);
        }

        [Test]
        public async Task SetAsync_WithExpiry_ShouldExpireAfterTTL()
        {
            // Arrange
            var sessionId = "test-session-expiry";
            var testData = new TestStateData { Id = 4, Name = "Expiry Test", Value = 300.0 };
            var expiry = TimeSpan.FromMilliseconds(100);

            // Act
            await _stateProvider.SetAsync(sessionId, testData, expiry);
            
            // Check immediately - should exist
            var existsImmediately = await _stateProvider.ExistsAsync(sessionId);
            
            // Wait for expiry
            await Task.Delay(150);
            
            // Check after expiry - should not exist
            var existsAfterExpiry = await _stateProvider.ExistsAsync(sessionId);

            // Assert
            Assert.That(existsImmediately, Is.True);
            Assert.That(existsAfterExpiry, Is.False);
        }

        [Test]
        public async Task IsHealthyAsync_ShouldReturnTrue()
        {
            // Act
            var isHealthy = await _stateProvider.IsHealthyAsync();

            // Assert
            Assert.That(isHealthy, Is.True);
        }

        [Test]
        public async Task GetStatisticsAsync_ShouldReturnValidStatistics()
        {
            // Arrange
            var sessionId1 = "stats-test-001";
            var sessionId2 = "stats-test-002";
            var testData1 = new TestStateData { Id = 1, Name = "Stats Test 1", Value = 100.0 };
            var testData2 = new TestStateData { Id = 2, Name = "Stats Test 2", Value = 200.0 };

            // Act
            await _stateProvider.SetAsync(sessionId1, testData1);
            await _stateProvider.SetAsync(sessionId2, testData2);
            
            // Get some data to generate reads
            await _stateProvider.GetAsync<TestStateData>(sessionId1);
            await _stateProvider.GetAsync<TestStateData>(sessionId2);
            
            var statistics = await _stateProvider.GetStatisticsAsync();

            // Assert
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics.TotalStates, Is.EqualTo(2));
            Assert.That(statistics.ActiveSessions, Is.EqualTo(2));
            Assert.That(statistics.TotalReads, Is.GreaterThan(0));
            Assert.That(statistics.TotalWrites, Is.GreaterThan(0));
            Assert.That(statistics.CollectedAt, Is.Not.EqualTo(default(DateTime)));
        }

        [Test]
        public async Task BeginTransactionAsync_ShouldReturnValidTransaction()
        {
            // Act
            using var transaction = await _stateProvider.BeginTransactionAsync();

            // Assert
            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction.TransactionId, Is.Not.Empty);
            Assert.That(transaction.StartTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(transaction.State, Is.EqualTo(TransactionState.Active));
        }

        [Test]
        public async Task Transaction_SetAndCommit_ShouldPersistData()
        {
            // Arrange
            var sessionId = "transaction-test-001";
            var testData = new TestStateData { Id = 1, Name = "Transaction Test", Value = 500.0 };

            // Act
            using var transaction = await _stateProvider.BeginTransactionAsync();
            await transaction.SetAsync(sessionId, testData);
            await transaction.CommitAsync();

            // Check data exists in main provider
            var retrievedData = await _stateProvider.GetAsync<TestStateData>(sessionId);

            // Assert
            Assert.That(retrievedData, Is.Not.Null);
            Assert.That(retrievedData.Name, Is.EqualTo(testData.Name));
        }

        [Test]
        public async Task Transaction_SetAndRollback_ShouldNotPersistData()
        {
            // Arrange
            var sessionId = "transaction-rollback-001";
            var testData = new TestStateData { Id = 1, Name = "Rollback Test", Value = 600.0 };

            // Act
            using var transaction = await _stateProvider.BeginTransactionAsync();
            await transaction.SetAsync(sessionId, testData);
            await transaction.RollbackAsync();

            // Check data does not exist in main provider
            var retrievedData = await _stateProvider.GetAsync<TestStateData>(sessionId);

            // Assert
            Assert.That(retrievedData, Is.Null);
        }

        [Test]
        public async Task CleanupExpiredStatesAsync_ShouldRemoveExpiredStates()
        {
            // Arrange
            var sessionId1 = "cleanup-test-001";
            var sessionId2 = "cleanup-test-002";
            var testData = new TestStateData { Id = 1, Name = "Cleanup Test", Value = 700.0 };
            
            // Set one with short expiry, one without
            await _stateProvider.SetAsync(sessionId1, testData, TimeSpan.FromMilliseconds(50));
            await _stateProvider.SetAsync(sessionId2, testData);
            
            // Wait for first to expire
            await Task.Delay(100);

            // Act
            var cleanedCount = await _stateProvider.CleanupExpiredStatesAsync();

            // Assert
            Assert.That(cleanedCount, Is.GreaterThan(0));
            Assert.That(await _stateProvider.ExistsAsync(sessionId1), Is.False);
            Assert.That(await _stateProvider.ExistsAsync(sessionId2), Is.True);
        }

        [Test]
        public void SetAsync_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            // Arrange
            var testData = new TestStateData { Id = 1, Name = "Null Test", Value = 800.0 };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _stateProvider.SetAsync(null!, testData));
        }

        [Test]
        public void SetAsync_WithNullState_ShouldThrowArgumentNullException()
        {
            // Arrange
            var sessionId = "null-state-test";

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _stateProvider.SetAsync<TestStateData>(sessionId, null!));
        }

        [Test]
        public void GetAsync_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _stateProvider.GetAsync<TestStateData>(null!));
        }
    }

    /// <summary>
    /// 테스트용 상태 데이터 클래스
    /// </summary>
    public class TestStateData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}