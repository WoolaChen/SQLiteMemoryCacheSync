using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;
using SQLiteMemoryCacheSync.Services;

namespace SQLiteMemoryCacheSync.Tests
{
    public class MemoryCacheManagerTests
    {
        private readonly Mock<ILogger<MemoryCacheManager>> _mockLogger;
        private readonly Mock<IPersistentDatabase> _mockPersistentDb;
        private MemoryCacheManager _cacheManager;

        public MemoryCacheManagerTests()
        {
            _mockLogger = new Mock<ILogger<MemoryCacheManager>>();
            _mockPersistentDb = new Mock<IPersistentDatabase>();
            _cacheManager = new MemoryCacheManager(_mockLogger.Object, _mockPersistentDb.Object);
        }

        [Fact]
        public async Task InitializeAsync_ShouldCreateInMemoryCache()
        {
            // Act
            await _cacheManager.InitializeAsync();

            // Assert - Should not throw exception
            Assert.NotNull(_cacheManager);
        }

        [Fact]
        public async Task InsertAsync_ShouldInsertRecordAndTrackChange()
        {
            // Arrange
            await _cacheManager.InitializeAsync();
            var testData = new Dictionary<string, object>
            {
                { "id", "test-1" },
                { "name", "Test User" },
                { "email", "test@example.com" }
            };

            // Act
            var result = await _cacheManager.InsertAsync("users", testData);
            var changes = await _cacheManager.GetChangesAsync();

            // Assert
            Assert.True(result > 0);
            Assert.Single(changes);
            Assert.Equal(OperationType.Insert, changes[0].OperationType);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateRecordAndTrackChange()
        {
            // Arrange
            await _cacheManager.InitializeAsync();
            var testData = new Dictionary<string, object>
            {
                { "id", "test-1" },
                { "name", "Test User" },
                { "email", "test@example.com" }
            };
            await _cacheManager.InsertAsync("users", testData);

            var updateData = new Dictionary<string, object>
            {
                { "name", "Updated User" }
            };

            // Act
            var result = await _cacheManager.UpdateAsync("users", "test-1", updateData);
            var changes = await _cacheManager.GetChangesAsync();

            // Assert
            Assert.True(result > 0);
            Assert.Equal(2, changes.Count); // Insert + Update
            Assert.Equal(OperationType.Update, changes[1].OperationType);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteRecordAndTrackChange()
        {
            // Arrange
            await _cacheManager.InitializeAsync();
            var testData = new Dictionary<string, object>
            {
                { "id", "test-1" },
                { "name", "Test User" }
            };
            await _cacheManager.InsertAsync("users", testData);

            // Act
            var result = await _cacheManager.DeleteAsync("users", "test-1");
            var changes = await _cacheManager.GetChangesAsync();

            // Assert
            Assert.True(result > 0);
            Assert.Equal(2, changes.Count); // Insert + Delete
            Assert.Equal(OperationType.Delete, changes[1].OperationType);
        }

        [Fact]
        public async Task QueryAsync_ShouldReturnRecords()
        {
            // Arrange
            await _cacheManager.InitializeAsync();
            var testData = new Dictionary<string, object>
            {
                { "id", "test-1" },
                { "name", "Test User" },
                { "email", "test@example.com" }
            };
            await _cacheManager.InsertAsync("users", testData);

            // Act
            var results = await _cacheManager.QueryAsync("SELECT * FROM users WHERE id = @id",
                new Dictionary<string, object> { { "id", "test-1" } });

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal("test-1", results[0]["id"].ToString());
        }

        [Fact]
        public async Task ClearChangesAsync_ShouldMarkChangesAsSynced()
        {
            // Arrange
            await _cacheManager.InitializeAsync();
            var testData = new Dictionary<string, object>
            {
                { "id", "test-1" },
                { "name", "Test User" }
            };
            await _cacheManager.InsertAsync("users", testData);
            var changes = await _cacheManager.GetChangesAsync();
            var changeId = changes[0].Id;

            // Act
            await _cacheManager.ClearChangesAsync(new List<int> { changeId });
            var remainingChanges = await _cacheManager.GetChangesAsync();

            // Assert
            Assert.Empty(remainingChanges);
        }

        [Fact]
        public async Task GetStatsAsync_ShouldReturnCacheStatistics()
        {
            // Arrange
            await _cacheManager.InitializeAsync();
            var testData = new Dictionary<string, object>
            {
                { "id", "test-1" },
                { "name", "Test User" }
            };
            await _cacheManager.InsertAsync("users", testData);

            // Act
            var stats = await _cacheManager.GetStatsAsync();

            // Assert
            Assert.NotEmpty(stats);
            Assert.True(stats.ContainsKey("TotalChanges"));
            Assert.True(stats.ContainsKey("PendingChanges"));
        }
    }
}
