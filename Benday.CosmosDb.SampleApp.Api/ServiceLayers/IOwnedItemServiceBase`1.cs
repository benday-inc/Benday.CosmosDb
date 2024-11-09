using Benday.CosmosDb.DomainModels;
using System;
using System.Linq;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public interface IOwnedItemServiceBase<T> where T : class, IOwnedItem, new()
{
    Task DeleteAsync(T item);
    Task<IEnumerable<T>> GetAllAsync(string ownerId);
    Task<T?> GetByIdAsync(string ownerId, string id);
    Task<T?> SaveAsync(T item);
}
