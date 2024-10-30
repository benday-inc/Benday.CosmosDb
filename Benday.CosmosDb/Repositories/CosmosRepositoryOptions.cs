using System.ComponentModel;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Configuration options for a repository.
/// </summary>
/// <typeparam name="T">Domain model type managed by the repository</typeparam>
public class CosmosRepositoryOptions<T>
{
    /// <summary>
    /// Connection string for the Cosmos DB account.
    /// </summary>
    [Description("Cosmos DB Connection String")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Configure whether to create Cosmos DB structures if they don't already exists. This option is disabled by default.
    /// </summary>
    [Description("Create Cosmos DB structures if they don't already exists")]
    public bool WithCreateStructures { get; set; }

    /// <summary>
    /// Cosmos DB database name.
    /// </summary>
    [Description("Database Name")]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the container in the Cosmos DB database.
    /// </summary>
    [Description("Container Name")]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Partition key configuration for the Cosmos DB container.
    /// </summary>
    [Description("Partition Key")]
    public string PartitionKey { get; set; } = CosmosDbConstants.DefaultPartitionKey;

}
