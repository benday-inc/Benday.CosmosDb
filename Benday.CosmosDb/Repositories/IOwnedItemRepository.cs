using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.Repositories;

public interface IOwnedItemRepository<T> : IRepository<T>
    where T : class, IOwnedItem, new()
{
    Task<IEnumerable<T>> GetAllByOwnerIdAsync(string ownerId);
    Task<T?> GetByIdAndOwnerAsync(string ownerId, string id);
    Task DeleteAsync(T itemToDelete);
}
