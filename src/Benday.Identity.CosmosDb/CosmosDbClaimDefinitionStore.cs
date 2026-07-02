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
        var queryContext = await GetQueryContextAsync(_identityTenantId);
        var results = await GetResultsAsync(queryContext.Queryable, GetQueryDescription(), queryContext.PartitionKey);
        return results;
    }

    public async Task<CosmosIdentityClaimDefinition?> FindByClaimTypeAsync(string claimType)
    {
        var queryContext = await GetQueryContextAsync(_identityTenantId);
        var queryable = queryContext.Queryable.Where(x => x.ClaimType == claimType);
        var results = await GetResultsAsync(queryable, GetQueryDescription(), queryContext.PartitionKey);
        return results.FirstOrDefault();
    }
}
