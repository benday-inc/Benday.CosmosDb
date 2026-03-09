using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.Identity.CosmosDb;

public class CosmosDbClaimDefinitionStore :
    CosmosOwnedItemRepository<CosmosIdentityClaimDefinition>,
    ICosmosDbClaimDefinitionStore
{
    public CosmosDbClaimDefinitionStore(
        IOptions<CosmosRepositoryOptions<CosmosIdentityClaimDefinition>> options,
        CosmosClient client,
        ILogger<CosmosDbClaimDefinitionStore> logger) :
        base(options, client, logger)
    {
    }

    public async Task<IList<CosmosIdentityClaimDefinition>> GetAllAsync()
    {
        var query = await GetQueryable();
        var results = await GetResults(query.Queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    public async Task<CosmosIdentityClaimDefinition?> FindByClaimTypeAsync(string claimType)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x => x.ClaimType == claimType);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }
}
