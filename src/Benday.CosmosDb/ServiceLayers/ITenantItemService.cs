using Benday.Common.Interfaces;
using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.ServiceLayers;

/// <summary>
/// Service abstraction over a tenant-scoped Cosmos DB repository for entity type <typeparamref name="T"/>.
/// Provides tenant-aware CRUD operations layered on top of the repository.
/// </summary>
/// <typeparam name="T">The tenant-scoped entity type.</typeparam>
public interface ITenantItemService<T> :
    IAsyncTenantService<T, string>
    where T : class, ITenantItem, new()
{
    /// <summary>Deletes the specified item.</summary>
    /// <param name="item">The item to delete.</param>
    new Task DeleteAsync(T item);

    /// <summary>Gets all items belonging to the specified tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The items for the tenant.</returns>
    Task<IEnumerable<T>> GetAllAsync(string tenantId);

    /// <summary>Gets a single item by tenant and id.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="id">The item identifier.</param>
    /// <returns>The matching item, or null if not found.</returns>
    new Task<T?> GetByIdAsync(string tenantId, string id);

    /// <summary>Inserts or updates the specified item.</summary>
    /// <param name="item">The item to save.</param>
    /// <returns>The saved item, including any server-assigned values.</returns>
    new Task<T?> SaveAsync(T item);
}
