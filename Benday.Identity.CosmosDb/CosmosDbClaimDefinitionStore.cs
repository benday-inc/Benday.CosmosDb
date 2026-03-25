using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.Identity.CosmosDb;

public class CosmosDbClaimDefinitionStore :
    CosmosTenantItemRepository<CosmosIdentityClaimDefinition>,
    ICosmosDbClaimDefinitionStore
{
    private readonly string _identityTenantId;

    public CosmosDbClaimDefinitionStore(
        IOptions<CosmosRepositoryOptions<CosmosIdentityClaimDefinition>> options,
        CosmosClient client,
        ILogger<CosmosDbClaimDefinitionStore> logger,
        CosmosIdentityOptions identityOptions) :
        base(options, client, logger)
    {
        _identityTenantId = identityOptions.IdentityTenantId;
    }

    public new async Task<IList<CosmosIdentityClaimDefinition>> GetAllAsync()
    {
        var query = await GetQueryable(_identityTenantId);
        var results = await GetResults(query.Queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    public async Task<CosmosIdentityClaimDefinition?> FindByClaimTypeAsync(string claimType)
    {
        var query = await GetQueryable(_identityTenantId);
        var queryable = query.Queryable.Where(x => x.ClaimType == claimType);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }
}
