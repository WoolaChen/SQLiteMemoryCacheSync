using System.Text.Json;
using Microsoft.Extensions.Logging;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;

namespace SQLiteMemoryCacheSync.Services
{
    /// <summary>
    /// Implementation of conflict resolution logic
    /// </summary>
    public class ConflictResolver : IConflictResolver
    {
        private readonly ILogger<ConflictResolver> _logger;

        public ConflictResolver(ILogger<ConflictResolver> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resolve conflict between cache and database versions
        /// </summary>
        public async Task<Dictionary<string, object>> ResolveAsync(
            string tableName,
            Dictionary<string, object> cacheData,
            Dictionary<string, object> dbData,
            ConflictResolutionStrategy strategy)
        {
            _logger.LogWarning($"Conflict detected in table {tableName}. Strategy: {strategy}");

            return strategy switch
            {
                ConflictResolutionStrategy.CacheWins => CacheWinsResolution(cacheData, dbData),
                ConflictResolutionStrategy.DbWins => DbWinsResolution(cacheData, dbData),
                ConflictResolutionStrategy.Merge => await MergeResolution(cacheData, dbData),
                ConflictResolutionStrategy.ThrowException => throw new InvalidOperationException(
                    $"Conflict detected in {tableName}. Cache and database versions differ."),
                _ => CacheWinsResolution(cacheData, dbData)
            };
        }

        /// <summary>
        /// Cache wins - use cache version
        /// </summary>
        private Dictionary<string, object> CacheWinsResolution(
            Dictionary<string, object> cacheData,
            Dictionary<string, object> dbData)
        {
            _logger.LogInformation("Resolving conflict: Cache wins strategy applied");
            return cacheData;
        }

        /// <summary>
        /// Database wins - use database version
        /// </summary>
        private Dictionary<string, object> DbWinsResolution(
            Dictionary<string, object> cacheData,
            Dictionary<string, object> dbData)
        {
            _logger.LogInformation("Resolving conflict: Database wins strategy applied");
            return dbData;
        }

        /// <summary>
        /// Merge resolution - combine both versions
        /// </summary>
        private async Task<Dictionary<string, object>> MergeResolution(
            Dictionary<string, object> cacheData,
            Dictionary<string, object> dbData)
        {
            _logger.LogInformation("Resolving conflict: Merge strategy applied");

            var merged = new Dictionary<string, object>();

            // Add all keys from both dictionaries
            foreach (var key in cacheData.Keys.Union(dbData.Keys))
            {
                if (cacheData.ContainsKey(key) && dbData.ContainsKey(key))
                {
                    // Key exists in both - prefer cache value
                    merged[key] = cacheData[key];
                }
                else if (cacheData.ContainsKey(key))
                {
                    merged[key] = cacheData[key];
                }
                else
                {
                    merged[key] = dbData[key];
                }
            }

            return merged;
        }
    }
}
