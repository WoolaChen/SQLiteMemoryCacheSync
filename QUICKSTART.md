# Quick Start Guide

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 / Visual Studio Code / JetBrains Rider (optional)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/WoolaChen/SQLiteMemoryCacheSync.git
cd SQLiteMemoryCacheSync
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Project

```bash
dotnet build
```

### 4. Run the Application

```bash
dotnet run
```

You should see output similar to:

```
info: SQLiteMemoryCacheSync.CacheSyncHostedService[0]
      Cache Sync Service starting...
info: SQLiteMemoryCacheSync.Services.PersistentSQLiteDatabase[0]
      Persistent SQLite database connection established
info: SQLiteMemoryCacheSync.Services.MemoryCacheManager[0]
      In-memory SQLite cache initialized successfully
info: SQLiteMemoryCacheSync.Services.SyncService[0]
      Starting sync service with interval of 5 seconds

=== Running Example Operations ===

1. Inserting a user into cache...
✓ User inserted

2. Querying cache for users...
✓ Found 1 user(s)

3. Updating user in cache...
✓ User updated

4. Inserting an order into cache...
✓ Order inserted

5. Getting tracked changes...
✓ Total changes: 4
  - Insert on users (ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
  - Update on users (ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
  - Insert on orders (ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
  - Insert on orders (ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)

6. Cache Statistics...
  - TotalChanges: 4
  - PendingChanges: 4
  - SyncedChanges: 0
  - LastChangeTime: 2026-09-10 00:35:00

7. Performing manual sync...
✓ Sync Status: Success
  - Total Changes: 4
  - Successful: 4
  - Failed: 0
  - Duration: 125.45ms
  - Summary: Synced 4/4 changes in 125.45ms

8. Last Sync Result...
  - Status: Success
  - Synced at: 2026-09-10T00:35:30.1234567Z

9. Waiting for periodic sync cycles...
  Sync Status: Idle
  Sync Status: Idle
  Sync Status: Idle

=== Examples Completed ===
```

## Configuration

Edit `appsettings.json` to customize settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "CacheSettings": {
    "SyncIntervalSeconds": 5,           // Sync every 5 seconds
    "BatchSizeForSync": 100,            // Process 100 changes per batch
    "ConflictResolutionStrategy": "CacheWins",  // Options: CacheWins, DbWins, Merge, ThrowException
    "MaxRetryAttempts": 3,              // Retry failed syncs 3 times
    "RetryDelayMilliseconds": 1000      // Wait 1 second between retries
  },
  "PersistentDatabase": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=persistent_cache.db;Version=3;"
  }
}
```

### Conflict Resolution Strategies

- **CacheWins**: In-memory cache values overwrite database values
- **DbWins**: Database values take precedence, cache changes discarded
- **Merge**: Intelligently combines cache and database values
- **ThrowException**: Throws exception on conflicts

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter ClassName=MemoryCacheManagerTests
```

## Project Structure

```
SQLiteMemoryCacheSync/
├── Models/                          # Data models and enums
│   ├── Enums.cs                    # OperationType, ConflictResolutionStrategy, SyncStatus
│   ├── CacheChange.cs              # Represents tracked changes
│   ├── SyncResult.cs               # Result of sync operations
│   └── CacheSettings.cs            # Configuration settings
├── Interfaces/                      # Service contracts
│   ├── IMemoryCacheManager.cs       # Cache operations
│   ├── IPersistentDatabase.cs       # Database operations
│   ├── ISyncService.cs              # Synchronization
│   └── IConflictResolver.cs         # Conflict resolution
├── Services/                        # Implementation
│   ├── MemoryCacheManager.cs        # In-memory cache
│   ├── PersistentSQLiteDatabase.cs  # Persistent DB
│   ├── SyncService.cs               # Sync logic
│   └── ConflictResolver.cs          # Conflict handling
├── Tests/                           # Unit tests
│   ├── MemoryCacheManagerTests.cs
│   ├── ConflictResolverTests.cs
│   ├── SyncServiceTests.cs
│   └── SQLiteMemoryCacheSync.Tests.csproj
├── Program.cs                       # Application entry point
├── appsettings.json                 # Configuration
├── SQLiteMemoryCacheSync.csproj     # Project file
├── README.md                        # Full documentation
├── QUICKSTART.md                    # This file
└── .gitignore                       # Git ignore rules
```

## Common Usage Patterns

### 1. Dependency Injection Setup

```csharp
var services = new ServiceCollection();
var settings = new CacheSettings { SyncIntervalSeconds = 5 };

services.AddSingleton(settings);
services.AddSingleton<IPersistentDatabase, PersistentSQLiteDatabase>();
services.AddSingleton<IMemoryCacheManager, MemoryCacheManager>();
services.AddSingleton<IConflictResolver, ConflictResolver>();
services.AddSingleton<ISyncService, SyncService>();

var provider = services.BuildServiceProvider();
```

### 2. Insert a Record

```csharp
var cacheManager = provider.GetRequiredService<IMemoryCacheManager>();
await cacheManager.InitializeAsync();

await cacheManager.InsertAsync("users", new Dictionary<string, object>
{
    { "id", "user-123" },
    { "name", "John Doe" },
    { "email", "john@example.com" },
    { "created_at", DateTime.UtcNow }
});
```

### 3. Query Records

```csharp
var results = await cacheManager.QueryAsync(
    "SELECT * FROM users WHERE email = @email",
    new Dictionary<string, object> { { "email", "john@example.com" } });

foreach (var row in results)
{
    Console.WriteLine($"Name: {row["name"]}");
}
```

### 4. Update a Record

```csharp
await cacheManager.UpdateAsync("users", "user-123", new Dictionary<string, object>
{
    { "name", "Jane Doe" },
    { "updated_at", DateTime.UtcNow }
});
```

### 5. Delete a Record

```csharp
await cacheManager.DeleteAsync("users", "user-123");
```

### 6. Get Pending Changes

```csharp
var changes = await cacheManager.GetChangesAsync();

foreach (var change in changes)
{
    Console.WriteLine($"{change.OperationType} on {change.TableName}");
    Console.WriteLine($"Record ID: {change.RecordId}");
    Console.WriteLine($"Changed at: {change.ChangedAt}");
}
```

### 7. Start Periodic Sync

```csharp
var syncService = provider.GetRequiredService<ISyncService>();
syncService.Start();

// ... application runs ...

syncService.Stop();
```

### 8. Manual Sync

```csharp
var result = await syncService.SyncAsync();

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Changes: {result.SuccessfulSyncs}/{result.TotalChanges}");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");

if (result.Errors.Any())
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### 9. Monitor Sync Status

```csharp
var status = syncService.GetStatus();
Console.WriteLine($"Current Status: {status}");

var lastResult = syncService.GetLastSyncResult();
if (lastResult != null)
{
    Console.WriteLine($"Last Sync: {lastResult.SyncEndTime}");
    Console.WriteLine($"Summary: {lastResult.Summary}");
}
```

### 10. Get Cache Statistics

```csharp
var stats = await cacheManager.GetStatsAsync();

Console.WriteLine($"Total Changes: {stats["TotalChanges"]}");
Console.WriteLine($"Pending Changes: {stats["PendingChanges"]}");
Console.WriteLine($"Synced Changes: {stats["SyncedChanges"]}");
```

## Troubleshooting

### Issue: Database file not found

**Solution**: Ensure the connection string in `appsettings.json` points to a valid location:

```json
"ConnectionString": "Data Source=persistent_cache.db;Version=3;"
```

### Issue: In-memory cache not syncing

**Solutions**:
1. Check that `SyncIntervalSeconds` is not too large
2. Verify no exceptions in logs
3. Ensure persistent database is initialized
4. Call `syncService.Start()` to begin periodic sync

### Issue: Conflict resolution not working

**Solution**: Verify the `ConflictResolutionStrategy` in `appsettings.json`:

```json
"ConflictResolutionStrategy": "CacheWins"
```

Valid options: `CacheWins`, `DbWins`, `Merge`, `ThrowException`

## Performance Tips

1. **Increase batch size** for faster syncs when handling many changes:
   ```json
   "BatchSizeForSync": 500
   ```

2. **Adjust sync interval** based on your needs:
   ```json
   "SyncIntervalSeconds": 10  // Sync every 10 seconds instead of 5
   ```

3. **Use connection pooling** in persistent database for better performance

4. **Index frequently queried columns** in both cache and persistent DB

## Next Steps

1. **Customize schemas**: Modify table definitions in `PersistentSQLiteDatabase.cs`
2. **Add more conflict strategies**: Extend `ConflictResolver.cs`
3. **Implement custom sync logic**: Override methods in `SyncService.cs`
4. **Add monitoring/metrics**: Extend `SyncResult` and logging
5. **Support multiple databases**: Implement `IPersistentDatabase` for PostgreSQL, MySQL, etc.

## Additional Resources

- [Full Documentation](README.md)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging)

## License

MIT License - See LICENSE file for details

## Support

For issues and questions:
1. Check existing GitHub issues
2. Review the troubleshooting section above
3. Create a new GitHub issue with detailed information
4. Include logs and configuration (without sensitive data)

---

**Happy caching!** 🚀
