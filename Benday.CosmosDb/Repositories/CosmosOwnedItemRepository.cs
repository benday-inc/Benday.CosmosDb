using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace Benday.CosmosDb.Repositories;

public abstract class CosmosOwnedItemRepository<T>(IOptions<CosmosRepositoryOptions<T>> options, CosmosClient client) :
    CosmosRepository<T>(options, client), IOwnedItemRepository<T>
    where T : class, IOwnedItem, new()
{
    public async Task<IEnumerable<T>> GetAllByOwnerIdAsync(string ownerId)
    {
        var container = await GetContainer();

        var query = $"SELECT * FROM c WHERE c.OwnerId = \"{ownerId}\" and c.{CosmosDbConstants.DiscriminatorPropertyName} = \"{DiscriminatorValue}\" ORDER BY c._ts desc";

        // Execute the query
        var resultSetIterator = container.GetItemQueryIterator<T>(query);

        var items = await GetResults(resultSetIterator, nameof(GetAllByOwnerIdAsync));

        return items;
    }

    protected virtual PartitionKey GetPartitionKey(string ownerId) => GetPartitionKey(ownerId, DiscriminatorValue);    

    protected virtual async Task<IOrderedQueryable<T>> GetQueryable(string ownerId)
    {
        var pk = new PartitionKeyBuilder().Add(ownerId).Add(DiscriminatorValue).Build();

        var container = await GetContainer();

        var queryable =
            container.GetItemLinqQueryable<T>(true, 
            requestOptions: new QueryRequestOptions() { PartitionKey = pk });

        return queryable;
    }

    public virtual async Task<T?> GetByIdAndOwnerAsync(string ownerId, string id)
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

        if (item == null) {
            return null;
        }

        return item;
    }

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
