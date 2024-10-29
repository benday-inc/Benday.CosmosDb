using System.ComponentModel;

namespace Benday.CosmosDb.Repositories;



public class CosmosRepositoryOptions<T>
{
    //public CosmosRepositoryOptions(string connectionString,
    //    bool withCreateStructures,
    //    string databaseName,
    //    string containerName,
    //    string partitionKey)
    //{
    //    ConnectionString = connectionString;
    //    WithCreateStructures = withCreateStructures;
    //    DatabaseName = databaseName;
    //    ContainerName = containerName;
    //    PartitionKey = partitionKey;
    //}

    [Description("Cosmos DB Connection String")]
    public string? ConnectionString { get; set; }

    [Description("Create Cosmos DB structures if not exists")]
    public bool WithCreateStructures { get; set; }

    [Description("Database Name")]
    public string DatabaseName { get; set; } = string.Empty;

    [Description("Container Name")]
    public string ContainerName { get; set; } = string.Empty;

    [Description("Partition Key")]
    public string PartitionKey { get; set; } = string.Empty;

}
