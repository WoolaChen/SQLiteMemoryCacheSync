using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SQLiteMemoryCacheSync.Interfaces;

namespace SQLiteMemoryCacheSync.Services
{
    /// <summary>
    /// Implementation for persistent SQLite database
    /// </summary>
    public class PersistentSQLiteDatabase : IPersistentDatabase
    {
        private SQLiteConnection? _connection;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PersistentSQLiteDatabase> _logger;
        private readonly string _connectionString;

        public PersistentSQLiteDatabase(IConfiguration configuration, ILogger<PersistentSQLiteDatabase> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("PersistentDatabase") 
                ?? "Data Source=persistent_cache.db;Version=3;";
        }

        public async Task InitializeAsync()
        {
            try
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();
                _logger.LogInformation("Persistent SQLite database connection established");
                await CreateTablesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing persistent database");
                throw;
            }
        }

        public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null)
                throw new InvalidOperationException("Database not initialized");

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

        public async Task<int> ExecuteAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (_connection == null)
                throw new InvalidOperationException("Database not initialized");

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

                    return await Task.Run(() => command.ExecuteNonQuery());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing: {sql}");
                throw;
            }
        }

        public async Task<ITransaction> BeginTransactionAsync()
        {
            if (_connection == null)
                throw new InvalidOperationException("Database not initialized");

            var transaction = _connection.BeginTransaction();
            return new SQLiteTransaction(transaction, _logger);
        }

        public async Task<bool> RecordExistsAsync(string tableName, string recordId)
        {
            var result = await QueryAsync(
                $"SELECT COUNT(*) as count FROM {tableName} WHERE id = @id",
                new Dictionary<string, object> { { "id", recordId } });

            return result.Count > 0 && (long)result[0]["count"] > 0;
        }

        public async Task CloseAsync()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _logger.LogInformation("Persistent database connection closed");
            }
        }

        private async Task CreateTablesAsync()
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection not initialized");

            // Create sample tables - customize based on your needs
            var sql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT UNIQUE NOT NULL,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME NOT NULL
                );

                CREATE TABLE IF NOT EXISTS orders (
                    id TEXT PRIMARY KEY,
                    user_id TEXT NOT NULL,
                    amount REAL NOT NULL,
                    status TEXT NOT NULL,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES users(id)
                );

                CREATE TABLE IF NOT EXISTS sync_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sync_start DATETIME NOT NULL,
                    sync_end DATETIME NOT NULL,
                    changes_applied INTEGER NOT NULL,
                    status TEXT NOT NULL
                );
            ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                await Task.Run(() => command.ExecuteNonQuery());
            }

            _logger.LogInformation("Database tables created successfully");
        }
    }

    /// <summary>
    /// Implementation of database transaction
    /// </summary>
    public class SQLiteTransaction : ITransaction
    {
        private readonly System.Data.SQLite.SQLiteTransaction _transaction;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public SQLiteTransaction(System.Data.SQLite.SQLiteTransaction transaction, ILogger logger)
        {
            _transaction = transaction;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using (var command = _transaction.Connection!.CreateCommand())
                {
                    command.Transaction = _transaction;
                    command.CommandText = sql;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                        }
                    }

                    return await Task.Run(() => command.ExecuteNonQuery());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing in transaction: {sql}");
                throw;
            }
        }

        public async Task CommitAsync()
        {
            _transaction.Commit();
            _logger.LogDebug("Transaction committed");
        }

        public async Task RollbackAsync()
        {
            _transaction.Rollback();
            _logger.LogDebug("Transaction rolled back");
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _disposed = true;
            }
        }
    }
}
