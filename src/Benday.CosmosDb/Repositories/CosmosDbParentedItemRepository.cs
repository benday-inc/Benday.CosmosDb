using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Default <see cref="IParentedItemRepository{T}"/> implementation for parent-child entities,
/// building on <see cref="CosmosTenantItemRepository{T}"/>.
/// </summary>
/// <typeparam name="T">The entity type that implements <see cref="IParentedItem"/>.</typeparam>
public class CosmosDbParentedItemRepository<T> :
    CosmosTenantItemRepository<T>, IParentedItemRepository<T>
    where T : class, IParentedItem, new()
{
    /// <summary>
    /// Initializes a new instance of the repository.
    /// </summary>
    /// <param name="options">Repository options (connection, database, container, partition key).</param>
    /// <param name="client">The shared Cosmos client.</param>
    /// <param name="logger">Logger for the repository.</param>
    /// <param name="sink">Optional query-diagnostics sink; defaults to the DI-registered sink.</param>
    /// <param name="diagnosticsRegistry">Optional per-entity diagnostics options registry.</param>
    public CosmosDbParentedItemRepository(
        IOptions<CosmosRepositoryOptions<T>> options,
        CosmosClient client,
        ILogger<CosmosTenantItemRepository<T>> logger,
        ICosmosQueryLogSink? sink = null,
        CosmosDiagnosticsRegistry? diagnosticsRegistry = null) :
        base(options, client, logger, sink, diagnosticsRegistry)
    {
    }

    /// <inheritdoc/>
    public async Task<List<T>> GetAllByParentIdAsync(string tenantId, string parentId, string? parentEntityType = null)
    {
        var container = await GetContainerAsync();

        var queryContext = await GetQueryContextAsync(tenantId);

        var query = queryContext.Queryable
                    .Where(x => x.ParentId == parentId);

        if (!string.IsNullOrEmpty(parentEntityType))
        {
            query = query.Where(x => x.ParentEntityType == parentEntityType);
        }

        var results = await GetResultsAsync(query, GetQueryDescription(), queryContext.PartitionKey);

        return results;
    }
}
