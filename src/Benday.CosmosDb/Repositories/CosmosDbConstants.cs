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
    public const string PropertyName_Etag = "_etag";
    public const string PropertyName_Id = "id";
    public const string PropertyName_TenantId = "tenantId";
    public const string PropertyName_Timestamp = "_ts";

    /// <summary>
    /// String representation of the default partition key for a Cosmos DB entity.
    /// </summary>
    public const string DefaultPartitionKey = "/tenantId,/entityType";
    public const int DefaultDatabaseThroughput = 400;
}
