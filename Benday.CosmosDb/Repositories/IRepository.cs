namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Interface for the basic operations of a Cosmos DB repository.
/// </summary>
/// <typeparam name="T">Domain model type that is managed by this repository</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Save the entity to the repository. Perform an insert if the entity does not exist, otherwise perform an update.
    /// </summary>
    /// <param name="entity">Entity to save</param>
    /// <returns>The saved entity</returns>
    Task<T> SaveAsync(T entity);

    /// <summary>
    /// Save a list of entities to the repository. Perform an insert if the entity does not exist, otherwise perform an update.
    /// </summary>
    /// <param name="items">Entities to save</param>
    /// <returns></returns>
    Task SaveAsync(IList<T> items);

    /// <summary>
    /// Delete the entity from the repository.
    /// </summary>
    /// <param name="id">Id for the entity to delete</param>
    /// <returns></returns>
    Task DeleteAsync(string id);

    /// <summary>
    /// Gets an entity by its id.
    /// </summary>
    /// <param name="id">Id of the entity</param>
    /// <returns>The matching entity or null if not found</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Gets all entities in the repository.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Gets a page of results with continuation support.
    /// </summary>
    /// <param name="pageSize">Maximum number of items to return</param>
    /// <param name="continuationToken">Continuation token from previous query (null for first page)</param>
    /// <returns>A page of results with continuation information</returns>
    Task<PagedResults<T>> GetPagedAsync(int pageSize = 100, string? continuationToken = null);
}
