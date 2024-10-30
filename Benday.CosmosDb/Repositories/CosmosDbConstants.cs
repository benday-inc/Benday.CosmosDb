namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Constants for the Benday.CosmosDb library.
/// </summary>
public class CosmosDbConstants
{
    /// <summary>
    /// The name of the discriminator property for the entity.
    /// </summary>
    public const string DiscriminatorPropertyName = "discriminator";

    /// <summary>
    /// String representation of the default partition key for a Cosmos DB entity.
    /// </summary>
    public const string DefaultPartitionKey = "/pk,/discriminator";
}