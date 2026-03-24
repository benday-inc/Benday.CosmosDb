using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Repositories;

public class CosmosDbParentedItemRepository<T> :
    CosmosTenantItemRepository<T>, IParentedItemRepository<T>
    where T : class, IParentedItem, new()
{
    public CosmosDbParentedItemRepository(
        IOptions<CosmosRepositoryOptions<T>> options,
        CosmosClient client,
        ILogger<CosmosTenantItemRepository<T>> logger) :
        base(options, client, logger)
    {
    }

    public async Task<List<T>> GetAllByParentIdAsync(string tenantId, string parentId, string? parentEntityType = null)
    {
        var container = await GetContainer();

        var queryable = await GetQueryable(tenantId);

        var query = queryable.Queryable
                    .Where(x => x.ParentId == parentId);

        if (!string.IsNullOrEmpty(parentEntityType))
        {
            query = query.Where(x => x.ParentEntityType == parentEntityType);
        }

        var results = await GetResults(query, GetQueryDescription(), queryable.PartitionKey);

        return results;
    }
}
