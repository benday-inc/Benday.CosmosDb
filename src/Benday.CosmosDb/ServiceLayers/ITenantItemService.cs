using Benday.Common.Interfaces;
using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.ServiceLayers;

public interface ITenantItemService<T> :
    IAsyncTenantService<T, string>
    where T : class, ITenantItem, new()
{
    new Task DeleteAsync(T item);
    Task<IEnumerable<T>> GetAllAsync(string tenantId);
    new Task<T?> GetByIdAsync(string tenantId, string id);
    new Task<T?> SaveAsync(T item);
}
