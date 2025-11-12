using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.ServiceLayers;

/// <summary>
/// Service layer implementation for parented items. Extends OwnedItemService with parent-specific operations.
/// </summary>
/// <typeparam name="T">The entity type that implements IParentedItem</typeparam>
public class ParentedItemService<T> : OwnedItemService<T>, IParentedItemService<T>
    where T : class, IParentedItem, new()
{
    private IParentedItemRepository<T> _ParentedRepository;

    public ParentedItemService(IParentedItemRepository<T> repository) : base(repository)
    {
        _ParentedRepository = repository;
    }

    /// <summary>
    /// Gets all items by parent ID and optionally filters by parent discriminator
    /// </summary>
    /// <param name="ownerId">The owner ID</param>
    /// <param name="parentId">The parent ID</param>
    /// <param name="parentDiscriminator">Optional parent discriminator to filter by parent type</param>
    /// <returns>List of items belonging to the specified parent</returns>
    public virtual async Task<IEnumerable<T>> GetAllByParentIdAsync(string ownerId, string parentId, string? parentDiscriminator = null)
    {
        return await _ParentedRepository.GetAllByParentIdAsync(ownerId, parentId, parentDiscriminator);
    }
}
