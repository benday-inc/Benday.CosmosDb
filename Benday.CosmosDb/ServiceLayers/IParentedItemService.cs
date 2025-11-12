using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.ServiceLayers;

/// <summary>
/// Service layer interface for parented items. Extends IOwnedItemService with parent-specific operations.
/// </summary>
/// <typeparam name="T">The entity type that implements IParentedItem</typeparam>
public interface IParentedItemService<T> : IOwnedItemService<T>
    where T : class, IParentedItem, new()
{
    /// <summary>
    /// Gets all items by parent ID and optionally filters by parent discriminator
    /// </summary>
    /// <param name="ownerId">The owner ID</param>
    /// <param name="parentId">The parent ID</param>
    /// <param name="parentDiscriminator">Optional parent discriminator to filter by parent type</param>
    /// <returns>List of items belonging to the specified parent</returns>
    Task<IEnumerable<T>> GetAllByParentIdAsync(string ownerId, string parentId, string? parentDiscriminator = null);
}
