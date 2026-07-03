using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Cosmos DB store for custom claim type definitions and their allowed values.
/// </summary>
public class CosmosDbClaimDefinitionStore :
    CosmosTenantItemRepository<CosmosIdentityClaimDefinition>,
    ICosmosDbClaimDefinitionStore
{
    private readonly string _identityTenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbClaimDefinitionStore"/> class.
    /// </summary>
    /// <param name="options">Repository options for the claim definition container.</param>
    /// <param name="client">The Cosmos DB client used to access the container.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="identityOptions">Identity options that supply the tenant id used for claim definition storage.</param>
    public CosmosDbClaimDefinitionStore(
        IOptions<CosmosRepositoryOptions<CosmosIdentityClaimDefinition>> options,
        CosmosClient client,
        ILogger<CosmosDbClaimDefinitionStore> logger,
        CosmosIdentityOptions identityOptions) :
        base(options, client, logger)
    {
        _identityTenantId = identityOptions.IdentityTenantId;
    }

    /// <summary>
    /// Gets all claim definitions for the identity tenant.
    /// </summary>
    /// <returns>The list of all claim definitions.</returns>
    public new async Task<IList<CosmosIdentityClaimDefinition>> GetAllAsync()
    {
        var queryContext = await GetQueryContextAsync(_identityTenantId);
        var results = await GetResultsAsync(queryContext.Queryable, GetQueryDescription(), queryContext.PartitionKey);
        return results;
    }

    /// <summary>
    /// Finds the claim definition that matches the specified claim type.
    /// </summary>
    /// <param name="claimType">The claim type to search for.</param>
    /// <returns>The matching claim definition, or <c>null</c> if none exists.</returns>
    public async Task<CosmosIdentityClaimDefinition?> FindByClaimTypeAsync(string claimType)
    {
        var queryContext = await GetQueryContextAsync(_identityTenantId);
        var queryable = queryContext.Queryable.Where(x => x.ClaimType == claimType);
        var results = await GetResultsAsync(queryable, GetQueryDescription(), queryContext.PartitionKey);
        return results.FirstOrDefault();
    }
}
