namespace Benday.CosmosDb.Repositories;

public interface IRepository<T> where T : class
{
    Task<T> SaveAsync(T entity);
    Task SaveAsync(IList<T> items);
    Task DeleteAsync(string id);
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
}
