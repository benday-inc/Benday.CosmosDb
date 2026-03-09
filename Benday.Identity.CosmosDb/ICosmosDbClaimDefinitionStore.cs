using Benday.CosmosDb.Repositories;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Repository interface for managing claim definitions.
/// </summary>
public interface ICosmosDbClaimDefinitionStore : IOwnedItemRepository<CosmosIdentityClaimDefinition>
{
    /// <summary>
    /// Gets all claim definitions.
    /// </summary>
    Task<IList<CosmosIdentityClaimDefinition>> GetAllAsync();

    /// <summary>
    /// Finds a claim definition by its claim type name.
    /// </summary>
    Task<CosmosIdentityClaimDefinition?> FindByClaimTypeAsync(string claimType);
}
