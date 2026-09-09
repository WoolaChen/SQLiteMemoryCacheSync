namespace SQLiteMemoryCacheSync.Models
{
    /// <summary>
    /// Configuration settings for the cache manager
    /// </summary>
    public class CacheSettings
    {
        public int SyncIntervalSeconds { get; set; } = 5;
        public int BatchSizeForSync { get; set; } = 100;
        public ConflictResolutionStrategy ConflictResolutionStrategy { get; set; } = ConflictResolutionStrategy.CacheWins;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;
    }
}
