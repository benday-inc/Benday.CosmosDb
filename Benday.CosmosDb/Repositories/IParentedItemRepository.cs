using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.Repositories;

public interface IParentedItemRepository<T> :
    IOwnedItemRepository<T> where T : class, IParentedItem, new()
{
    Task<List<T>> GetAllByParentIdAsync(string ownerId, string parentId, string? parentDiscriminator = null);
}