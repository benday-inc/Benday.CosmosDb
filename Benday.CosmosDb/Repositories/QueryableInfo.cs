using Benday.CosmosDb.DomainModels;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.Repositories;

public class QueryableInfo<T> where T : class, ICosmosIdentity, new()
{
    public QueryableInfo(PartitionKey partitionKey, IOrderedQueryable<T> queryable)
    {
        PartitionKey = partitionKey;
        Queryable = queryable;
    }

    public PartitionKey PartitionKey { get; }
    public IOrderedQueryable<T> Queryable { get; }
}

