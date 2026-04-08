using System.Net;
using System.Threading;
using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Provides repository implementation for items that belong to a tenant.
/// </summary>
/// <typeparam name="T">Domain model type managed by the repository</typeparam>
/// <param name="options">Configuration options for the repository</param>
/// <param name="client">Instance of the cosmos db client. NOTE: for performance reasons, this should probably be a singleton in the application.</param>
public class CosmosTenantItemRepository<T>(
        IOptions<CosmosRepositoryOptions<T>> options,
        CosmosClient client,
        ILogger<CosmosTenantItemRepository<T>> logger) :
    CosmosRepository<T>(options, client, logger), ITenantItemRepository<T>
    where T : class, ITenantItem, new()
{
    /// <summary>
    /// Get all items in the repository that have the specified tenant id.
    /// Default implementation will return items in descending order by timestamp.
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <returns></returns>
    public async Task<IEnumerable<T>> GetAllAsync(string tenantId)
    {
        var container = await GetContainerAsync();

        var queryContext = await GetQueryContextAsync(tenantId);

        var query = queryContext.Queryable.OrderByDescending(x => x.Timestamp);

        var results = await GetResultsAsync(query,
            GetQueryDescription(nameof(GetAllAsync)), queryContext.PartitionKey);

        return results;
    }

    /// <summary>
    /// Gets an entity by its id and tenant id.
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="id"></param>
    /// <returns>Matching item or null if not found</returns>
    public virtual async Task<T?> GetByIdAsync(string tenantId, string id)
    {
        if (string.IsNullOrWhiteSpace(tenantId) == true ||
            string.IsNullOrWhiteSpace(id) == true)
        {
            return null;
        }

        try
        {
            var container = await GetContainerAsync();

            var pk = new PartitionKeyBuilder().Add(tenantId).Add(EntityType).Build();

            ItemResponse<T?>? response;

            try
            {
                response = await container.ReadItemAsync<T?>(id, pk);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogDebug($"{nameof(GetByIdAsync)}() -- Not found. Partition key: {pk}, Id: {id}");

                return null;
            }

            LogPointOperationDiagnostics(nameof(GetByIdAsync), response.RequestCharge, response.Diagnostics);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var item = response.Resource;

            if (item == null)
            {
                return null;
            }
            else
            {
                return item;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetByIdAsync)}().");

            throw;
        }
    }

    /// <summary>
    /// Delete an item from the repository.
    /// </summary>
    /// <param name="itemToDelete"></param>
    /// <returns></returns>
    public async Task DeleteAsync(T itemToDelete)
    {
        var container = await GetContainerAsync();

        var builder = new PartitionKeyBuilder();

        _ = builder.Add(itemToDelete.TenantId);
        _ = builder.Add(itemToDelete.EntityType);

        var partitionKey = builder.Build();

        ItemResponse<T> response;
        try
        {
            response = await container.DeleteItemAsync<T>(itemToDelete.Id, partitionKey);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets a page of results for the specified tenant with continuation support.
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <param name="pageSize">Maximum number of items to return</param>
    /// <param name="continuationToken">Continuation token from previous query (null for first page)</param>
    /// <returns>A page of results with continuation information</returns>
    public new async Task<PagedResults<T>> GetPagedAsync(string tenantId, int pageSize = 100, string? continuationToken = null)
    {
        return await base.GetPagedAsync(tenantId, pageSize, continuationToken);
    }

    #region Bulk Operation Settings

    /// <summary>
    /// Maximum concurrent operations for bulk operations. Override in derived class if needed.
    /// </summary>
    protected virtual int BulkMaxConcurrency => 10;

    /// <summary>
    /// Maximum retry attempts for throttled requests. Override in derived class if needed.
    /// </summary>
    protected virtual int BulkMaxRetries => 5;

    #endregion

    #region Bulk Delete Operations

    /// <summary>
    /// Deletes all items for the specified tenant with throttling and retry logic.
    /// </summary>
    /// <param name="tenantId">Tenant id for the items to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAllByTenantIdAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await DeleteAllByTenantIdAsync(tenantId, BulkMaxConcurrency, BulkMaxRetries, cancellationToken);
    }

    /// <summary>
    /// Deletes all items for the specified tenant with configurable throttling and retry logic.
    /// </summary>
    /// <param name="tenantId">Tenant id for the items to delete</param>
    /// <param name="maxConcurrency">Maximum number of concurrent delete operations</param>
    /// <param name="maxRetries">Maximum number of retry attempts for throttled requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAllByTenantIdAsync(
        string tenantId,
        int maxConcurrency,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(tenantId);
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            Logger.LogDebug("No items to delete for tenant {TenantId}", tenantId);
            return;
        }

        Logger.LogInformation(
            "Deleting {Count} items for tenant {TenantId} with max concurrency {MaxConcurrency}",
            itemList.Count, tenantId, maxConcurrency);

        var failedItems = new List<(T Item, Exception Exception)>();

        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = itemList.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await DeleteWithRetryAsync(item, maxRetries, cancellationToken);
            }
            catch (Exception ex)
            {
                lock (failedItems)
                {
                    failedItems.Add((item, ex));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (failedItems.Count > 0)
        {
            Logger.LogError(
                "Failed to delete {FailedCount} of {TotalCount} items for tenant {TenantId}",
                failedItems.Count, itemList.Count, tenantId);

            throw new AggregateException(
                $"Failed to delete {failedItems.Count} items",
                failedItems.Select(f => f.Exception));
        }

        Logger.LogInformation(
            "Successfully deleted {Count} items for tenant {TenantId}",
            itemList.Count, tenantId);
    }

    /// <summary>
    /// Deletes a single item with retry logic for throttled requests (HTTP 429).
    /// </summary>
    /// <param name="item">Item to delete</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task DeleteWithRetryAsync(
        T item,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await DeleteAsync(item);
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    Logger.LogWarning(
                        "Delete failed after {MaxRetries} retries due to throttling", maxRetries);
                    throw;
                }

                var delay = ex.RetryAfter ?? TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));

                Logger.LogDebug(
                    "Throttled on delete attempt {Attempt}, waiting {DelayMs}ms before retry",
                    attempt + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    #endregion

    #region Bulk Save Operations

    /// <summary>
    /// Saves multiple items with throttling and retry logic.
    /// </summary>
    /// <param name="items">Items to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SaveAllAsync(
        IEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        await SaveAllAsync(items, BulkMaxConcurrency, BulkMaxRetries, cancellationToken);
    }

    /// <summary>
    /// Saves multiple items with configurable throttling and retry logic.
    /// </summary>
    /// <param name="items">Items to save</param>
    /// <param name="maxConcurrency">Maximum number of concurrent save operations</param>
    /// <param name="maxRetries">Maximum number of retry attempts for throttled requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SaveAllAsync(
        IEnumerable<T> items,
        int maxConcurrency,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0) return;

        Logger.LogInformation(
            "Saving {Count} items with max concurrency {MaxConcurrency}",
            itemList.Count, maxConcurrency);

        var failedItems = new List<(T Item, Exception Exception)>();

        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = itemList.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await SaveWithRetryAsync(item, maxRetries, cancellationToken);
            }
            catch (Exception ex)
            {
                lock (failedItems)
                {
                    failedItems.Add((item, ex));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (failedItems.Count > 0)
        {
            Logger.LogError(
                "Failed to save {FailedCount} of {TotalCount} items",
                failedItems.Count, itemList.Count);

            throw new AggregateException(
                $"Failed to save {failedItems.Count} items",
                failedItems.Select(f => f.Exception));
        }

        Logger.LogInformation("Successfully saved {Count} items", itemList.Count);
    }

    /// <summary>
    /// Saves a single item with retry logic for throttled requests (HTTP 429).
    /// </summary>
    /// <param name="item">Item to save</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task SaveWithRetryAsync(
        T item,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await SaveAsync(item);
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    Logger.LogWarning(
                        "Save failed after {MaxRetries} retries due to throttling", maxRetries);
                    throw;
                }

                var delay = ex.RetryAfter ?? TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));

                Logger.LogDebug(
                    "Throttled on save attempt {Attempt}, waiting {DelayMs}ms before retry",
                    attempt + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    #endregion

    #region IAsyncTenantRepository<T, string> explicit implementations

    /// <summary>
    /// Gets all items for the specified tenant. Satisfies the shared
    /// IAsyncTenantRepository&lt;T, string&gt; contract.
    /// </summary>
    async Task<IList<T>> Benday.Common.Interfaces.IAsyncTenantRepository<T, string>.GetByTenantAsync(string tenantId)
    {
        var results = await GetAllAsync(tenantId);
        return results.ToList();
    }

    /// <summary>
    /// Saves an entity. Explicit implementation for the shared
    /// IAsyncRepository&lt;T, string&gt; contract which returns Task (not Task&lt;T&gt;).
    /// </summary>
    async Task Benday.Common.Interfaces.IAsyncRepository<T, string>.SaveAsync(T entity)
    {
        await SaveAsync(entity);
    }

    #endregion
}
