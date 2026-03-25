using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.ServiceLayers;

public interface ITenantItemService<T> where T : class, ITenantItem, new()
{
    Task DeleteAsync(T item);
    Task<IEnumerable<T>> GetAllAsync(string tenantId);
    Task<T?> GetByIdAsync(string tenantId, string id);
    Task<T?> SaveAsync(T item);
}
