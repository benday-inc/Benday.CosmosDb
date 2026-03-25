using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.ServiceLayers;

/// <summary>
/// Service layer implementation for parented items. Extends TenantItemService with parent-specific operations.
/// </summary>
/// <typeparam name="T">The entity type that implements IParentedItem</typeparam>
public class ParentedItemService<T> : TenantItemService<T>, IParentedItemService<T>
    where T : class, IParentedItem, new()
{
    private IParentedItemRepository<T> _ParentedRepository;

    public ParentedItemService(IParentedItemRepository<T> repository) : base(repository)
    {
        _ParentedRepository = repository;
    }

    /// <summary>
    /// Gets all items by parent ID and optionally filters by parent entity type
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="parentId">The parent ID</param>
    /// <param name="parentEntityType">Optional parent entity type to filter by parent type</param>
    /// <returns>List of items belonging to the specified parent</returns>
    public virtual async Task<IEnumerable<T>> GetAllByParentIdAsync(string tenantId, string parentId, string? parentEntityType = null)
    {
        return await _ParentedRepository.GetAllByParentIdAsync(tenantId, parentId, parentEntityType);
    }
}
