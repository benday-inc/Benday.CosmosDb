using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.Identity.CosmosDb;

public class CosmosDbClaimDefinitionStore :
    CosmosOwnedItemRepository<CosmosIdentityClaimDefinition>,
    ICosmosDbClaimDefinitionStore
{
    private readonly string _identityOwnerId;

    public CosmosDbClaimDefinitionStore(
        IOptions<CosmosRepositoryOptions<CosmosIdentityClaimDefinition>> options,
        CosmosClient client,
        ILogger<CosmosDbClaimDefinitionStore> logger,
        CosmosIdentityOptions identityOptions) :
        base(options, client, logger)
    {
        _identityOwnerId = identityOptions.IdentityOwnerId;
    }

    public new async Task<IList<CosmosIdentityClaimDefinition>> GetAllAsync()
    {
        var query = await GetQueryable(_identityOwnerId);
        var results = await GetResults(query.Queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    public async Task<CosmosIdentityClaimDefinition?> FindByClaimTypeAsync(string claimType)
    {
        var query = await GetQueryable(_identityOwnerId);
        var queryable = query.Queryable.Where(x => x.ClaimType == claimType);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }
}
