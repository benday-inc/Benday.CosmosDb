using System;
using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.Utilities;

public class CosmosConfig
{
    public CosmosConfig()
    {
        AccountKey = string.Empty;
        Endpoint = string.Empty;
        DatabaseName = string.Empty;
        ContainerName = string.Empty;
        PartitionKey = string.Empty;
        CreateStructures = false;
        DatabaseThroughput = CosmosDbConstants.DefaultDatabaseThroughput;
        UseGatewayMode = false;
        UseHierarchicalPartitionKey = false;
        AllowBulkExecution = true;
        UseDefaultAzureCredential = false;
    }

    /// <summary>
    /// Account key for Cosmos DB
    /// </summary>
    public string AccountKey { get; set; }

    /// <summary>
    /// Endpoint for Cosmos DB
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Cosmos DB database name.
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// Name of the container in the Cosmos DB database.
    /// </summary>
    public string ContainerName { get; set; }

    /// <summary>
    /// Partition key configuration for the Cosmos DB container.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Configure whether to create Cosmos DB structures if they don't already exists. This option is disabled by default.
    /// </summary>
    public bool CreateStructures { get; set; }

    /// <summary>
    /// Use Gateway mode for Cosmos DB. Default is false (direct mode).
    /// </summary>
    public bool UseGatewayMode { get; set; }

    /// <summary>
    /// Use DefaultAzureCredential for authentication. This is disabled by default.
    /// </summary>
    public bool UseDefaultAzureCredential { get; set; }

    /// <summary>
    /// Use hierarchical partition key. This option is disabled by default.
    /// </summary>
    public bool UseHierarchicalPartitionKey { get; set; }

    /// <summary>
    /// Throughput for the database. This is only used if the database is created.
    /// </summary>
    public int DatabaseThroughput { get; set; }

    /// <summary>
    /// Allow bulk execution for Cosmos DB operations on the client connection. This is enabled by default.
    /// </summary>
    public bool AllowBulkExecution { get; set; } = true;

    /// <summary>
    /// Gets the connection string for the Cosmos DB account.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            if (UseGatewayMode == true)
            {
                return $"AccountEndpoint={Endpoint};AccountKey={AccountKey};ConnectionMode=Gateway;";
            }
            else
            {
                return $"AccountEndpoint={Endpoint};AccountKey={AccountKey};";
            }
        }
    }
}
