using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using System;
using System.Linq;

namespace Benday.CosmosDb.ServiceLayers;

public class OwnedItemServiceBase<T> : IOwnedItemServiceBase<T>
    where T : class, IOwnedItem, new()
{
    private IOwnedItemRepository<T> _Repository;

    public OwnedItemServiceBase(IOwnedItemRepository<T> repository)
    {
        _Repository = repository;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(string ownerId)
    {
        return await _Repository.GetAllAsync(ownerId);
    }

    public virtual async Task<T?> GetByIdAsync(string ownerId, string id)
    {
        return await _Repository.GetByIdAsync(ownerId, id);
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
