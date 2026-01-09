using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Defines the contract for a repository that manages items that have an owner.
/// </summary>
/// <typeparam name="T">Domain model type managed by the repository</typeparam>
public interface IOwnedItemRepository<T> : IRepository<T>
    where T : class, IOwnedItem, new()
{
    /// <summary>
    /// Get all items in the repository that have the specified owner id.
    /// </summary>
    /// <param name="ownerId">Owner id</param>
    /// <returns>Matching items</returns>
    Task<IEnumerable<T>> GetAllAsync(string ownerId);

    /// <summary>
    /// Get a specific item by its id and owner id.
    /// </summary>
    /// <param name="ownerId">Owner id</param>
    /// <param name="id">Id for the entity</param>
    /// <returns>The matching item or null</returns>
    Task<T?> GetByIdAsync(string ownerId, string id);

    /// <summary>
    /// Delete an item from the repository.
    /// </summary>
    /// <param name="itemToDelete">Item to delete</param>
    /// <returns></returns>
    Task DeleteAsync(T itemToDelete);

    /// <summary>
    /// Deletes all items for the specified owner with throttling and retry logic.
    /// </summary>
    /// <param name="ownerId">Owner id for the items to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAllByOwnerIdAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all items for the specified owner with configurable throttling and retry logic.
    /// </summary>
    /// <param name="ownerId">Owner id for the items to delete</param>
    /// <param name="maxConcurrency">Maximum number of concurrent delete operations</param>
    /// <param name="maxRetries">Maximum number of retry attempts for throttled requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAllByOwnerIdAsync(string ownerId, int maxConcurrency, int maxRetries, CancellationToken cancellationToken = default);

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
}
