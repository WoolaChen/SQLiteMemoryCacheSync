using System.Data;

namespace SQLiteMemoryCacheSync.Interfaces
{
    /// <summary>
    /// Interface for in-memory cache operations
    /// </summary>
    public interface IMemoryCacheManager
    {
        /// <summary>
        /// Initialize cache from persistent database
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Execute a query against the cache
        /// </summary>
        Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Execute a scalar query
        /// </summary>
        Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Insert a record into cache
        /// </summary>
        Task<int> InsertAsync(string tableName, Dictionary<string, object> data);

        /// <summary>
        /// Update a record in cache
        /// </summary>
        Task<int> UpdateAsync(string tableName, string recordId, Dictionary<string, object> data);

        /// <summary>
        /// Delete a record from cache
        /// </summary>
        Task<int> DeleteAsync(string tableName, string recordId);

        /// <summary>
        /// Get all tracked changes
        /// </summary>
        Task<List<CacheChange>> GetChangesAsync();

        /// <summary>
        /// Clear tracked changes after successful sync
        /// </summary>
        Task ClearChangesAsync(List<int> changeIds);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        Task<Dictionary<string, object>> GetStatsAsync();
    }
}
