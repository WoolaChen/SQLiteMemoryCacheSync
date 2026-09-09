namespace SQLiteMemoryCacheSync.Models
{
    /// <summary>
    /// Represents the result of a sync operation
    /// </summary>
    public class SyncResult
    {
        public SyncStatus Status { get; set; }
        public int TotalChanges { get; set; }
        public int SuccessfulSyncs { get; set; }
        public int FailedSyncs { get; set; }
        public DateTime SyncStartTime { get; set; }
        public DateTime SyncEndTime { get; set; }
        public TimeSpan Duration => SyncEndTime - SyncStartTime;
        public List<string> Errors { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }
}
