using Benday.Common.Interfaces;
using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.ServiceLayers;

/// <summary>
/// Default <see cref="ITenantItemService{T}"/> implementation that delegates to an
/// <see cref="ITenantItemRepository{T}"/>. Virtual members allow per-entity subclasses
/// to add domain behavior around the repository calls.
/// </summary>
/// <typeparam name="T">The tenant-scoped entity type.</typeparam>
public class TenantItemService<T> : ITenantItemService<T>
    where T : class, ITenantItem, new()
{
    private ITenantItemRepository<T> _Repository;

    /// <summary>
    /// Initializes a new instance backed by the given repository.
    /// </summary>
    /// <param name="repository">The repository this service delegates to.</param>
    public TenantItemService(ITenantItemRepository<T> repository)
    {
        _Repository = repository;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> GetAllAsync(string tenantId)
    {
        return await _Repository.GetAllAsync(tenantId);
    }

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(string tenantId, string id)
    {
        return await _Repository.GetByIdAsync(tenantId, id);
    }

    /// <inheritdoc/>
    public virtual async Task<T?> SaveAsync(T item)
    {
        return await ((IRepository<T>)_Repository).SaveAsync(item);
    }

    /// <inheritdoc/>
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
