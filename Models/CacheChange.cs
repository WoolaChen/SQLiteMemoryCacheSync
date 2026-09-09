namespace SQLiteMemoryCacheSync.Models
{
    /// <summary>
    /// Represents a tracked change in the cache
    /// </summary>
    public class CacheChange
    {
        public int Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public OperationType OperationType { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public bool SyncedToDb { get; set; }
        public DateTime? SyncedAt { get; set; }
    }
}
