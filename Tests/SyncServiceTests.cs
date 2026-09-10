using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;
using SQLiteMemoryCacheSync.Services;

namespace SQLiteMemoryCacheSync.Tests
{
    public class SyncServiceTests
    {
        private readonly Mock<ILogger<SyncService>> _mockLogger;
        private readonly Mock<IMemoryCacheManager> _mockCacheManager;
        private readonly Mock<IPersistentDatabase> _mockPersistentDb;
        private readonly Mock<IConflictResolver> _mockConflictResolver;
        private readonly CacheSettings _cacheSettings;
        private readonly SyncService _syncService;

        public SyncServiceTests()
        {
            _mockLogger = new Mock<ILogger<SyncService>>();
            _mockCacheManager = new Mock<IMemoryCacheManager>();
            _mockPersistentDb = new Mock<IPersistentDatabase>();
            _mockConflictResolver = new Mock<IConflictResolver>();
            _cacheSettings = new CacheSettings
            {
                SyncIntervalSeconds = 5,
                BatchSizeForSync = 100,
                ConflictResolutionStrategy = ConflictResolutionStrategy.CacheWins
            };
            _syncService = new SyncService(
                _mockCacheManager.Object,
                _mockPersistentDb.Object,
                _mockConflictResolver.Object,
                _cacheSettings,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SyncAsync_WithNoChanges_ShouldReturnSuccess()
        {
            // Arrange
            _mockCacheManager.Setup(m => m.GetChangesAsync())
                .ReturnsAsync(new List<CacheChange>());

            // Act
            var result = await _syncService.SyncAsync();

            // Assert
            Assert.Equal(SyncStatus.Success, result.Status);
            Assert.Equal(0, result.TotalChanges);
        }

        [Fact]
        public void Start_ShouldSetStatusToIdle()
        {
            // Act
            _syncService.Start();

            // Assert
            Assert.Equal(SyncStatus.Idle, _syncService.GetStatus());

            // Cleanup
            _syncService.Stop();
        }

        [Fact]
        public void Stop_ShouldStopSyncWorker()
        {
            // Arrange
            _syncService.Start();

            // Act
            _syncService.Stop();

            // Assert - Service should not throw
            Assert.NotNull(_syncService);
        }

        [Fact]
        public async Task GetLastSyncResult_ShouldReturnPreviousSyncResult()
        {
            // Arrange
            _mockCacheManager.Setup(m => m.GetChangesAsync())
                .ReturnsAsync(new List<CacheChange>());
            await _syncService.SyncAsync();

            // Act
            var result = _syncService.GetLastSyncResult();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(SyncStatus.Success, result.Status);
        }
    }
}
