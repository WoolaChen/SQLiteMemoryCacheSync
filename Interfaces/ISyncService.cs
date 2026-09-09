using SQLiteMemoryCacheSync.Models;

namespace SQLiteMemoryCacheSync.Interfaces
{
    /// <summary>
    /// Interface for synchronization logic
    /// </summary>
    public interface ISyncService
    {
        /// <summary>
        /// Start the sync worker
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the sync worker
        /// </summary>
        void Stop();

        /// <summary>
        /// Execute a manual sync
        /// </summary>
        Task<SyncResult> SyncAsync();

        /// <summary>
        /// Get the last sync result
        /// </summary>
        SyncResult? GetLastSyncResult();

        /// <summary>
        /// Get sync status
        /// </summary>
        SyncStatus GetStatus();
    }
}
