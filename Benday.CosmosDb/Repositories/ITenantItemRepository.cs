using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Defines the contract for a repository that manages items that belong to a tenant.
/// </summary>
/// <typeparam name="T">Domain model type managed by the repository</typeparam>
public interface ITenantItemRepository<T> : IRepository<T>
    where T : class, ITenantItem, new()
{
    /// <summary>
    /// Get all items in the repository that have the specified tenant id.
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <returns>Matching items</returns>
    Task<IEnumerable<T>> GetAllAsync(string tenantId);

    /// <summary>
    /// Get a specific item by its id and tenant id.
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <param name="id">Id for the entity</param>
    /// <returns>The matching item or null</returns>
    Task<T?> GetByIdAsync(string tenantId, string id);

    /// <summary>
    /// Delete an item from the repository.
    /// </summary>
    /// <param name="itemToDelete">Item to delete</param>
    /// <returns></returns>
    Task DeleteAsync(T itemToDelete);

    /// <summary>
    /// Deletes all items for the specified tenant with throttling and retry logic.
    /// </summary>
    /// <param name="tenantId">Tenant id for the items to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAllByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all items for the specified tenant with configurable throttling and retry logic.
    /// </summary>
    /// <param name="tenantId">Tenant id for the items to delete</param>
    /// <param name="maxConcurrency">Maximum number of concurrent delete operations</param>
    /// <param name="maxRetries">Maximum number of retry attempts for throttled requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAllByTenantIdAsync(string tenantId, int maxConcurrency, int maxRetries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple items with throttling and retry logic.
    /// </summary>
    /// <param name="items">Items to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple items with configurable throttling and retry logic.
    /// </summary>
    /// <param name="items">Items to save</param>
    /// <param name="maxConcurrency">Maximum number of concurrent save operations</param>
    /// <param name="maxRetries">Maximum number of retry attempts for throttled requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveAllAsync(IEnumerable<T> items, int maxConcurrency, int maxRetries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a page of results for the specified tenant with continuation support.
    /// </summary>
    /// <param name="tenantId">Tenant id</param>
    /// <param name="pageSize">Maximum number of items to return</param>
    /// <param name="continuationToken">Continuation token from previous query (null for first page)</param>
    /// <returns>A page of results with continuation information</returns>
    Task<PagedResults<T>> GetPagedAsync(string tenantId, int pageSize = 100, string? continuationToken = null);
}
