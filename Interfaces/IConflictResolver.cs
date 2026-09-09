using SQLiteMemoryCacheSync.Models;

namespace SQLiteMemoryCacheSync.Interfaces
{
    /// <summary>
    /// Interface for conflict resolution
    /// </summary>
    public interface IConflictResolver
    {
        /// <summary>
        /// Resolve conflict between cache and database
        /// </summary>
        Task<Dictionary<string, object>> ResolveAsync(
            string tableName,
            Dictionary<string, object> cacheData,
            Dictionary<string, object> dbData,
            ConflictResolutionStrategy strategy);
    }
}
