using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Repositories;

public class CosmosDbParentedItemRepository<T> : 
    CosmosOwnedItemRepository<T>, IParentedItemRepository<T>
    where T : class, IParentedItem, new()
{
    public CosmosDbParentedItemRepository(
        IOptions<CosmosRepositoryOptions<T>> options,
        CosmosClient client,
        ILogger<CosmosOwnedItemRepository<T>> logger) :
        base(options, client, logger)
    {
    }

    public async Task<List<T>> GetAllByParentIdAsync(string ownerId, string parentId)
    {
        var container = await GetContainer();

        var queryable = await GetQueryable(ownerId);

        var query = queryable.Queryable
                    .Where(x => x.ParentId == parentId);

        var results = await GetResults(query, GetQueryDescription(), queryable.PartitionKey);

        return results;
    }
}
