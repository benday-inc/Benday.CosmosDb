using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Pairs a LINQ queryable expression tree with the partition key it was created for.
/// Returned by <see cref="CosmosRepository{T}.GetQueryContextAsync(string)"/> as the
/// starting point for building custom queries in derived repository classes.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public class QueryContext<T> where T : class, ICosmosIdentity, new()
{
    /// <summary>
    /// Creates a new query context.
    /// </summary>
    /// <param name="partitionKey">The partition key that scopes this query.</param>
    /// <param name="queryable">The LINQ expression tree for building queries against Cosmos DB.</param>
    public QueryContext(PartitionKey partitionKey, IOrderedQueryable<T> queryable)
    {
        PartitionKey = partitionKey;
        Queryable = queryable;
    }

    /// <summary>
    /// The partition key that scopes this query. Pass this to
    /// <c>GetResultsAsync</c> so the query executes within the correct partition
    /// and RU consumption is logged accurately.
    /// </summary>
    public PartitionKey PartitionKey { get; }

    /// <summary>
    /// The LINQ expression tree for building queries against Cosmos DB.
    /// Chain <c>.Where()</c>, <c>.OrderBy()</c>, etc. to build your query,
    /// then pass the result to <c>GetResultsAsync</c> for execution.
    /// </summary>
    public IOrderedQueryable<T> Queryable { get; }
}
