using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Repository for parent-child entities. Extends <see cref="ITenantItemRepository{T}"/>
/// with a query for retrieving all children of a given parent.
/// </summary>
/// <typeparam name="T">The entity type that implements <see cref="IParentedItem"/>.</typeparam>
public interface IParentedItemRepository<T> :
    ITenantItemRepository<T> where T : class, IParentedItem, new()
{
    /// <summary>
    /// Gets all items belonging to the specified parent, optionally filtered by the parent's entity type.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="parentId">The parent entity's identifier.</param>
    /// <param name="parentEntityType">Optional parent entity type to disambiguate parents that share an id space.</param>
    /// <returns>The child items belonging to the specified parent.</returns>
    Task<List<T>> GetAllByParentIdAsync(string tenantId, string parentId, string? parentEntityType = null);
}
