using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;
using SQLiteMemoryCacheSync.Services;

namespace SQLiteMemoryCacheSync.Tests
{
    public class ConflictResolverTests
    {
        private readonly Mock<ILogger<ConflictResolver>> _mockLogger;
        private readonly ConflictResolver _resolver;

        public ConflictResolverTests()
        {
            _mockLogger = new Mock<ILogger<ConflictResolver>>();
            _resolver = new ConflictResolver(_mockLogger.Object);
        }

        [Fact]
        public async Task ResolveAsync_WithCacheWins_ShouldReturnCacheData()
        {
            // Arrange
            var cacheData = new Dictionary<string, object>
            {
                { "id", "user-1" },
                { "name", "Cache Version" },
                { "version", 2 }
            };
            var dbData = new Dictionary<string, object>
            {
                { "id", "user-1" },
                { "name", "DB Version" },
                { "version", 1 }
            };

            // Act
            var result = await _resolver.ResolveAsync(
                "users",
                cacheData,
                dbData,
                ConflictResolutionStrategy.CacheWins);

            // Assert
            Assert.Equal("Cache Version", result["name"]);
            Assert.Equal(2, result["version"]);
        }

        [Fact]
        public async Task ResolveAsync_WithDbWins_ShouldReturnDbData()
        {
            // Arrange
            var cacheData = new Dictionary<string, object>
            {
                { "id", "user-1" },
                { "name", "Cache Version" }
            };
            var dbData = new Dictionary<string, object>
            {
                { "id", "user-1" },
                { "name", "DB Version" }
            };

            // Act
            var result = await _resolver.ResolveAsync(
                "users",
                cacheData,
                dbData,
                ConflictResolutionStrategy.DbWins);

            // Assert
            Assert.Equal("DB Version", result["name"]);
        }

        [Fact]
        public async Task ResolveAsync_WithMerge_ShouldCombineData()
        {
            // Arrange
            var cacheData = new Dictionary<string, object>
            {
                { "id", "user-1" },
                { "name", "Cache Version" },
                { "email", "cache@example.com" }
            };
            var dbData = new Dictionary<string, object>
            {
                { "id", "user-1" },
                { "name", "DB Version" },
                { "phone", "123-456-7890" }
            };

            // Act
            var result = await _resolver.ResolveAsync(
                "users",
                cacheData,
                dbData,
                ConflictResolutionStrategy.Merge);

            // Assert
            Assert.Equal("Cache Version", result["name"]); // Cache wins on conflict
            Assert.Equal("cache@example.com", result["email"]); // From cache
            Assert.Equal("123-456-7890", result["phone"]); // From DB
        }

        [Fact]
        public async Task ResolveAsync_WithThrowException_ShouldThrow()
        {
            // Arrange
            var cacheData = new Dictionary<string, object> { { "id", "user-1" }, { "name", "Cache" } };
            var dbData = new Dictionary<string, object> { { "id", "user-1" }, { "name", "DB" } };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _resolver.ResolveAsync(
                "users",
                cacheData,
                dbData,
                ConflictResolutionStrategy.ThrowException));
        }
    }
}
