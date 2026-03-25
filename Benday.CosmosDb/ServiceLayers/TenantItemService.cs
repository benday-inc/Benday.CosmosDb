using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.ServiceLayers;

public class TenantItemService<T> : ITenantItemService<T>
    where T : class, ITenantItem, new()
{
    private ITenantItemRepository<T> _Repository;

    public TenantItemService(ITenantItemRepository<T> repository)
    {
        _Repository = repository;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(string tenantId)
    {
        return await _Repository.GetAllAsync(tenantId);
    }

    public virtual async Task<T?> GetByIdAsync(string tenantId, string id)
    {
        return await _Repository.GetByIdAsync(tenantId, id);
    }

    public virtual async Task<T?> SaveAsync(T item)
    {
        return await _Repository.SaveAsync(item);
    }

    public virtual async Task DeleteAsync(T item)
    {
        await _Repository.DeleteAsync(item);
    }
}
