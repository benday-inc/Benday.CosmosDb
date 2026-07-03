namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Constants for the Benday.CosmosDb library.
/// </summary>
public class CosmosDbConstants
{
    /// <summary>
    /// The JSON property name for the entity type (second-level partition key).
    /// </summary>
    public const string PropertyName_EntityType = "entityType";
    /// <summary>The JSON property name for the Cosmos DB ETag used for optimistic concurrency.</summary>
    public const string PropertyName_Etag = "_etag";

    /// <summary>The JSON property name for the entity id.</summary>
    public const string PropertyName_Id = "id";

    /// <summary>The JSON property name for the tenant id (first-level partition key).</summary>
    public const string PropertyName_TenantId = "tenantId";

    /// <summary>The JSON property name for the Cosmos DB timestamp (<c>_ts</c>).</summary>
    public const string PropertyName_Timestamp = "_ts";

    /// <summary>
    /// String representation of the default partition key for a Cosmos DB entity.
    /// </summary>
    public const string DefaultPartitionKey = "/tenantId,/entityType";

    /// <summary>Default provisioned database throughput (RU/s) used when a database is created.</summary>
    public const int DefaultDatabaseThroughput = 400;
}
