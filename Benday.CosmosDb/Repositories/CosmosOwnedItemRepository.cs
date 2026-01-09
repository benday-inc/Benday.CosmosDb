using System.Net;
using System.Threading;
using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Provides repository implementation for items that are owned by a user.
/// </summary>
/// <typeparam name="T">Domain model type managed by the repository</typeparam>
/// <param name="options">Configuration options for the repository</param>
/// <param name="client">Instance of the cosmos db client. NOTE: for performance reasons, this should probably be a singleton in the application.</param>
public class CosmosOwnedItemRepository<T>(
        IOptions<CosmosRepositoryOptions<T>> options, 
        CosmosClient client, 
        ILogger<CosmosOwnedItemRepository<T>> logger) :
    CosmosRepository<T>(options, client, logger), IOwnedItemRepository<T>
    where T : class, IOwnedItem, new()
{
    /// <summary>
    /// Get all items in the repository that have the specified owner id. 
    /// Default implementation will return items in descending order by timestamp.
    /// </summary>
    /// <param name="ownerId">Owner id</param>
    /// <returns></returns>
    public async Task<IEnumerable<T>> GetAllAsync(string ownerId)
    {
        var container = await GetContainer();

        var queryable = await GetQueryable(ownerId);

        var query = queryable.Queryable.OrderByDescending(x => x.Timestamp);
        
        var results = await GetResults(query, 
            GetQueryDescription(nameof(GetAllAsync)), queryable.PartitionKey);

        return results;
    }

    /// <summary>
    /// Gets an entity by its id and owner id.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="id"></param>
    /// <returns>Matching item or null if not found</returns>
    public virtual async Task<T?> GetByIdAsync(string ownerId, string id)
    {
        if (string.IsNullOrWhiteSpace(ownerId) == true || 
            string.IsNullOrWhiteSpace(id) == true)
        {
            return null;
        }

        try
        {
            var container = await GetContainer();

            var pk = new PartitionKeyBuilder().Add(ownerId).Add(DiscriminatorValue).Build();

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

            var ruCharge = response.RequestCharge;

            Logger.LogInformation($"Request Charge: {ruCharge}");

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
        var container = await GetContainer();

        var builder = new PartitionKeyBuilder();

        _ = builder.Add(itemToDelete.PartitionKey);
        _ = builder.Add(itemToDelete.DiscriminatorValue);
        // builder.Add(id);

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
    /// Deletes all items for the specified owner with throttling and retry logic.
    /// </summary>
    /// <param name="ownerId">Owner id for the items to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAllByOwnerIdAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        await DeleteAllByOwnerIdAsync(ownerId, BulkMaxConcurrency, BulkMaxRetries, cancellationToken);
    }

    /// <summary>
    /// Deletes all items for the specified owner with configurable throttling and retry logic.
    /// </summary>
    /// <param name="ownerId">Owner id for the items to delete</param>
    /// <param name="maxConcurrency">Maximum number of concurrent delete operations</param>
    /// <param name="maxRetries">Maximum number of retry attempts for throttled requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAllByOwnerIdAsync(
        string ownerId,
        int maxConcurrency,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(ownerId);
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            Logger.LogDebug("No items to delete for owner {OwnerId}", ownerId);
            return;
        }

        Logger.LogInformation(
            "Deleting {Count} items for owner {OwnerId} with max concurrency {MaxConcurrency}",
            itemList.Count, ownerId, maxConcurrency);

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
                "Failed to delete {FailedCount} of {TotalCount} items for owner {OwnerId}",
                failedItems.Count, itemList.Count, ownerId);

            throw new AggregateException(
                $"Failed to delete {failedItems.Count} items",
                failedItems.Select(f => f.Exception));
        }

        Logger.LogInformation(
            "Successfully deleted {Count} items for owner {OwnerId}",
            itemList.Count, ownerId);
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
}
