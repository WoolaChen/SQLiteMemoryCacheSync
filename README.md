# SQLite In-Memory Cache with Periodic Sync

A complete C# implementation demonstrating in-memory SQLite caching with periodic synchronization to a persistent database.

## Features

- **In-Memory SQLite Cache**: Ultra-fast read/write operations using SQLite in-memory database
- **Change Tracking**: Automatic tracking of all INSERT, UPDATE, and DELETE operations
- **Periodic Synchronization**: Background sync worker that pushes changes to persistent database
- **Conflict Resolution**: Multiple strategies for handling conflicts between cache and database
- **Transaction Support**: ACID compliance with transaction support
- **Dependency Injection**: Full DI integration with Microsoft.Extensions
- **Logging**: Comprehensive logging for debugging and monitoring
- **Batch Processing**: Efficient batch processing of changes during sync

## Architecture

```
┌─────────────────────────────┐
│   Application Layer         │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│  Cache Manager Service      │
│  - Fast Read/Write          │
│  - Change Tracking          │
└──────────────┬──────────────┘
               │
      ┌────────┴────────┐
      │                 │
┌─────▼──────┐    ┌────▼──────────┐
│  In-Memory │    │  Persistent   │
│  SQLite    │    │  Database     │
│  (Cache)   │    │  (SQL Server/ │
│            │    │   SQLite)     │
└────────────┘    └───────────────┘
      ▲                    ▲
      └────────┬───────────┘
           Sync Service
       (Periodic Sync Worker)
```

## Project Structure

```
SQLiteMemoryCacheSync/
├── Models/
│   ├── Enums.cs                    # OperationType, ConflictResolutionStrategy, SyncStatus
│   ├── CacheChange.cs              # Represents a tracked change
│   ├── SyncResult.cs               # Result of a sync operation
│   └── CacheSettings.cs            # Configuration settings
├── Interfaces/
│   ├── IMemoryCacheManager.cs      # Cache operations contract
│   ├── IPersistentDatabase.cs      # Database operations contract
│   ├── ISyncService.cs             # Sync service contract
│   └── IConflictResolver.cs        # Conflict resolution contract
├── Services/
│   ├── MemoryCacheManager.cs       # In-memory cache implementation
│   ├── PersistentSQLiteDatabase.cs # Persistent DB implementation
│   ├── SyncService.cs              # Sync logic implementation
│   └── ConflictResolver.cs         # Conflict resolution logic
├── Program.cs                       # Application entry point
├── appsettings.json                # Configuration file
└── SQLiteMemoryCacheSync.csproj    # Project file
```

## Configuration

### appsettings.json

```json
{
  "CacheSettings": {
    "SyncIntervalSeconds": 5,
    "BatchSizeForSync": 100,
    "ConflictResolutionStrategy": "CacheWins",
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 1000
  },
  "PersistentDatabase": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=persistent_cache.db;Version=3;"
  }
}
```

## Usage

### 1. Basic Insert Operation

```csharp
var cacheManager = serviceProvider.GetRequiredService<IMemoryCacheManager>();

await cacheManager.InsertAsync("users", new Dictionary<string, object>
{
    { "id", "user-123" },
    { "name", "John Doe" },
    { "email", "john@example.com" },
    { "created_at", DateTime.UtcNow }
});
```

### 2. Query Cache

```csharp
var results = await cacheManager.QueryAsync("SELECT * FROM users WHERE email = @email",
    new Dictionary<string, object>
    {
        { "email", "john@example.com" }
    });
```

### 3. Update Record

```csharp
await cacheManager.UpdateAsync("users", "user-123", new Dictionary<string, object>
{
    { "name", "Jane Doe" },
    { "updated_at", DateTime.UtcNow }
});
```

### 4. Delete Record

```csharp
await cacheManager.DeleteAsync("users", "user-123");
```

### 5. Manual Sync

```csharp
var syncService = serviceProvider.GetRequiredService<ISyncService>();
var result = await syncService.SyncAsync();

Console.WriteLine($"Sync Status: {result.Status}");
Console.WriteLine($"Changes Synced: {result.SuccessfulSyncs}/{result.TotalChanges}");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
```

### 6. Start/Stop Sync Service

```csharp
var syncService = serviceProvider.GetRequiredService<ISyncService>();
syncService.Start();  // Start periodic sync

// ... application runs ...

syncService.Stop();   // Stop periodic sync
```

## Conflict Resolution Strategies

### CacheWins
Use the in-memory cache value and overwrite the database.

```csharp
cacheSettings.ConflictResolutionStrategy = ConflictResolutionStrategy.CacheWins;
```

### DbWins
Discard cache changes and keep the database value.

```csharp
cacheSettings.ConflictResolutionStrategy = ConflictResolutionStrategy.DbWins;
```

### Merge
Combine both values intelligently.

```csharp
cacheSettings.ConflictResolutionStrategy = ConflictResolutionStrategy.Merge;
```

### ThrowException
Throw an exception when a conflict is detected.

```csharp
cacheSettings.ConflictResolutionStrategy = ConflictResolutionStrategy.ThrowException;
```

## Key Components

### IMemoryCacheManager
Manages in-memory SQLite operations:
- `InitializeAsync()` - Initialize cache
- `QueryAsync()` - Execute SELECT queries
- `InsertAsync()` - Insert records
- `UpdateAsync()` - Update records
- `DeleteAsync()` - Delete records
- `GetChangesAsync()` - Retrieve tracked changes
- `ClearChangesAsync()` - Mark changes as synced
- `GetStatsAsync()` - Get cache statistics

### IPersistentDatabase
Manages persistent database operations:
- `InitializeAsync()` - Initialize connection
- `QueryAsync()` - Execute queries
- `ExecuteAsync()` - Execute non-queries
- `BeginTransactionAsync()` - Start transaction
- `RecordExistsAsync()` - Check record existence
- `CloseAsync()` - Close connection

### ISyncService
Manages synchronization:
- `Start()` - Start periodic sync
- `Stop()` - Stop periodic sync
- `SyncAsync()` - Manual sync
- `GetLastSyncResult()` - Get last sync result
- `GetStatus()` - Get current status

### IConflictResolver
Resolves conflicts between cache and database:
- `ResolveAsync()` - Resolve conflict with specified strategy

## Performance Considerations

1. **Batch Processing**: Changes are processed in batches to reduce transaction overhead
2. **Connection Pooling**: Persistent database uses connection pooling
3. **Async/Await**: All I/O operations are asynchronous
4. **Locking**: Thread-safe change tracking with minimal lock contention
5. **Lazy Sync**: Sync only when there are pending changes

## Error Handling

The service includes comprehensive error handling:
- Transaction rollback on failures
- Retry logic with exponential backoff (configurable)
- Detailed error logging
- Partial success reporting (some changes synced, some failed)

## Monitoring

Get sync statistics and status:

```csharp
// Get current status
var status = syncService.GetStatus();

// Get last sync result
var result = syncService.GetLastSyncResult();
if (result != null)
{
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
    Console.WriteLine($"Summary: {result.Summary}");
    if (result.Errors.Count > 0)
    {
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"Error: {error}");
        }
    }
}

// Get cache statistics
var stats = await cacheManager.GetStatsAsync();
Console.WriteLine($"Total Changes: {stats["TotalChanges"]}");
Console.WriteLine($"Pending Changes: {stats["PendingChanges"]}");
```

## Building and Running

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

## Testing

The project includes examples that demonstrate:
- Inserting records into cache
- Querying cached data
- Updating records
- Tracking changes
- Manual sync operations
- Periodic sync cycles

## Future Enhancements

- [ ] Support for multiple persistent database providers (PostgreSQL, MySQL, SQL Server)
- [ ] Advanced change batching strategies
- [ ] Real-time sync notifications
- [ ] Cache size management and eviction policies
- [ ] Distributed cache coordination
- [ ] Metrics and health checks
- [ ] Unit tests suite

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
