using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SQLiteMemoryCacheSync.Interfaces;
using SQLiteMemoryCacheSync.Models;
using SQLiteMemoryCacheSync.Services;

namespace SQLiteMemoryCacheSync
{
    /// <summary>
    /// Main application entry point demonstrating cache sync functionality
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Load configuration
                    var cacheSettings = new CacheSettings();
                    context.Configuration.GetSection("CacheSettings").Bind(cacheSettings);

                    // Register services
                    services.AddSingleton(cacheSettings);
                    services.AddSingleton<IPersistentDatabase, PersistentSQLiteDatabase>();
                    services.AddSingleton<IMemoryCacheManager, MemoryCacheManager>();
                    services.AddSingleton<IConflictResolver, ConflictResolver>();
                    services.AddSingleton<ISyncService, SyncService>();
                    services.AddSingleton<IHostedService, CacheSyncHostedService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            await host.RunAsync();
        }
    }

    /// <summary>
    /// Hosted service for managing cache sync lifecycle
    /// </summary>
    public class CacheSyncHostedService : IHostedService
    {
        private readonly IMemoryCacheManager _cacheManager;
        private readonly IPersistentDatabase _persistentDb;
        private readonly ISyncService _syncService;
        private readonly ILogger<CacheSyncHostedService> _logger;

        public CacheSyncHostedService(
            IMemoryCacheManager cacheManager,
            IPersistentDatabase persistentDb,
            ISyncService syncService,
            ILogger<CacheSyncHostedService> logger)
        {
            _cacheManager = cacheManager;
            _persistentDb = persistentDb;
            _syncService = syncService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache Sync Service starting...");

            try
            {
                // Initialize databases
                await _persistentDb.InitializeAsync();
                await _cacheManager.InitializeAsync();

                // Start sync service
                _syncService.Start();

                // Run example operations
                await RunExamplesAsync();

                _logger.LogInformation("Cache Sync Service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Cache Sync Service");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache Sync Service stopping...");
            _syncService.Stop();
            await _persistentDb.CloseAsync();
            _logger.LogInformation("Cache Sync Service stopped");
        }

        private async Task RunExamplesAsync()
        {
            _logger.LogInformation("\n=== Running Example Operations ===");

            try
            {
                // Example 1: Insert a user
                _logger.LogInformation("\n1. Inserting a user into cache...");
                var userId = Guid.NewGuid().ToString();
                await _cacheManager.InsertAsync("users", new Dictionary<string, object>
                {
                    { "id", userId },
                    { "name", "John Doe" },
                    { "email", "john@example.com" },
                    { "created_at", DateTime.UtcNow },
                    { "updated_at", DateTime.UtcNow }
                });
                _logger.LogInformation("✓ User inserted");

                // Example 2: Query the cache
                _logger.LogInformation("\n2. Querying cache for users...");
                var users = await _cacheManager.QueryAsync("SELECT * FROM users");
                _logger.LogInformation($"✓ Found {users.Count} user(s)");

                // Example 3: Update a record
                _logger.LogInformation("\n3. Updating user in cache...");
                await _cacheManager.UpdateAsync("users", userId, new Dictionary<string, object>
                {
                    { "name", "John Smith" },
                    { "updated_at", DateTime.UtcNow }
                });
                _logger.LogInformation("✓ User updated");

                // Example 4: Insert an order
                _logger.LogInformation("\n4. Inserting an order into cache...");
                var orderId = Guid.NewGuid().ToString();
                await _cacheManager.InsertAsync("orders", new Dictionary<string, object>
                {
                    { "id", orderId },
                    { "user_id", userId },
                    { "amount", 99.99 },
                    { "status", "pending" },
                    { "created_at", DateTime.UtcNow },
                    { "updated_at", DateTime.UtcNow }
                });
                _logger.LogInformation("✓ Order inserted");

                // Example 5: Get cache changes
                _logger.LogInformation("\n5. Getting tracked changes...");
                var changes = await _cacheManager.GetChangesAsync();
                _logger.LogInformation($"✓ Total changes: {changes.Count}");
                foreach (var change in changes)
                {
                    _logger.LogInformation($"  - {change.OperationType} on {change.TableName} (ID: {change.RecordId})");
                }

                // Example 6: Get cache statistics
                _logger.LogInformation("\n6. Cache Statistics...");
                var stats = await _cacheManager.GetStatsAsync();
                foreach (var stat in stats)
                {
                    _logger.LogInformation($"  - {stat.Key}: {stat.Value}");
                }

                // Example 7: Manual sync
                _logger.LogInformation("\n7. Performing manual sync...");
                var syncResult = await _syncService.SyncAsync();
                _logger.LogInformation($"✓ Sync Status: {syncResult.Status}");
                _logger.LogInformation($"  - Total Changes: {syncResult.TotalChanges}");
                _logger.LogInformation($"  - Successful: {syncResult.SuccessfulSyncs}");
                _logger.LogInformation($"  - Failed: {syncResult.FailedSyncs}");
                _logger.LogInformation($"  - Duration: {syncResult.Duration.TotalMilliseconds:F2}ms");
                _logger.LogInformation($"  - Summary: {syncResult.Summary}");

                // Example 8: Display last sync result
                _logger.LogInformation("\n8. Last Sync Result...");
                var lastResult = _syncService.GetLastSyncResult();
                if (lastResult != null)
                {
                    _logger.LogInformation($"  - Status: {lastResult.Status}");
                    _logger.LogInformation($"  - Synced at: {lastResult.SyncEndTime:O}");
                }

                // Wait for a bit to see periodic syncs
                _logger.LogInformation("\n9. Waiting for periodic sync cycles...");
                for (int i = 0; i < 3; i++)
                {
                    await Task.Delay(2000);
                    var status = _syncService.GetStatus();
                    _logger.LogInformation($"  Sync Status: {status}");
                }

                _logger.LogInformation("\n=== Examples Completed ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running examples");
            }
        }
    }
}
