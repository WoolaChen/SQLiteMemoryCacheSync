namespace SQLiteMemoryCacheSync.Models
{
    /// <summary>
    /// Enum representing different types of database operations
    /// </summary>
    public enum OperationType
    {
        Insert,
        Update,
        Delete
    }

    /// <summary>
    /// Enum representing conflict resolution strategies
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// Cache wins - overwrite persistent DB with cache data
        /// </summary>
        CacheWins,

        /// <summary>
        /// Database wins - discard cache changes and keep DB data
        /// </summary>
        DbWins,

        /// <summary>
        /// Custom merge logic - application decides how to merge
        /// </summary>
        Merge,

        /// <summary>
        /// Throw exception on conflict
        /// </summary>
        ThrowException
    }

    /// <summary>
    /// Enum representing sync status
    /// </summary>
    public enum SyncStatus
    {
        Idle,
        Syncing,
        Success,
        Failed,
        PartialSuccess
    }
}
