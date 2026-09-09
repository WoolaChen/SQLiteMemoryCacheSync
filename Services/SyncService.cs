using Microsoft.Extensions.Logging;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;
using System.Text.Json;

namespace SQLiteMemoryCacheSync.Services
{
    /// <summary>
    /// Service responsible for synchronizing cache with persistent database
    /// </summary>
    public class SyncService : ISyncService, IDisposable
    {
        private readonly IMemoryCacheManager _cacheManager;
        private readonly IPersistentDatabase _persistentDb;
        private readonly IConflictResolver _conflictResolver;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<SyncService> _logger;
        private SyncStatus _status = SyncStatus.Idle;
        private SyncResult? _lastSyncResult;
        private Timer? _syncTimer;
        private bool _isSyncing = false;
        private readonly object _lockObject = new();

        public SyncService(
            IMemoryCacheManager cacheManager,
            IPersistentDatabase persistentDb,
            IConflictResolver conflictResolver,
            CacheSettings cacheSettings,
            ILogger<SyncService> logger)
        {
            _cacheManager = cacheManager;
            _persistentDb = persistentDb;
            _conflictResolver = conflictResolver;
            _cacheSettings = cacheSettings;
            _logger = logger;
        }

        /// <summary>
        /// Start the periodic sync worker
        /// </summary>
        public void Start()
        {
            if (_syncTimer != null)
            {
                _logger.LogWarning("Sync service already running");
                return;
            }

            _logger.LogInformation($"Starting sync service with interval of {_cacheSettings.SyncIntervalSeconds} seconds");
            _syncTimer = new Timer(
                _ => _ = SyncAsync(),
                null,
                TimeSpan.FromSeconds(_cacheSettings.SyncIntervalSeconds),
                TimeSpan.FromSeconds(_cacheSettings.SyncIntervalSeconds));
        }

        /// <summary>
        /// Stop the periodic sync worker
        /// </summary>
        public void Stop()
        {
            if (_syncTimer != null)
            {
                _syncTimer.Dispose();
                _syncTimer = null;
                _logger.LogInformation("Sync service stopped");
            }
        }

        /// <summary>
        /// Execute a manual sync operation
        /// </summary>
        public async Task<SyncResult> SyncAsync()
        {
            lock (_lockObject)
            {
                if (_isSyncing)
                {
                    _logger.LogWarning("Sync already in progress, skipping this cycle");
                    return _lastSyncResult ?? new SyncResult { Status = SyncStatus.Idle };
                }
                _isSyncing = true;
                _status = SyncStatus.Syncing;
            }

            var result = new SyncResult
            {
                SyncStartTime = DateTime.UtcNow
            };

            try
            {
                // Get all pending changes from cache
                var changes = await _cacheManager.GetChangesAsync();
                result.TotalChanges = changes.Count;

                if (changes.Count == 0)
                {
                    _logger.LogDebug("No changes to sync");
                    result.Status = SyncStatus.Success;
                    return result;
                }

                _logger.LogInformation($"Starting sync of {changes.Count} changes");

                // Process changes in batches
                var batches = changes
                    .Chunk(_cacheSettings.BatchSizeForSync)
                    .ToList();

                var successfulChanges = new List<int>();

                foreach (var batch in batches)
                {
                    await ProcessBatchAsync(batch, result, successfulChanges);
                }

                // Mark synced changes
                await _cacheManager.ClearChangesAsync(successfulChanges);

                result.SuccessfulSyncs = successfulChanges.Count;
                result.FailedSyncs = changes.Count - successfulChanges.Count;
                result.Status = result.FailedSyncs == 0 ? SyncStatus.Success : SyncStatus.PartialSuccess;
                result.Summary = $"Synced {result.SuccessfulSyncs}/{result.TotalChanges} changes in {result.Duration.TotalMilliseconds:F2}ms";

                _logger.LogInformation(result.Summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync operation");
                result.Status = SyncStatus.Failed;
                result.Errors.Add(ex.Message);
            }
            finally
            {
                result.SyncEndTime = DateTime.UtcNow;
                _lastSyncResult = result;

                lock (_lockObject)
                {
                    _isSyncing = false;
                    _status = result.Status == SyncStatus.Failed ? SyncStatus.Failed : SyncStatus.Idle;
                }
            }

            return result;
        }

        /// <summary>
        /// Get the last sync result
        /// </summary>
        public SyncResult? GetLastSyncResult()
        {
            return _lastSyncResult;
        }

        /// <summary>
        /// Get current sync status
        /// </summary>
        public SyncStatus GetStatus()
        {
            return _status;
        }

        // Private helper methods

        private async Task ProcessBatchAsync(
            CacheChange[] batch,
            SyncResult result,
            List<int> successfulChanges)
        {
            using (var transaction = await _persistentDb.BeginTransactionAsync())
            {
                try
                {
                    foreach (var change in batch)
                    {
                        await ProcessChangeAsync(change, transaction, result);
                        successfulChanges.Add(change.Id);
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation($"Batch of {batch.Length} changes committed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch, rolling back");
                    await transaction.RollbackAsync();
                    result.Errors.Add($"Batch error: {ex.Message}");
                }
            }
        }

        private async Task ProcessChangeAsync(
            CacheChange change,
            ITransaction transaction,
            SyncResult result)
        {
            try
            {
                switch (change.OperationType)
                {
                    case OperationType.Insert:
                        await HandleInsertAsync(change, transaction);
                        break;

                    case OperationType.Update:
                        await HandleUpdateAsync(change, transaction);
                        break;

                    case OperationType.Delete:
                        await HandleDeleteAsync(change, transaction);
                        break;
                }

                _logger.LogDebug($"Processed {change.OperationType} for {change.TableName} record {change.RecordId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing change {change.Id}");
                throw;
            }
        }

        private async Task HandleInsertAsync(CacheChange change, ITransaction transaction)
        {
            if (string.IsNullOrEmpty(change.NewValue))
                return;

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(change.NewValue) 
                ?? new Dictionary<string, object>();

            // Check if record already exists
            bool exists = await _persistentDb.RecordExistsAsync(change.TableName, change.RecordId);

            if (exists)
            {
                // Handle conflict
                var existingData = await _persistentDb.QueryAsync(
                    $"SELECT * FROM {change.TableName} WHERE id = @id",
                    new Dictionary<string, object> { { "id", change.RecordId } });

                if (existingData.Count > 0)
                {
                    var resolved = await _conflictResolver.ResolveAsync(
                        change.TableName,
                        data,
                        existingData[0],
                        _cacheSettings.ConflictResolutionStrategy);

                    await HandleUpdateAsync(new CacheChange
                    {
                        TableName = change.TableName,
                        RecordId = change.RecordId,
                        OperationType = OperationType.Update,
                        NewValue = JsonSerializer.Serialize(resolved)
                    }, transaction);
                }
            }
            else
            {
                var columns = string.Join(", ", data.Keys);
                var values = string.Join(", ", data.Keys.Select(k => $"@{k}"));
                var sql = $"INSERT INTO {change.TableName} ({columns}) VALUES ({values})";

                await transaction.ExecuteAsync(sql, data);
            }
        }

        private async Task HandleUpdateAsync(CacheChange change, ITransaction transaction)
        {
            if (string.IsNullOrEmpty(change.NewValue))
                return;

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(change.NewValue)
                ?? new Dictionary<string, object>();

            var setClause = string.Join(", ", data.Keys.Select(k => $"{k} = @{k}"));
            var sql = $"UPDATE {change.TableName} SET {setClause} WHERE id = @id";

            data["id"] = change.RecordId;
            await transaction.ExecuteAsync(sql, data);
        }

        private async Task HandleDeleteAsync(CacheChange change, ITransaction transaction)
        {
            var sql = $"DELETE FROM {change.TableName} WHERE id = @id";
            await transaction.ExecuteAsync(sql, new Dictionary<string, object> { { "id", change.RecordId } });
        }

        public void Dispose()
        {
            Stop();
            _syncTimer?.Dispose();
        }
    }
}
