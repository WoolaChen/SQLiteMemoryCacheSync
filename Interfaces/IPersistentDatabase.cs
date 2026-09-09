using SQLiteMemoryCacheSync.Models;

namespace SQLiteMemoryCacheSync.Interfaces
{
    /// <summary>
    /// Interface for persistent database operations
    /// </summary>
    public interface IPersistentDatabase
    {
        /// <summary>
        /// Initialize database connection
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Execute a query against the persistent database
        /// </summary>
        Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Execute a non-query command
        /// </summary>
        Task<int> ExecuteAsync(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Begin a transaction
        /// </summary>
        Task<ITransaction> BeginTransactionAsync();

        /// <summary>
        /// Check if record exists in database
        /// </summary>
        Task<bool> RecordExistsAsync(string tableName, string recordId);

        /// <summary>
        /// Close connection
        /// </summary>
        Task CloseAsync();
    }

    /// <summary>
    /// Interface for database transactions
    /// </summary>
    public interface ITransaction : IAsyncDisposable
    {
        /// <summary>
        /// Execute a query within transaction
        /// </summary>
        Task<int> ExecuteAsync(string sql, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Commit the transaction
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rollback the transaction
        /// </summary>
        Task RollbackAsync();
    }
}
