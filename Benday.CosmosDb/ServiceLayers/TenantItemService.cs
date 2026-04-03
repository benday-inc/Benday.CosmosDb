using Benday.Common.Interfaces;
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
        return await ((IRepository<T>)_Repository).SaveAsync(item);
    }

    public virtual async Task DeleteAsync(T item)
    {
        await _Repository.DeleteAsync(item);
    }

    #region IAsyncTenantService<T, string> explicit implementations

    /// <summary>
    /// Gets all items for the specified tenant. Satisfies the shared
    /// IAsyncTenantService&lt;T, string&gt; contract.
    /// </summary>
    async Task<IList<T>> IAsyncTenantService<T, string>.GetByTenantAsync(string tenantId)
    {
        var results = await GetAllAsync(tenantId);
        return results.ToList();
    }

    /// <summary>
    /// Saves an entity. Explicit implementation for the shared
    /// IAsyncService&lt;T, string&gt; contract which returns Task (not Task&lt;T?&gt;).
    /// </summary>
    async Task IAsyncService<T, string>.SaveAsync(T entity)
    {
        await SaveAsync(entity);
    }

    /// <summary>
    /// Gets an entity by id without tenant context.
    /// Cosmos DB requires tenantId for efficient lookups.
    /// Use GetByIdAsync(tenantId, id) instead.
    /// </summary>
    Task<T?> IAsyncService<T, string>.GetByIdAsync(string id)
    {
        throw new NotSupportedException(
            "Cosmos DB requires tenantId for efficient lookups. " +
            "Use GetByIdAsync(tenantId, id) instead.");
    }

    #endregion
}
