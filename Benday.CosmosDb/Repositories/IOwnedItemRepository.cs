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
    Task<IEnumerable<T>> GetAllByOwnerIdAsync(string ownerId);

    /// <summary>
    /// Get a specific item by its id and owner id.
    /// </summary>
    /// <param name="ownerId">Owner id</param>
    /// <param name="id">Id for the entity</param>
    /// <returns>The matching item or null</returns>
    Task<T?> GetByIdAndOwnerAsync(string ownerId, string id);

    /// <summary>
    /// Delete an item from the repository.
    /// </summary>
    /// <param name="itemToDelete">Item to delete</param>
    /// <returns></returns>
    Task DeleteAsync(T itemToDelete);
}
