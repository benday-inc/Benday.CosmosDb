using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.ServiceLayers;

/// <summary>
/// Service layer interface for parented items. Extends ITenantItemService with parent-specific operations.
/// </summary>
/// <typeparam name="T">The entity type that implements IParentedItem</typeparam>
public interface IParentedItemService<T> : ITenantItemService<T>
    where T : class, IParentedItem, new()
{
    /// <summary>
    /// Gets all items by parent ID and optionally filters by parent entity type
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="parentId">The parent ID</param>
    /// <param name="parentEntityType">Optional parent entity type to filter by parent type</param>
    /// <returns>List of items belonging to the specified parent</returns>
    Task<IEnumerable<T>> GetAllByParentIdAsync(string tenantId, string parentId, string? parentEntityType = null);
}
