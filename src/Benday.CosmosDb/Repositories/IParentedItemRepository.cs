using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.Repositories;

public interface IParentedItemRepository<T> :
    ITenantItemRepository<T> where T : class, IParentedItem, new()
{
    Task<List<T>> GetAllByParentIdAsync(string tenantId, string parentId, string? parentEntityType = null);
}
