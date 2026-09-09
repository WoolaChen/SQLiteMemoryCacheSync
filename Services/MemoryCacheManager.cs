using System.Data.SQLite;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;

namespace SQLiteMemoryCacheSync.Services
{
    /// <summary>
    /// Implementation of in-memory SQLite cache manager
    /// </summary>
    public class MemoryCacheManager : IMemoryCacheManager
    {
        private SQLiteConnection? _connection;
        private List<CacheChange> _changes = new();
        private readonly ILogger<MemoryCacheManager> _logger;
        private readonly IPersistentDatabase _persistentDb;
        private readonly object _lockObject = new();
        private int _changeIdCounter = 0;

        public MemoryCacheManager(ILogger<MemoryCacheManager> logger, IPersistentDatabase persistentDb)
        {
            _logger = logger;
            _persistentDb = persistentDb;
        }

        /// <summary>
        /// Initialize in-memory SQLite cache
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _connection = new SQLiteConnection("Data Source=:memory:;Version=3;");
                _connection.Open();
                _logger.LogInformation("In-memory SQLite cache initialized successfully");

                // Create tracking table
                await CreateTrackingTableAsync();
                await LoadInitialDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing in-memory cache");
                throw;
            }
        }

        /// <summary>
        /// Execute a query against the cache
        /// </summary>
        public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null)
                throw new InvalidOperationException("Cache not initialized");

            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = sql;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                        }
                    }

                    using (var reader = await Task.Run(() => command.ExecuteReader()))
                    {
                        var results = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            results.Add(row);
                        }

                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Execute a scalar query
        /// </summary>
        public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null)
                throw new InvalidOperationException("Cache not initialized");

            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = sql;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                        }
                    }

                    return await Task.Run(() => command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing scalar query: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Insert a record into cache and track the change
        /// </summary>
        public async Task<int> InsertAsync(string tableName, Dictionary<string, object> data)
        {
            if (_connection == null)
                throw new InvalidOperationException("Cache not initialized");

            lock (_lockObject)
            {
                try
                {
                    var columns = string.Join(", ", data.Keys);
                    var values = string.Join(", ", data.Keys.Select(k => $"@{k}"));
                    var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = sql;

                        foreach (var param in data)
                        {
                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                        }

                        int result = command.ExecuteNonQuery();

                        // Track this change
                        string recordId = data.ContainsKey("id") ? data["id"].ToString() ?? "" : Guid.NewGuid().ToString();
                        TrackChange(tableName, OperationType.Insert, recordId, null, JsonSerializer.Serialize(data));

                        _logger.LogInformation($"Inserted record into {tableName} cache");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error inserting into {tableName}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Update a record in cache and track the change
        /// </summary>
        public async Task<int> UpdateAsync(string tableName, string recordId, Dictionary<string, object> data)
        {
            if (_connection == null)
                throw new InvalidOperationException("Cache not initialized");

            lock (_lockObject)
            {
                try
                {
                    // Get old value for tracking
                    var oldData = QueryAsync($"SELECT * FROM {tableName} WHERE id = @id", new Dictionary<string, object> { { "id", recordId } }).Result.FirstOrDefault();
                    string? oldValue = oldData != null ? JsonSerializer.Serialize(oldData) : null;

                    var setClause = string.Join(", ", data.Keys.Select(k => $"{k} = @{k}"));
                    var sql = $"UPDATE {tableName} SET {setClause} WHERE id = @id";

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.Parameters.AddWithValue("@id", recordId);

                        foreach (var param in data)
                        {
                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                        }

                        int result = command.ExecuteNonQuery();

                        // Track this change
                        TrackChange(tableName, OperationType.Update, recordId, oldValue, JsonSerializer.Serialize(data));

                        _logger.LogInformation($"Updated record in {tableName} cache");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating {tableName}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Delete a record from cache and track the change
        /// </summary>
        public async Task<int> DeleteAsync(string tableName, string recordId)
        {
            if (_connection == null)
                throw new InvalidOperationException("Cache not initialized");

            lock (_lockObject)
            {
                try
                {
                    // Get old value for tracking
                    var oldData = QueryAsync($"SELECT * FROM {tableName} WHERE id = @id", new Dictionary<string, object> { { "id", recordId } }).Result.FirstOrDefault();
                    string? oldValue = oldData != null ? JsonSerializer.Serialize(oldData) : null;

                    var sql = $"DELETE FROM {tableName} WHERE id = @id";

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.Parameters.AddWithValue("@id", recordId);

                        int result = command.ExecuteNonQuery();

                        // Track this change
                        TrackChange(tableName, OperationType.Delete, recordId, oldValue, null);

                        _logger.LogInformation($"Deleted record from {tableName} cache");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting from {tableName}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Get all tracked changes
        /// </summary>
        public async Task<List<CacheChange>> GetChangesAsync()
        {
            lock (_lockObject)
            {
                return _changes.Where(c => !c.SyncedToDb).ToList();
            }
        }

        /// <summary>
        /// Clear tracked changes after successful sync
        /// </summary>
        public async Task ClearChangesAsync(List<int> changeIds)
        {
            lock (_lockObject)
            {
                foreach (var id in changeIds)
                {
                    var change = _changes.FirstOrDefault(c => c.Id == id);
                    if (change != null)
                    {
                        change.SyncedToDb = true;
                        change.SyncedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatsAsync()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, object>
                {
                    { "TotalChanges", _changes.Count },
                    { "PendingChanges", _changes.Count(c => !c.SyncedToDb) },
                    { "SyncedChanges", _changes.Count(c => c.SyncedToDb) },
                    { "LastChangeTime", _changes.LastOrDefault()?.ChangedAt ?? DateTime.MinValue }
                };
            }
        }

        // Private helper methods

        private async Task CreateTrackingTableAsync()
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection not initialized");

            var sql = @"
                CREATE TABLE IF NOT EXISTS cache_changes (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    table_name TEXT NOT NULL,
                    operation_type TEXT NOT NULL,
                    record_id TEXT NOT NULL,
                    old_value TEXT,
                    new_value TEXT,
                    changed_at DATETIME NOT NULL,
                    synced_to_db INTEGER NOT NULL DEFAULT 0,
                    synced_at DATETIME
                )
            ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                await Task.Run(() => command.ExecuteNonQuery());
            }
        }

        private async Task LoadInitialDataAsync()
        {
            // This would load data from persistent DB if needed
            _logger.LogInformation("Initial data loading complete");
        }

        private void TrackChange(string tableName, OperationType operationType, string recordId, string? oldValue, string? newValue)
        {
            var change = new CacheChange
            {
                Id = ++_changeIdCounter,
                TableName = tableName,
                OperationType = operationType,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = DateTime.UtcNow,
                SyncedToDb = false
            };

            _changes.Add(change);
            _logger.LogDebug($"Change tracked: {operationType} on {tableName} record {recordId}");
        }
    }
}
