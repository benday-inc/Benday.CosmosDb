namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Constants for the Benday.CosmosDb library.
/// </summary>
public class CosmosDbConstants
{
    /// <summary>
    /// The name of the discriminator property for the entity.
    /// </summary>
    public const string PropertyName_Discriminator = "discriminator";
    public const string PropertyName_Etag = "_etag";
    public const string PropertyName_Id = "id";
    public const string PropertyName_PartitionKey = "pk";
    public const string PropertyName_Timestamp = "_ts";

    /// <summary>
    /// String representation of the default partition key for a Cosmos DB entity.
    /// </summary>
    public const string DefaultPartitionKey = "/pk,/discriminator";
    public const int DefaultDatabaseThroughput = 400;
}