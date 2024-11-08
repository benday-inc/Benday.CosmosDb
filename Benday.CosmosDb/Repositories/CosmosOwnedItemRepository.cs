using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Provides repository implementation for items that are owned by a user.
/// </summary>
/// <typeparam name="T">Domain model type managed by the repository</typeparam>
/// <param name="options">Configuration options for the repository</param>
/// <param name="client">Instance of the cosmos db client. NOTE: for performance reasons, this should probably be a singleton in the application.</param>
public class CosmosOwnedItemRepository<T>(IOptions<CosmosRepositoryOptions<T>> options, CosmosClient client) :
    CosmosRepository<T>(options, client), IOwnedItemRepository<T>
    where T : class, IOwnedItem, new()
{
    /// <summary>
    /// Get all items in the repository that have the specified owner id. 
    /// Default implementation will return items in descending order by timestamp.
    /// </summary>
    /// <param name="ownerId">Owner id</param>
    /// <returns></returns>
    public async Task<IEnumerable<T>> GetAllAsync(string ownerId)
    {
        var container = await GetContainer();

        var queryable = await GetQueryable(ownerId);

        var query = queryable.OrderByDescending(x => x.Timestamp);
        
        var results = await GetResults(query, 
            GetQueryDescription(nameof(GetAllAsync)));

        return results;
    }

    /// <summary>
    /// Gets an entity by its id and owner id.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="id"></param>
    /// <returns>Matching item or null if not found</returns>
    public virtual async Task<T?> GetByIdAsync(string ownerId, string id)
    {
        if (string.IsNullOrWhiteSpace(ownerId) == true || 
            string.IsNullOrWhiteSpace(id) == true)
        {
            return null;
        }

        try
        {
            var container = await GetContainer();

            var pk = new PartitionKeyBuilder().Add(ownerId).Add(DiscriminatorValue).Build();

            var response = await container.ReadItemAsync<T>(id, pk);

            var ruCharge = response.RequestCharge;

            Console.WriteLine($"Request Charge: {ruCharge}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var item = response.Resource;

            if (item == null)
            {
                return null;
            }
            else
            {
                return item;
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Delete an item from the repository.
    /// </summary>
    /// <param name="itemToDelete"></param>
    /// <returns></returns>
    public async Task DeleteAsync(T itemToDelete)
    {
        var container = await GetContainer();

        var builder = new PartitionKeyBuilder();

        _ = builder.Add(itemToDelete.PartitionKey);
        _ = builder.Add(itemToDelete.DiscriminatorValue);
        // builder.Add(id);        

        var partitionKey = builder.Build();

        ItemResponse<T> response;
        try
        {
            response = await container.DeleteItemAsync<T>(itemToDelete.Id, partitionKey);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
